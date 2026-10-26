using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("High Alert Tactics")]
    [SerializeField, Min(0f)] private float blockMinDistance = 6f;
    [SerializeField, Min(0f)] private float blockMaxDistance = 30f;
    [SerializeField, Min(0f)] private float blockIdealDistance = 18f;
    [SerializeField, Min(0f)] private float blockDistancePenalty = 2f;
    [SerializeField, Min(0f)] private float blockWatchDuration = 10f;
    [SerializeField, Min(0f)] private float blockOffsetDistance = 2.5f;
    [SerializeField, Min(0f)] private float blockPatrolClearance = 1.6f;

    [Header("Support Incident")]
    [SerializeField, Min(0f)] private float incidentMinDuration = 15f;
    [SerializeField, Min(0f)] private float incidentAfterSearchDuration = 8f;
    [SerializeField, Min(0f)] private float supportResolveTimeout = 30f;

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

    public float BlockWatchDuration => blockWatchDuration;
    private float nextSupportTime;
    private float lastNoiseTime = -100f;
    private float nextDecayTime;

    private enum SupportTaskStatus
    {
        Moving,
        Investigating,
        Completed,
        Interrupted,
        Cancelled
    }

    private enum SupportRole
    {
        Search,
        Block
    }

    private class SupportAssignment
    {
        public GuardController sourceGuard;
        public SupportTaskStatus status;
        public SupportRole role;
    }


    private class SupportIncident
    {
        public Vector3 lastKnownPosition;
        public float startTime;
        public float lastSearchEndTime = -1f;

        public readonly HashSet<GuardController> searchingGuards = new HashSet<GuardController>();
        public readonly HashSet<GuardController> supportSources = new HashSet<GuardController>();

        public readonly Dictionary<GuardController, SupportAssignment> supportAssignments = new Dictionary<GuardController, SupportAssignment>();
    }

    private SupportIncident currentIncident;

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
            currentIncident = null;
            return;
        }

        UpdateSupportIncident();

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

        if (currentIncident != null && sourceGuard != null)
        {
            currentIncident.searchingGuards.Add(sourceGuard);
            currentIncident.lastSearchEndTime = -1f;
        }
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
            SupportRole supportRole = SupportRole.Search;

            if (i == 1)
            {
                float bestScore = float.MaxValue;
                float bestPathDistance = 0f;
                float bestIncidentDistance = 0f;
                Transform selectedPoint = null;
                GuardController selectedRouteGuard = null;

                foreach (GuardController routeGuard in guards)
                {
                    if (routeGuard == null || routeGuard.PatrolRoute == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < routeGuard.PatrolRoute.Count; j++)
                    {
                        Transform waypoint = routeGuard.PatrolRoute.GetPoint(j);

                        if (waypoint == null)
                        {
                            continue;
                        }

                        if (!NavMesh.SamplePosition(waypoint.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                        {
                            continue;
                        }

                        float incidentDistance = Vector3.Distance(targetPos, hit.position);

                        if (incidentDistance < blockMinDistance || incidentDistance > blockMaxDistance)
                        {
                            continue;
                        }

                        foreach (GuardController supportGuard in guards)
                        {
                            if (supportGuard == null || supportGuard == sourceGuard || !supportGuard.CanAssist)
                            {
                                continue;
                            }

                            NavMeshPath path = new NavMeshPath();

                            if (!supportGuard.Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                            {
                                continue;
                            }

                            Vector3[] corners = path.corners;

                            if (corners.Length == 0)
                            {
                                continue;
                            }

                            float pathDistance = Vector3.Distance(supportGuard.transform.position, corners[0]);

                            for (int k = 1; k < corners.Length; k++)
                            {
                                pathDistance += Vector3.Distance(corners[k - 1], corners[k]);
                            }

                            pathDistance += Vector3.Distance(corners[corners.Length - 1], hit.position);

                            float score = pathDistance + Mathf.Abs(incidentDistance - blockIdealDistance) * blockDistancePenalty;

                            if (score >= bestScore)
                            {
                                continue;
                            }

                            bestScore = score;
                            bestPathDistance = pathDistance;
                            bestIncidentDistance = incidentDistance;
                            selectedPoint = waypoint;
                            selectedRouteGuard = routeGuard;
                            searchPos = hit.position;
                        }
                    }
                }

                if (selectedPoint != null && TryFindSafeBlockPosition(searchPos, out Vector3 safeBlockPosition))
                {
                    Vector3 originalPosition = searchPos;

                    searchPos = safeBlockPosition;
                    supportRole = SupportRole.Block;

                    Debug.Log($"[Director] Tactical Block: {selectedRouteGuard.name}/{selectedPoint.name} / Path: {bestPathDistance:F1} / Distance: {bestIncidentDistance:F1} / Score: {bestScore:F1}");
                    Debug.Log($"[Director] Block position adjusted: {originalPosition} → {searchPos}");
                }
                else
                {
                    Vector3 candidate = targetPos + sourceGuard.transform.right * searchSpread;

                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        continue;
                    }

                    searchPos = hit.position;
                }
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

                Vector3[] corners = path.corners;

                if (corners.Length == 0)
                {
                    continue;
                }

                float distance = Vector3.Distance(guard.transform.position, corners[0]);

                for (int j = 1; j < corners.Length; j++)
                {
                    distance += Vector3.Distance(corners[j - 1], corners[j]);
                }

                distance += Vector3.Distance(corners[corners.Length - 1], searchPos);

                if (distance < closestDistance)
                {
                    Debug.Log($"[Director] Support candidate: {guard.name} / Straight: {Vector3.Distance(guard.transform.position, searchPos):F1} / Path: {distance:F1}");
                    closestDistance = distance;
                    closestGuard = guard;
                }
            }

            if (closestGuard == null)
            {
                continue;
            }

            closestGuard.OnDirectorAlert(searchPos, sourceGuard, supportRole == SupportRole.Block, targetPos);

            if (currentIncident != null)
            {
                SupportAssignment assignment = new SupportAssignment();
                assignment.sourceGuard = sourceGuard;
                assignment.status = SupportTaskStatus.Moving;
                assignment.role = supportRole;

                currentIncident.supportAssignments[closestGuard] = assignment;
            }

            dispatchedCount++;

            Debug.Log($"[Director] Search support: {closestGuard.name} → {searchPos} / Role: {supportRole}");
        }

        if (dispatchedCount > 0)
        {
            nextSupportTime = Time.time + supportCooldown;
        }
    }


    private bool TryFindSafeBlockPosition(Vector3 waypointPosition, out Vector3 safePosition)
    {
        safePosition = waypointPosition;
        float bestClearance = -1f;

        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * blockOffsetDistance;
            Vector3 candidatePosition = waypointPosition + offset;

            if (!NavMesh.SamplePosition(candidatePosition, out NavMeshHit hit, 0.6f, NavMesh.AllAreas))
            {
                continue;
            }

            if (Vector3.Distance(waypointPosition, hit.position) < blockPatrolClearance)
            {
                continue;
            }

            if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas) || edge.distance < 0.6f)
            {
                continue;
            }

            float nearestPatrolDistance = float.MaxValue;
            Vector3 candidate = hit.position;
            candidate.y = 0f;

            foreach (GuardController guard in guards)
            {
                if (guard == null || guard.PatrolRoute == null || guard.PatrolRoute.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < guard.PatrolRoute.Count; j++)
                {
                    Transform startPoint = guard.PatrolRoute.GetPoint(j);
                    Transform endPoint = guard.PatrolRoute.GetPoint((j + 1) % guard.PatrolRoute.Count);

                    if (startPoint == null || endPoint == null)
                    {
                        continue;
                    }

                    Vector3 start = startPoint.position;
                    Vector3 end = endPoint.position;
                    start.y = 0f;
                    end.y = 0f;

                    Vector3 segment = end - start;
                    float t = segment.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(candidate - start, segment) / segment.sqrMagnitude) : 0f;
                    Vector3 nearestPoint = start + segment * t;

                    float distance = Vector3.Distance(candidate, nearestPoint);
                    nearestPatrolDistance = Mathf.Min(nearestPatrolDistance, distance);
                }
            }

            if (nearestPatrolDistance < blockPatrolClearance || nearestPatrolDistance <= bestClearance)
            {
                continue;
            }

            bestClearance = nearestPatrolDistance;
            safePosition = hit.position;
        }

        return bestClearance >= 0f;
    }


    public void ReportLostTarget(Vector3 lastKnownPos, GuardController sourceGuard)
    {
        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            return;
        }

        if (sourceGuard == null)
        {
            return;
        }

        if (currentIncident == null)
        {
            currentIncident = new SupportIncident();
            currentIncident.startTime = Time.time;

            Debug.Log("[Director] Incident started");
        }

        currentIncident.lastKnownPosition = lastKnownPos;
        currentIncident.searchingGuards.Add(sourceGuard);
        currentIncident.supportSources.Add(sourceGuard);
        currentIncident.lastSearchEndTime = -1f;

        DispatchSupport(lastKnownPos, sourceGuard);
    }


    public void ReportSearchFinished(GuardController sourceGuard)
    {
        if (currentIncident == null || sourceGuard == null)
        {
            return;
        }

        if (currentIncident.searchingGuards.Remove(sourceGuard) && currentIncident.searchingGuards.Count == 0)
        {
            currentIncident.lastSearchEndTime = Time.time;

            Debug.Log("[Director] Incident resolving");
        }
    }

    public void ReportSupportArrived(GuardController supportGuard, GuardController sourceGuard)
    {
        if (currentIncident == null || supportGuard == null || sourceGuard == null)
        {
            return;
        }

        if (!currentIncident.supportAssignments.TryGetValue(supportGuard, out SupportAssignment assignment))
        {
            return;
        }

        if (assignment.sourceGuard != sourceGuard || assignment.status != SupportTaskStatus.Moving)
        {
            return;
        }

        assignment.status = SupportTaskStatus.Investigating;

        Debug.Log($"[Director] Support arrived: {supportGuard.name}");
    }

    private void UpdateSupportIncident()
    {
        if (currentIncident == null || currentIncident.searchingGuards.Count > 0)
        {
            return;
        }

        if (currentIncident.lastSearchEndTime < 0f)
        {
            currentIncident.lastSearchEndTime = Time.time;
        }

        float minEndTime = currentIncident.startTime + incidentMinDuration;
        float searchEndTime = currentIncident.lastSearchEndTime + incidentAfterSearchDuration;

        if (Time.time < minEndTime || Time.time < searchEndTime)
        {
            return;
        }

        bool hasActiveSupport = false;

        foreach (SupportAssignment assignment in currentIncident.supportAssignments.Values)
        {
            if (assignment.status == SupportTaskStatus.Moving || assignment.status == SupportTaskStatus.Investigating)
            {
                hasActiveSupport = true;
                break;
            }
        }

        float supportEndTime = currentIncident.lastSearchEndTime + Mathf.Max(incidentAfterSearchDuration, supportResolveTimeout);

        if (hasActiveSupport && Time.time < supportEndTime)
        {
            return;
        }

        if (guards != null)
        {
            foreach (GuardController guard in guards)
            {
                if (guard == null)
                {
                    continue;
                }

                foreach (GuardController sourceGuard in currentIncident.supportSources)
                {
                    if (sourceGuard != null)
                    {
                        guard.CancelDirectorSupport(sourceGuard);
                    }
                }
            }
        }

        Debug.Log("[Director] Incident ended");

        currentIncident = null;
    }

    public void ReportSupportCompleted(GuardController supportGuard, GuardController sourceGuard)
    {
        UpdateSupportAssignmentStatus(supportGuard, sourceGuard, SupportTaskStatus.Completed);
    }

    public void ReportSupportInterrupted(GuardController supportGuard, GuardController sourceGuard)
    {
        UpdateSupportAssignmentStatus(supportGuard, sourceGuard, SupportTaskStatus.Interrupted);
    }

    public void ReportSupportCancelled(GuardController supportGuard, GuardController sourceGuard)
    {
        UpdateSupportAssignmentStatus(supportGuard, sourceGuard, SupportTaskStatus.Cancelled);
    }

    private void UpdateSupportAssignmentStatus(GuardController supportGuard, GuardController sourceGuard, SupportTaskStatus status)
    {
        if (currentIncident == null || supportGuard == null || sourceGuard == null)
        {
            return;
        }

        if (!currentIncident.supportAssignments.TryGetValue(supportGuard, out SupportAssignment assignment))
        {
            return;
        }

        if (assignment.sourceGuard != sourceGuard)
        {
            return;
        }

        if (assignment.status != SupportTaskStatus.Moving && assignment.status != SupportTaskStatus.Investigating)
        {
            return;
        }

        assignment.status = status;

        if (status == SupportTaskStatus.Completed)
        {
            Debug.Log($"[Director] Support completed: {supportGuard.name}");
        }
        else if (status == SupportTaskStatus.Interrupted)
        {
            Debug.Log($"[Director] Support interrupted: {supportGuard.name}");
        }
    }
}
