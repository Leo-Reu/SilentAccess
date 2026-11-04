using UnityEngine;

public class GuardSuspiciousState : IState<GuardController>
{
    public void Enter(GuardController owner)
    {
        owner.Agent.ResetPath();

        if (owner.Vision.IsPlayerVisible)
        {
            owner.UpdateLastKnownPosition();
        }
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision.IsPlayerVisible)
        {
            // 플레이어를 보고 있는 동안 마지막 목격 위치 갱신
            owner.UpdateLastKnownPosition();

            if (owner.Vision.IsFullyDetected)
            {
                owner.ChangeToChase();
                return;
            }

            LookAtPlayer(owner);
            return;
        }

        // 플레이어를 놓쳤다면 마지막 목격 위치로 조사 이동
        owner.InvestigateLastKnownPosition();
    }

    public void Exit(GuardController owner)
    {
    }

    private void LookAtPlayer(GuardController owner)
    {
        Transform player = owner.Vision.Player;

        if (player == null)
        {
            return;
        }

        Vector3 dir = player.position - owner.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, targetRotation, owner.Agent.angularSpeed * Time.deltaTime);
    }
}