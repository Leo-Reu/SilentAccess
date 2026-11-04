using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CCTVRangeVisualizer : MonoBehaviour
{
    [Header("Range")]
    [SerializeField, Min(2)] private int segmentCount = 20;

    [Header("Option")]
    [SerializeField] private bool showRange = true;

    private CCTVData cctvData;
    private Transform eyePoint;
    private LayerMask obstacleMask;

    [Header("Display")]
    [SerializeField] private float groundY = 0.05f;

    private LineRenderer lineRenderer;
    public void Configure(CCTVData data, Transform eye, LayerMask obstacles)
    {
        cctvData = data;
        eyePoint = eye;
        obstacleMask = obstacles;
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
    }

    private void LateUpdate()
    {
        if (!showRange || eyePoint == null || cctvData == null)
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

        float viewDistance = cctvData.visionRange;
        float viewAngle = cctvData.visionAngle;

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