using UnityEngine;

public class GuardChaseState : IState<GuardController>
{
    private float lostSightTimer;

    public void Enter(GuardController owner)
    {
        owner.SetMoveSpeed(owner.Data.chaseSpeed);
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

            if (owner.TryCapturePlayer())
            {
                return;
            }

            MoveToPlayer(owner);
            return;
        }

        lostSightTimer += Time.deltaTime;

        // 최소 추격 유지 시간 동안 Chase 상태 유지
        if (lostSightTimer < owner.Data.lostSightTime)
        {
            return;
        }

        // 마지막 이동 경로가 남아 있다면 최대 유지 시간까지 추격
        bool movingToLastKnown = owner.Agent.pathPending || (owner.Agent.hasPath && owner.Agent.remainingDistance > owner.Agent.stoppingDistance + 0.2f);

        if (movingToLastKnown && lostSightTimer < owner.Data.maxLostSightChaseTime)
        {
            return;
        }

        owner.ChangeToSearch();
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