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

    private float baseMoveSpeed;

    public bool IsAlerted { get; private set; }

    private NavMeshAgent agent;
    private GuardHearing hearing;
    private GuardVision vision;

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
    public bool CanInvestigate =>
        isActiveAndEnabled &&
        agent != null &&
        agent.isOnNavMesh &&
        stateMachine != null &&
        stateMachine.CurrentState != suspiciousState &&
        stateMachine.CurrentState != chaseState;

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
        if (stateMachine.CurrentState == investigateState && noiseData.type == NoiseType.Footstep)
        {
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

        agent.speed = baseMoveSpeed * (IsAlerted ? alertSpeedMultiplier : 1f);
    }

    private void Investigate(Vector3 targetPos)
    {
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
        stateMachine.ChangeState(patrolState);
    }

    public void ChangeToSuspicious()
    {
        stateMachine.ChangeState(suspiciousState);
    }

    public void ChangeToChase()
    {
        stateMachine.ChangeState(chaseState);
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
        stateMachine.ChangeState(searchState);
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
}