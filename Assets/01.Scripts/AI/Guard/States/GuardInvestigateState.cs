using UnityEngine;

public class GuardInvestigateState : IState<GuardController>
{
    private float waitTimer;
    private bool arrived;

    public void Enter(GuardController owner)
    {
        owner.Agent.speed = owner.Data.investigateSpeed;

        UpdateTarget(owner);
    }

    public void Update(GuardController owner)
    {
        if (!arrived)
        {
            if (owner.Agent.pathPending)
            {
                return;
            }

            if (owner.Agent.remainingDistance > owner.Agent.stoppingDistance)
            {
                return;
            }

            arrived = true;
            owner.Agent.ResetPath();
        }

        waitTimer += Time.deltaTime;

        if (waitTimer >= owner.Data.investigateTime)
        {
            owner.ChangeToPatrol();
        }
    }

    public void Exit(GuardController owner)
    {
        waitTimer = 0f;
        arrived = false;
    }

    public void UpdateTarget(GuardController owner)
    {
        waitTimer = 0f;
        arrived = false;

        owner.Agent.SetDestination(owner.InvestigatePos);
    }
}