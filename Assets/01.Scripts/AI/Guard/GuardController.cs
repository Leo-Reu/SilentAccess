using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GuardData guardData;
    [SerializeField] private GuardPatrolRoute patrolRoute;

    private NavMeshAgent agent;
    private GuardHearing hearing;
    private GuardVision vision;

    private StateMachine<GuardController> stateMachine;

    private GuardPatrolState patrolState;
    private GuardInvestigateState investigateState;
    private GuardSuspiciousState suspiciousState;
    private GuardChaseState chaseState;

    private Vector3 investigatePos;
    private int patrolIndex;

    public NavMeshAgent Agent => agent;
    public GuardData Data => guardData;
    public GuardPatrolRoute PatrolRoute => patrolRoute;
    public GuardVision Vision => vision;
    public Vector3 InvestigatePos => investigatePos;

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
        if (stateMachine.CurrentState == suspiciousState || stateMachine.CurrentState == chaseState)
        {
            return;
        }

        investigatePos = noiseData.position;

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
}