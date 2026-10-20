
using System;
using UnityEngine;

public enum MissionPhase
{
    Infiltration,
    Escape
}

public class MissionController : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MainServer mainServer;

    [Header("Security Alert")]
    [SerializeField] private Transform serverInvestigationPoint;
    [SerializeField] private GuardController[] guards;

    public MissionPhase CurrentPhase { get; private set; } = MissionPhase.Infiltration;
    public bool IsSecurityAlertActive { get; private set; }

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
        if (mainServer == null)
        {
            Debug.LogError("[MissionController] MainServer is not assigned");
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
        if (CurrentPhase != MissionPhase.Infiltration)
        {
            return;
        }

        CurrentPhase = MissionPhase.Escape;
        IsSecurityAlertActive = true;

        Debug.Log("[MissionController] Phase changed to Escape");
        Debug.Log("[MissionController] Security Alert activated");

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

        Debug.Log($"[MissionController] {closestGuard.name} dispatched to server room");
    }
}
