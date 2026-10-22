using UnityEngine;
using UnityEngine.AI;

public enum SecurityLevel
{
    Normal,
    Caution,
    HighAlert
}

public class AdaptiveSecurityDirector : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MissionController missionController;

    [Header("Security Pressure")]
    [SerializeField] private int maxPressure = 10;
    [SerializeField] private int cautionThreshold = 3;
    [SerializeField] private int highAlertThreshold = 6;

    [Header("Time Settings")]
    [SerializeField] private float noiseCooldown = 3f;
    [SerializeField] private float decayDelay = 12f;
    [SerializeField] private float decayInterval = 8f;

    [Header("Guard Support")]
    [SerializeField] private GuardController[] guards;
    [SerializeField] private float supportCooldown = 12f;
    [SerializeField] private float searchSpread = 4f;


    [Header("Level Response - Movement")]
    [SerializeField, Min(1f)]
    private float highAlertSpeedMultiplier = 1.1f;

    [Header("Level Response - Patrol")]
    [SerializeField, Range(0.1f, 1f)]
    private float cautionPatrolWaitMultiplier = 0.75f;

    [SerializeField, Range(0.1f, 1f)]
    private float highAlertPatrolWaitMultiplier = 0.5f;

    [Header("Level Response - Investigation")]
    [SerializeField, Min(1f)]
    private float cautionInvestigateTimeMultiplier = 1.25f;

    [SerializeField, Min(1f)]
    private float highAlertInvestigateTimeMultiplier = 1.5f;

    [Header("Level Response - Search")]
    [SerializeField, Min(1f)]
    private float cautionSearchTimeMultiplier = 1.25f;

    [SerializeField, Min(1f)]
    private float highAlertSearchTimeMultiplier = 1.75f;


    private float nextSupportTime;
    private float lastNoiseTime = -100f;
    private float nextDecayTime;

    public int Pressure { get; private set; }
    public SecurityLevel CurrentLevel { get; private set; } = SecurityLevel.Normal;

    public float MoveSpeedMultiplier =>
        CurrentLevel == SecurityLevel.HighAlert
            ? highAlertSpeedMultiplier : 1f;

    public float PatrolWaitMultiplier =>
        CurrentLevel == SecurityLevel.HighAlert
            ? highAlertPatrolWaitMultiplier :
        CurrentLevel == SecurityLevel.Caution
            ? cautionPatrolWaitMultiplier : 1f;

    public float InvestigateTimeMultiplier =>
        CurrentLevel == SecurityLevel.HighAlert
            ? highAlertInvestigateTimeMultiplier :
        CurrentLevel == SecurityLevel.Caution
            ? cautionInvestigateTimeMultiplier : 1f;

    public float SearchTimeMultiplier =>
        CurrentLevel == SecurityLevel.HighAlert
            ? highAlertSearchTimeMultiplier :
        CurrentLevel == SecurityLevel.Caution
            ? cautionSearchTimeMultiplier : 1f;


    private void OnEnable()
    {
        if (missionController != null)
        {
            missionController.OnSecurityAlertTriggered += OnSecurityAlert;
        }
    }

    private void OnDisable()
    {
        if (missionController != null)
        {
            missionController.OnSecurityAlertTriggered -= OnSecurityAlert;
        }
    }

    private void Start()
    {
        if (missionController != null && missionController.IsSecurityAlertActive)
        {
            OnSecurityAlert();
        }
    }

    private void Update()
    {
        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            return;
        }

        if (guards != null)
        {
            foreach (GuardController guard in guards)
            {
                if (guard != null && guard.IsChasing)
                {
                    nextDecayTime = Time.time + decayDelay;
                    return;
                }
            }
        }

        int minimum = 0;

        if (missionController != null && missionController.IsSecurityAlertActive)
        {
            minimum = cautionThreshold;
        }

        if (Pressure <= minimum || Time.time < nextDecayTime)
        {
            return;
        }

        SetPressure(Mathf.Max(minimum, Pressure - 1));
        nextDecayTime = Time.time + decayInterval;
    }

    public void ReportHeardNoise(NoiseData noise)
    {
        if (noise.type != NoiseType.Footstep || noise.radius < 10f)
        {
            return;
        }

        if (Time.time - lastNoiseTime < noiseCooldown)
        {
            return;
        }

        lastNoiseTime = Time.time;
        AddPressure(1);
    }

    private void OnSecurityAlert()
    {
        SetPressure(Mathf.Max(Pressure, cautionThreshold));
        nextDecayTime = Time.time + decayDelay;
    }

    public void ReportCCTV()
    {
        AddPressure(2);
    }

    public void ReportChase(Vector3 detectedPos, GuardController sourceGuard)
    {
        AddPressure(3);
    }

    private void AddPressure(int amount)
    {
        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            return;
        }

        SetPressure(Pressure + amount);
        nextDecayTime = Time.time + decayDelay;
    }


    private void SetPressure(int value)
    {
        int nextPressure = Mathf.Clamp(value, 0, maxPressure);

        if (Pressure == nextPressure)
        {
            return;
        }

        Pressure = nextPressure;

        SecurityLevel nextLevel = SecurityLevel.Normal;

        if (Pressure >= highAlertThreshold)
        {
            nextLevel = SecurityLevel.HighAlert;
        }
        else if (Pressure >= cautionThreshold)
        {
            nextLevel = SecurityLevel.Caution;
        }

        if (CurrentLevel != nextLevel)
        {
            CurrentLevel = nextLevel;

            RefreshGuardResponses();

            Debug.Log($"[Director] Security Level: {CurrentLevel}");
        }

        Debug.Log($"[Director] Pressure: {Pressure}");
    }

    private void RefreshGuardResponses()
    {
        if (guards == null)
        {
            return;
        }

        foreach (GuardController guard in guards)
        {
            if (guard != null)
            {
                guard.RefreshDirectorResponse();
            }
        }
    }


    private void DispatchSupport(Vector3 targetPos, GuardController sourceGuard)
    {
        if (CurrentLevel == SecurityLevel.Normal || Time.time < nextSupportTime)
        {
            return;
        }

        if (guards == null || sourceGuard == null)
        {
            return;
        }

        int supportCount = CurrentLevel == SecurityLevel.HighAlert ? 2 : 1;
        int dispatchedCount = 0;

        for (int i = 0; i < supportCount; i++)
        {
            Vector3 searchPos = targetPos;

            // 두 번째 지원 Guard는 다른 위치로 분산
            if (i == 1)
            {
                Vector3 candidate = targetPos + sourceGuard.transform.right * searchSpread;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    continue;
                }

                searchPos = hit.position;
            }

            GuardController closestGuard = null;
            float closestDistance = float.MaxValue;

            foreach (GuardController guard in guards)
            {
                if (guard == null || guard == sourceGuard || !guard.CanAssist)
                {
                    continue;
                }

                NavMeshPath path = new NavMeshPath();

                if (!guard.Agent.CalculatePath(searchPos, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float distance = Vector3.Distance(guard.transform.position, searchPos);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGuard = guard;
                }
            }

            if (closestGuard == null)
            {
                continue;
            }

            closestGuard.OnDirectorAlert(searchPos);
            dispatchedCount++;

            Debug.Log($"[Director] Search support: {closestGuard.name} → {searchPos}");
        }

        if (dispatchedCount > 0)
        {
            nextSupportTime = Time.time + supportCooldown;
        }
    }

    public void ReportLostTarget(Vector3 lastKnownPos, GuardController sourceGuard)
    {
        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            return;
        }

        DispatchSupport(lastKnownPos, sourceGuard);
    }
}
