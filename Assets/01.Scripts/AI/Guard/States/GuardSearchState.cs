using UnityEngine;

public class GuardSearchState : IState<GuardController>
{
    private float waitTimer;
    private bool arrived;

    public void Enter(GuardController owner)
    {
        owner.Agent.speed = owner.Data.searchSpeed;

        waitTimer = 0f;
        arrived = false;

        owner.Agent.SetDestination(owner.LastKnownPos);
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision.IsPlayerVisible)
        {
            owner.ChangeToChase();
            return;
        }

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

        if (waitTimer >= owner.Data.searchTime)
        {
            owner.ChangeToPatrol();
            return;
        }
    }

    public void Exit(GuardController owner)
    {
        waitTimer = 0f;
        arrived = false;
    }
}