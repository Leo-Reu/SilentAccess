using UnityEngine;

public class GuardChaseState : IState<GuardController>
{
    private float lostSightTimer;

    public void Enter(GuardController owner)
    {
        owner.Agent.speed = owner.Data.chaseSpeed;

        lostSightTimer = 0f;

        if (owner.Vision.IsPlayerVisible)
        {
            owner.UpdateLastKnownPosition();
            MoveToPlayer(owner);
        }
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision.IsPlayerVisible)
        {
            lostSightTimer = 0f;

            owner.UpdateLastKnownPosition();
            MoveToPlayer(owner);

            return;
        }

        lostSightTimer += Time.deltaTime;

        if (lostSightTimer >= owner.Data.lostSightTime)
        {
            owner.ChangeToSearch();
            return;
        }
    }

    public void Exit(GuardController owner)
    {
        lostSightTimer = 0f;
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