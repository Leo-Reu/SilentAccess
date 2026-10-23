using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GuardData guardData;
    [SerializeField] private GuardPatrolRoute patrolRoute;

    [Header("Security Alert")]
    [SerializeField] private float alertSpeedMultiplier = 1.2f;

    [Header("Capture")]
    [SerializeField] private MissionController missionController;
    [SerializeField] private float captureDistance = 1.5f;

    [Header("Director")]
    [SerializeField] private AdaptiveSecurityDirector securityDirector;

    private float baseMoveSpeed;
    private float nextInvestigateNoiseTime;

    public bool IsAlerted { get; private set; }

    private NavMeshAgent agent;
    private GuardHearing hearing;
    private GuardVision vision; 
    private GuardController directorSupportSource;

    private StateMachine<GuardController> stateMachine;

    private GuardPatrolState patrolState;
    private GuardInvestigateState investigateState;
    private GuardSuspiciousState suspiciousState;
    private GuardChaseState chaseState;
    private GuardSearchState searchState;

    private Vector3 investigatePos;
    private Vector3 lastKnownPos;
    private int patrolIndex;

    public NavMeshAgent Agent => agent;
    public GuardData Data => guardData;
    public GuardPatrolRoute PatrolRoute => patrolRoute;
    public GuardVision Vision => vision;
    public Vector3 InvestigatePos => investigatePos;
    public Vector3 LastKnownPos => lastKnownPos;
    public string CurrentStateName => stateMachine?.CurrentState?.GetType().Name ?? "None";

    public float PatrolWaitDuration =>
        guardData.patrolWaitTime *
        (securityDirector != null
            ? securityDirector.PatrolWaitMultiplier : 1f);

    public float InvestigateDuration =>
        guardData.investigateTime *
        (securityDirector != null
            ? securityDirector.InvestigateTimeMultiplier : 1f);

    public float SearchDuration =>
        guardData.searchTime *
        (securityDirector != null
            ? securityDirector.SearchTimeMultiplier : 1f);


    public bool CanInvestigate =>
        isActiveAndEnabled &&
        agent != null &&
        agent.isOnNavMesh &&
        stateMachine != null &&
        stateMachine.CurrentState != suspiciousState &&
        stateMachine.CurrentState != chaseState;

    public bool CanAssist =>
        isActiveAndEnabled &&
        agent != null &&
        agent.isOnNavMesh &&
        stateMachine != null &&
        stateMachine.CurrentState == patrolState;

    public bool IsChasing =>
        stateMachine != null &&
        stateMachine.CurrentState == chaseState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hearing = GetComponent<GuardHearing>();
        vision = GetComponent<GuardVision>();

        stateMachine = new StateMachine<GuardController>(this);

        patrolState = new GuardPatrolState();
        investigateState = new GuardInvestigateState();
        suspiciousState = new GuardSuspiciousState();
        chaseState = new GuardChaseState();
        searchState = new GuardSearchState();
    }

    private void OnEnable()
    {
        if (hearing != null)
        {
            hearing.OnNoiseHeard += OnNoiseHeard;
        }
    }

    private void Start()
    {
        agent.stoppingDistance = guardData.stoppingDistance;
        stateMachine.ChangeState(patrolState);
    }

    private void Update()
    {
        stateMachine.Update();
    }

    private void OnDisable()
    {
        if (hearing != null)
        {
            hearing.OnNoiseHeard -= OnNoiseHeard;
        }
    }

    private void OnNoiseHeard(NoiseData noiseData)
    {
        if (securityDirector != null)
        {
            securityDirector.ReportHeardNoise(noiseData);
        }

        if (stateMachine.CurrentState == chaseState || stateMachine.CurrentState == suspiciousState)
        {
            return;
        }

        if (stateMachine.CurrentState == searchState)
        {
            searchState.UpdateNoiseTarget(this, noiseData.position);
            return;
        }

        if (stateMachine.CurrentState == investigateState && noiseData.type == NoiseType.Footstep)
        {
            if (Time.time < nextInvestigateNoiseTime)
            {
                return;
            }

            if (Vector3.Distance(investigatePos, noiseData.position) < guardData.noiseRetargetDistance)
            {
                return;
            }

            nextInvestigateNoiseTime = Time.time + guardData.noiseRetargetInterval;
            Investigate(noiseData.position);
            return;
        }

        Investigate(noiseData.position);
    }


    public void OnCCTVDetected(Vector3 detectedPos)
    {
        Investigate(detectedPos);
    }

    public void OnSecurityAlert(Vector3 alertPos)
    {
        Investigate(alertPos);
    }

    public void OnDirectorAlert(Vector3 detectedPos, GuardController sourceGuard)
    {
        Investigate(detectedPos);

        if (stateMachine.CurrentState == investigateState)
        {
            directorSupportSource = sourceGuard;
        }
    }

    public void SetMoveSpeed(float speed)
    {
        baseMoveSpeed = speed;
        UpdateMoveSpeed();
    }

    public void SetAlerted()
    {
        if (IsAlerted)
        {
            return;
        }

        IsAlerted = true;
        UpdateMoveSpeed();
    }

    private void UpdateMoveSpeed()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        float missionMultiplier =
            IsAlerted ? alertSpeedMultiplier : 1f;

        float directorMultiplier =
            securityDirector != null
                ? securityDirector.MoveSpeedMultiplier : 1f;

        agent.speed =
            baseMoveSpeed * missionMultiplier * directorMultiplier;
    }

    public void RefreshDirectorResponse()
    {
        UpdateMoveSpeed();
    }


    private void Investigate(Vector3 targetPos)
    {
        directorSupportSource = null;

        if (stateMachine.CurrentState == suspiciousState || stateMachine.CurrentState == chaseState)
        {
            return;
        }

        investigatePos = targetPos;

        if (stateMachine.CurrentState == investigateState)
        {
            investigateState.UpdateTarget(this);
            return;
        }

        stateMachine.ChangeState(investigateState);
    }

    public Transform GetPatrolPoint()
    {
        return patrolRoute.GetPoint(patrolIndex);
    }

    public void NextPatrolPoint()
    {
        patrolIndex++;

        if (patrolIndex >= patrolRoute.Count)
        {
            patrolIndex = 0;
        }
    }

    public void UpdateLastKnownPosition()
    {
        if (vision == null || vision.Player == null)
        {
            return;
        }

        lastKnownPos = vision.Player.position;
    }

    public void ChangeToPatrol()
    {
        bool searchFinished = stateMachine.CurrentState == searchState;

        directorSupportSource = null;
        stateMachine.ChangeState(patrolState);

        if (searchFinished && securityDirector != null)
        {
            securityDirector.ReportSearchFinished(this);
        }
    }

    public void ChangeToSuspicious()
    {
        directorSupportSource = null;
        stateMachine.ChangeState(suspiciousState);
    }

    public void ChangeToChase()
    {
        if (stateMachine.CurrentState == chaseState)
        {
            return;
        }

        directorSupportSource = null;
        stateMachine.ChangeState(chaseState);

        if (securityDirector != null && vision != null && vision.Player != null)
        {
            securityDirector.ReportChase(vision.Player.position, this);
        }
    }


    public void ReturnFromSuspicious()
    {
        if (stateMachine.PreviousState == investigateState)
        {
            stateMachine.ChangeState(investigateState);
            return;
        }

        stateMachine.ChangeState(patrolState);
    }

    public void ChangeToSearch()
    {
        bool lostChase = stateMachine.CurrentState == chaseState;

        stateMachine.ChangeState(searchState);

        if (lostChase && securityDirector != null)
        {
            securityDirector.ReportLostTarget(lastKnownPos, this);
        }
    }

    public bool TryCapturePlayer()
    {
        if (missionController == null || vision == null || !vision.IsPlayerVisible || vision.Player == null)
        {
            return false;
        }

        if (missionController.CurrentResult != MissionResult.None)
        {
            return false;
        }

        Vector3 dir = vision.Player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > captureDistance * captureDistance)
        {
            return false;
        }

        missionController.FailMission();
        return true;
    }

    public void CancelDirectorSupport(GuardController sourceGuard)
    {
        if (directorSupportSource != sourceGuard ||
            stateMachine.CurrentState != investigateState)
        {
            return;
        }

        directorSupportSource = null;
        ChangeToPatrol();
    }
}