using UnityEngine;

public class GuardInvestigateState : IState<GuardController>
{
    private float waitTimer;
    private float blockLookYaw;

    private bool arrived;
    private bool originalUpdateRotation;

    public void Enter(GuardController owner)
    {
        originalUpdateRotation = owner.Agent.updateRotation;

        owner.SetMoveSpeed(owner.Data.investigateSpeed);

        UpdateTarget(owner);
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

            owner.ReportDirectorSupportArrived();

            if (owner.IsDirectorBlocking)
            {
                Vector3 lookDirection = owner.DirectorBlockLookPosition - owner.transform.position;
                lookDirection.y = 0f;

                blockLookYaw = lookDirection.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lookDirection).eulerAngles.y : owner.transform.eulerAngles.y;

                owner.Agent.updateRotation = false;

                Debug.Log($"[Director] Block watching: {owner.name}");
            }
        }

        waitTimer += Time.deltaTime;

        if (owner.IsDirectorBlocking)
        {
            float targetYaw = blockLookYaw + Mathf.Sin(waitTimer * 1.5f) * 50f;
            Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);

            owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, targetRotation, owner.Agent.angularSpeed * Time.deltaTime);
        }

        float waitDuration = owner.IsDirectorBlocking ? owner.DirectorBlockWatchDuration : owner.InvestigateDuration;

        if (waitTimer >= waitDuration)
        {
            owner.ChangeToPatrol();
        }
    }

    public void Exit(GuardController owner)
    {
        owner.Agent.updateRotation = originalUpdateRotation;

        waitTimer = 0f;
        arrived = false;
    }

    public void UpdateTarget(GuardController owner)
    {
        waitTimer = 0f;
        arrived = false;

        owner.Agent.updateRotation = originalUpdateRotation;
        owner.Agent.SetDestination(owner.InvestigatePos);
    }
}