using UnityEngine;

public class GuardPatrolState : IState<GuardController>
{
    private float waitTimer;

    public void Enter(GuardController owner)
    {
        owner.Agent.speed = owner.Data.patrolSpeed;

        waitTimer = 0f;

        MoveToPoint(owner);
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision != null)
        {
            if (owner.Vision.IsFullyDetected)
            {
                owner.ChangeToChase();
                return;
            }

            if (owner.Vision.IsPartiallyDetected)
            {
                owner.ChangeToSuspicious();
                return;
            }
        }

        if (owner.PatrolRoute == null || owner.PatrolRoute.Count == 0)
        {
            return;
        }

        if (owner.Agent.pathPending)
        {
            return;
        }

        if (owner.Agent.remainingDistance > owner.Agent.stoppingDistance)
        {
            return;
        }

        waitTimer += Time.deltaTime;

        if (waitTimer < owner.Data.patrolWaitTime)
        {
            return;
        }

        owner.NextPatrolPoint();

        waitTimer = 0f;

        MoveToPoint(owner);
    }

    public void Exit(GuardController owner)
    {
        waitTimer = 0f;
    }

    private void MoveToPoint(GuardController owner)
    {
        Transform patrolPoint = owner.GetPatrolPoint();

        if (patrolPoint == null)
        {
            return;
        }

        owner.Agent.SetDestination(patrolPoint.position);
    }
}