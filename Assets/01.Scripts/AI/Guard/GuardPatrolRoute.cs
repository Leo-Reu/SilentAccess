using UnityEngine;

public class GuardPatrolRoute : MonoBehaviour
{
    [SerializeField] private Transform[] patrolPoints;

    public int Count => patrolPoints.Length;

    public Transform GetPoint(int index)
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return null;
        }

        return patrolPoints[index % patrolPoints.Length];
    }

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length < 2)
        {
            return;
        }

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform currentPoint = patrolPoints[i];
            Transform nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];

            if (currentPoint == null || nextPoint == null)
            {
                continue;
            }

            Gizmos.DrawSphere(currentPoint.position, 0.15f);
            Gizmos.DrawLine(currentPoint.position, nextPoint.position);
        }
    }
}