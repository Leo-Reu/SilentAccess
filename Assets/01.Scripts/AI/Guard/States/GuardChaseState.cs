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

        // 최소 추적 유지 시간이 지나기 전에는 Chase 유지
        if (lostSightTimer < owner.Data.lostSightTime)
        {
            return;
        }

        // 마지막 목격 위치까지 이동 중인지 확인
        bool movingToLastKnown = owner.Agent.pathPending ||
            (owner.Agent.hasPath && owner.Agent.remainingDistance > owner.Agent.stoppingDistance + 0.2f);

        // 이동 중이라면 최대 유지 시간까지 Chase 지속
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