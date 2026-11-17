using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MissionPhase
{
    Infiltration,
    Escape
}

public enum MissionResult
{
    None,
    Success,
    Failed
}

public class MissionController : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MainServer mainServer;

    [Header("Security Alert")]
    [SerializeField] private Transform serverInvestigationPoint;
    [SerializeField] private GuardController[] guards;

    [Header("Retry")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform escapeCheckpoint;
    [SerializeField] private SecurityDoor serverDoor;
    [SerializeField] private KeycardPickup keycardPickup;

    [Header("Debug")]
    [SerializeField] private bool debugInvincible;

    private static bool retryFromEscape;

    public bool DebugInvincible => Application.isEditor && debugInvincible;

    public MissionPhase CurrentPhase { get; private set; } = MissionPhase.Infiltration;
    public bool IsSecurityAlertActive { get; private set; }
    public MissionResult CurrentResult { get; private set; } = MissionResult.None;

    public event Action<MissionResult> OnMissionEnded;
    public event Action<MissionPhase> OnPhaseChanged;
    public event Action OnSecurityAlertTriggered;

    private void OnEnable()
    {
        if (mainServer != null)
        {
            mainServer.OnDataStolen += HandleDataStolen;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        SoundManager.Instance?.PlayBGM(BGMType.Stealth);

        if (mainServer == null)
        {
            Debug.LogError("[MissionController] MainServer is not assigned");
            return;
        }

        if (retryFromEscape)
        {
            retryFromEscape = false;
            StartCoroutine(RestoreEscapeCheckpoint());
            return;
        }

        if (mainServer.IsDataStolen)
        {
            HandleDataStolen();
        }
    }

    private void OnDisable()
    {
        if (mainServer != null)
        {
            mainServer.OnDataStolen -= HandleDataStolen;
        }
    }

    private void HandleDataStolen()
    {
        if (CurrentPhase != MissionPhase.Infiltration || CurrentResult != MissionResult.None)
        {
            return;
        }

        CurrentPhase = MissionPhase.Escape;
        IsSecurityAlertActive = true;

        OnPhaseChanged?.Invoke(CurrentPhase);
        OnSecurityAlertTriggered?.Invoke();

        if (guards != null)
        {
            foreach (GuardController guard in guards)
            {
                if (guard != null)
                {
                    guard.SetAlerted();
                }
            }
        }

        DispatchGuard();
    }

    private void DispatchGuard()
    {
        if (serverInvestigationPoint == null)
        {
            Debug.LogWarning("[MissionController] Investigation point is not assigned");
            return;
        }

        GuardController closestGuard = null;
        float closestDistance = float.MaxValue;

        if (guards == null)
        {
            return;
        }

        foreach (GuardController guard in guards)
        {
            if (guard == null || !guard.CanInvestigate)
            {
                continue;
            }

            float distance = Vector3.Distance(guard.transform.position, serverInvestigationPoint.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGuard = guard;
            }
        }

        if (closestGuard == null)
        {
            Debug.LogWarning("[MissionController] No available guard for security alert");
            return;
        }

        closestGuard.OnSecurityAlert(serverInvestigationPoint.position);
    }

    public void CompleteMission()
    {
        if (CurrentPhase != MissionPhase.Escape || CurrentResult != MissionResult.None)
        {
            return;
        }

        CurrentResult = MissionResult.Success;

        SoundManager.Instance?.FadeOutBGM();
        OnMissionEnded?.Invoke(CurrentResult);
    }

    public void FailMission()
    {
        if (CurrentResult != MissionResult.None)
        {
            return;
        }

        CurrentResult = MissionResult.Failed;

        SoundManager.Instance?.FadeOutBGM();
        OnMissionEnded?.Invoke(CurrentResult);
    }

    private void OnApplicationQuit()
    {
        retryFromEscape = false;
    }

    public void RetryMission()
    {
        retryFromEscape = CurrentResult == MissionResult.Failed && CurrentPhase == MissionPhase.Escape;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator RestoreEscapeCheckpoint()
    {
        if (player == null || escapeCheckpoint == null || serverDoor == null || mainServer == null)
        {
            Debug.LogError("[MissionController] Checkpoint references are missing");
            yield break;
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.SetPositionAndRotation(escapeCheckpoint.position, escapeCheckpoint.rotation);
        Physics.SyncTransforms();

        if (controller != null)
        {
            controller.enabled = true;
        }

        yield return null;

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError("[MissionController] PlayerInventory is missing");
            yield break;
        }

        inventory.CollectKeycard();

        if (keycardPickup != null)
        {
            keycardPickup.gameObject.SetActive(false);
        }

        serverDoor.Unlock();
        mainServer.RestoreDataTheft();

        HandleDataStolen();
    }

    public void SetDebugInvincible(bool value)
    {
        debugInvincible = value;
    }
}