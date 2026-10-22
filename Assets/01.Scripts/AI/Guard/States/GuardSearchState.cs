using UnityEngine;
using UnityEngine.AI;

public class GuardSearchState : IState<GuardController>
{
    private float searchTimer;
    private float pointWaitTimer;
    private float nextNoiseRetargetTime;
    private float lookYaw;

    private bool reachedCenter;
    private bool movingToPoint;
    private bool originalUpdateRotation;

    private int nextPointIndex;
    private Vector3 searchCenter;

    private static readonly Vector3[] searchDirections =
    {
        Vector3.forward,
        Vector3.right,
        Vector3.back,
        Vector3.left
    };

    public void Enter(GuardController owner)
    {
        owner.SetMoveSpeed(owner.Data.searchSpeed);

        searchTimer = 0f;
        pointWaitTimer = 0f;
        nextNoiseRetargetTime = 0f;

        reachedCenter = false;
        movingToPoint = false;
        nextPointIndex = 0;

        originalUpdateRotation = owner.Agent.updateRotation;

        searchCenter = owner.LastKnownPos;
        owner.Agent.SetDestination(searchCenter);
    }

    public void Update(GuardController owner)
    {
        if (owner.Vision.IsPlayerVisible)
        {
            owner.ChangeToChase();
            return;
        }

        if (!reachedCenter)
        {
            if (!HasArrived(owner))
            {
                return;
            }

            reachedCenter = true;
            BeginLookAround(owner);
        }

        searchTimer += Time.deltaTime;

        if (searchTimer >= owner.SearchDuration)
        {
            owner.ChangeToPatrol();
            return;
        }

        if (movingToPoint)
        {
            if (!HasArrived(owner))
            {
                return;
            }

            movingToPoint = false;
            BeginLookAround(owner);
            return;
        }

        pointWaitTimer += Time.deltaTime;

        float targetYaw = lookYaw + Mathf.Sin(pointWaitTimer * 4f) * 45f;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);

        owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, targetRotation, owner.Agent.angularSpeed * Time.deltaTime);

        if (pointWaitTimer < owner.Data.searchPointWaitTime)
        {
            return;
        }

        if (!MoveToNextSearchPoint(owner))
        {
            BeginLookAround(owner);
        }
    }

    public void Exit(GuardController owner)
    {
        owner.Agent.updateRotation = originalUpdateRotation;

        searchTimer = 0f;
        pointWaitTimer = 0f;
        reachedCenter = false;
        movingToPoint = false;
    }

    public void UpdateNoiseTarget(GuardController owner, Vector3 noisePos)
    {
        if (Time.time < nextNoiseRetargetTime)
        {
            return;
        }

        if (Vector3.Distance(searchCenter, noisePos) < owner.Data.noiseRetargetDistance)
        {
            return;
        }

        if (!NavMesh.SamplePosition(noisePos, out NavMeshHit hit, 2f, owner.Agent.areaMask))
        {
            return;
        }

        NavMeshPath path = new NavMeshPath();

        if (!owner.Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            return;
        }

        owner.Agent.updateRotation = originalUpdateRotation;

        if (!owner.Agent.SetDestination(hit.position))
        {
            return;
        }

        searchCenter = hit.position;
        reachedCenter = false;
        movingToPoint = false;

        searchTimer = 0f;
        pointWaitTimer = 0f;
        nextPointIndex = 0;

        nextNoiseRetargetTime = Time.time + owner.Data.noiseRetargetInterval;
    }

    private bool HasArrived(GuardController owner)
    {
        if (owner.Agent.pathPending)
        {
            return false;
        }

        if (!owner.Agent.hasPath)
        {
            return true;
        }

        return owner.Agent.remainingDistance <= owner.Agent.stoppingDistance + 0.15f;
    }

    private void BeginLookAround(GuardController owner)
    {
        owner.Agent.ResetPath();
        owner.Agent.updateRotation = false;

        lookYaw = owner.transform.eulerAngles.y;
        pointWaitTimer = 0f;
    }

    private bool MoveToNextSearchPoint(GuardController owner)
    {
        for (int i = 0; i < searchDirections.Length; i++)
        {
            Vector3 direction = searchDirections[nextPointIndex];
            nextPointIndex = (nextPointIndex + 1) % searchDirections.Length;

            Vector3 candidate = searchCenter + direction * owner.Data.searchRadius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, owner.Agent.areaMask))
            {
                continue;
            }

            if (Vector3.Distance(owner.transform.position, hit.position) < 1f)
            {
                continue;
            }

            NavMeshPath path = new NavMeshPath();

            if (!owner.Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            float pathDistance = 0f;

            for (int j = 1; j < path.corners.Length; j++)
            {
                pathDistance += Vector3.Distance(path.corners[j - 1], path.corners[j]);
            }

            if (pathDistance > owner.Data.searchRadius * 3.5f)
            {
                continue;
            }

            owner.Agent.updateRotation = originalUpdateRotation;

            if (!owner.Agent.SetDestination(hit.position))
            {
                continue;
            }

            movingToPoint = true;
            return true;
        }

        return false;
    }
}
