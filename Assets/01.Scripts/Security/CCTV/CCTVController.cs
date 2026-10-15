
using UnityEngine;

public class CCTVController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CCTVData cctvData;

    [Header("Vision")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Debug")]
    [SerializeField] private float detection;
    [SerializeField] private bool isPlayerVisible;

    private Transform player;
    private CharacterController playerController;
    private GuardController[] guards;

    private bool isDetectionEnabled = true;
    private bool isAlertSent;

    public bool IsDetectionEnabled => isDetectionEnabled;

    private void Start()
    {
        detection = 0f;
        isPlayerVisible = false;
        isAlertSent = false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerController = playerObject.GetComponent<CharacterController>();
        }

        guards = FindObjectsByType<GuardController>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (!isDetectionEnabled || cctvData == null)
        {
            return;
        }

        isPlayerVisible = CheckVision();
        UpdateDetection();

        if (detection >= cctvData.detectionThreshold && !isAlertSent)
        {
            isAlertSent = true;
            AlertGuards();
        }

        if (!isPlayerVisible && detection <= 0f)
        {
            isAlertSent = false;
        }
    }

    private bool CheckVision()
    {
        if (player == null || eyePoint == null)
        {
            return false;
        }

        Vector3 targetPos = playerController != null ? player.TransformPoint(playerController.center) : player.position;
        Vector3 dir = targetPos - eyePoint.position;
        float distance = dir.magnitude;

        if (distance > cctvData.visionRange)
        {
            return false;
        }

        float angle = Vector3.Angle(eyePoint.forward, dir);

        if (angle > cctvData.visionAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(eyePoint.position, dir.normalized, distance, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private void UpdateDetection()
    {
        if (isPlayerVisible)
        {
            detection += cctvData.detectionSpeed * Time.deltaTime;
        }
        else
        {
            detection -= cctvData.detectionDecreaseSpeed * Time.deltaTime;
        }

        detection = Mathf.Clamp(detection, 0f, cctvData.detectionThreshold);
    }

    private void AlertGuards()
    {
        if (player == null || guards == null)
        {
            return;
        }

        GuardController closestGuard = null;
        float closestDistance = float.MaxValue;

        foreach (GuardController guard in guards)
        {
            if (guard == null || !guard.isActiveAndEnabled)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, guard.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGuard = guard;
            }
        }

        if (closestGuard != null)
        {
            closestGuard.OnCCTVDetected(player.position);
        }
    }

    public void SetDetectionEnabled(bool value)
    {
        isDetectionEnabled = value;

        if (!isDetectionEnabled)
        {
            detection = 0f;
            isPlayerVisible = false;
            isAlertSent = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null || cctvData == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eyePoint.position, cctvData.visionRange);

        Vector3 leftDir = Quaternion.Euler(0f, -cctvData.visionAngle * 0.5f, 0f) * eyePoint.forward;
        Vector3 rightDir = Quaternion.Euler(0f, cctvData.visionAngle * 0.5f, 0f) * eyePoint.forward;

        Gizmos.DrawLine(eyePoint.position, eyePoint.position + leftDir * cctvData.visionRange);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + rightDir * cctvData.visionRange);
    }
}
