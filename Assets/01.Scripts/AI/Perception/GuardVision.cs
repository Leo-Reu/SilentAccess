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
    private GuardController guardController;

    public float Detection => detection;
    public bool IsPlayerVisible => isPlayerVisible;
    public bool IsPartiallyDetected => guardData != null && detection >= guardData.suspiciousThreshold; // 부분적 감지
    public bool IsFullyDetected => guardData != null && detection >= guardData.detectionThreshold;      // 완전 감지
    public Transform Player => player;

    private void Awake()
    {
        guardController = GetComponent<GuardController>();
    }

    private void Start()
    {
        detection = 0f;
        isPlayerVisible = false;

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

        // Chase 상태에서는 확장된 시야 범위를 사용
        bool isChasing = guardController != null && guardController.IsChasing;

        float currentVisionRange = isChasing ? guardData.chaseVisionRange : guardData.visionRange;
        float currentVisionAngle = isChasing ? guardData.chaseVisionAngle : guardData.visionAngle;

        Vector3 targetPos = playerController != null ? player.TransformPoint(playerController.center) : player.position;
        Vector3 dir = targetPos - eyePoint.position;

        float distance = dir.magnitude;

        if (distance > currentVisionRange)
        {
            return false;
        }

        float angle = Vector3.Angle(eyePoint.forward, dir);

        if (angle > currentVisionAngle * 0.5f)
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

        GuardController guard = GetComponent<GuardController>();
        bool isChasing = guard != null && guard.IsChasing;

        float currentVisionRange = isChasing ? guardData.chaseVisionRange : guardData.visionRange;
        float currentVisionAngle = isChasing ? guardData.chaseVisionAngle : guardData.visionAngle;

        Gizmos.color = isChasing ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(eyePoint.position, currentVisionRange);

        Vector3 leftDir = Quaternion.Euler(0f, -currentVisionAngle * 0.5f, 0f) * eyePoint.forward;
        Vector3 rightDir = Quaternion.Euler(0f, currentVisionAngle * 0.5f, 0f) * eyePoint.forward;

        Gizmos.DrawLine(eyePoint.position, eyePoint.position + leftDir * currentVisionRange);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + rightDir * currentVisionRange);
    }
}