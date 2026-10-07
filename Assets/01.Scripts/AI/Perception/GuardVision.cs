using UnityEngine;

public class GuardVision : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GuardData guardData;

    [Header("Vision")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Debug")]
    [SerializeField] private float detection;
    [SerializeField] private bool isPlayerVisible;

    private Transform player;
    private CharacterController playerController;

    public float Detection => detection;
    public bool IsPlayerVisible => isPlayerVisible;
    public bool IsFullyDetected => guardData != null && detection >= guardData.detectionThreshold;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        playerController = playerObject.GetComponent<CharacterController>();
    }

    private void Update()
    {
        isPlayerVisible = CheckVision();
        UpdateDetection();
    }

    private bool CheckVision()
    {
        if (player == null || eyePoint == null || guardData == null)
        {
            return false;
        }

        Vector3 targetPos = playerController != null ? player.TransformPoint(playerController.center) : player.position;
        Vector3 dir = targetPos - eyePoint.position;

        float distance = dir.magnitude;

        if (distance > guardData.visionRange)
        {
            return false;
        }

        float angle = Vector3.Angle(eyePoint.forward, dir);

        if (angle > guardData.visionAngle * 0.5f)
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
            detection += guardData.detectionSpeed * Time.deltaTime;
        }
        else
        {
            detection -= guardData.detectionDecreaseSpeed * Time.deltaTime;
        }

        detection = Mathf.Clamp(detection, 0f, guardData.detectionThreshold);
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null || guardData == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(eyePoint.position, guardData.visionRange);

        Vector3 leftDir = Quaternion.Euler(0f, -guardData.visionAngle * 0.5f, 0f) * eyePoint.forward;
        Vector3 rightDir = Quaternion.Euler(0f, guardData.visionAngle * 0.5f, 0f) * eyePoint.forward;

        Gizmos.DrawLine(eyePoint.position, eyePoint.position + leftDir * guardData.visionRange);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + rightDir * guardData.visionRange);
    }
}