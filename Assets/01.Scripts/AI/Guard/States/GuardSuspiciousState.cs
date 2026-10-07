using UnityEngine;

public class GuardSuspiciousState : IState<GuardController>
{
    public void Enter(GuardController owner)
    {
        owner.Agent.ResetPath();
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision.IsFullyDetected)
        {
            owner.ChangeToChase();
            return;
        }

        if (!owner.Vision.IsPartiallyDetected)
        {
            owner.ReturnFromSuspicious();
            return;
        }

        if (owner.Vision.IsPlayerVisible)
        {
            LookAtPlayer(owner);
        }
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