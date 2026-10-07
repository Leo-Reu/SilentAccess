using UnityEngine;

public class GuardChaseState : IState<GuardController>
{
    public void Enter(GuardController owner)
    {
        owner.Agent.speed = owner.Data.chaseSpeed;

        MoveToPlayer(owner);
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision.IsPlayerVisible)
        {
            MoveToPlayer(owner);
        }

        if (!owner.Vision.IsPlayerVisible && !owner.Vision.IsPartiallyDetected)
        {
            owner.ChangeToSuspicious();
            return;
        }
    }

    public void Exit(GuardController owner)
    {
    }

    private void MoveToPlayer(GuardController owner)
    {
        Transform player = owner.Vision.Player;

        if (player == null)
        {
            return;
        }

        owner.Agent.SetDestination(player.position);
    }
}