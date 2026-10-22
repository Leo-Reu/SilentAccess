using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CCTVRangeVisualizer : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform eyePoint;

    [Header("Range")]
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private int segmentCount = 20;

    [Header("Option")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool showRange = true;

    [Header("Display")]
    [SerializeField] private float groundY = 0.05f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
    }

    private void LateUpdate()
    {
        if (!showRange || eyePoint == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        DrawRange();
    }

    private void DrawRange()
    {
        lineRenderer.positionCount = segmentCount + 3;

        Vector3 origin = eyePoint.position;
        origin.y = groundY;

        Vector3 forward = eyePoint.forward;
        forward.y = 0f;
        forward.Normalize();

        lineRenderer.SetPosition(0, origin);

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float angle = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, t);

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * forward;

            Vector3 endPoint = origin + direction * viewDistance;

            if (Physics.Raycast(eyePoint.position, direction, out RaycastHit hit, viewDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                endPoint = origin + direction * hit.distance;
            }

            lineRenderer.SetPosition(i + 1, endPoint);
        }

        lineRenderer.SetPosition(segmentCount + 2, origin);
    }

    public void SetVisible(bool value)
    {
        showRange = value;
    }
}