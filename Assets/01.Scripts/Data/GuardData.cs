using UnityEngine;

[CreateAssetMenu(fileName = "GuardData", menuName = "Data/Guard Data")]
public class GuardData : ScriptableObject
{
    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed = 4.5f;
    public float searchSpeed = 2.5f;
    public float stoppingDistance = 0.2f;

    [Header("State")]
    public float patrolWaitTime = 1.5f;
    public float investigateTime = 3f;
    public float searchTime = 5f;
    public float lostSightTime = 1.5f;
    public float maxLostSightChaseTime = 5f;

    [Header("Noise Tracking")]
    public float noiseRetargetInterval = 1.2f;
    public float noiseRetargetDistance = 2.5f;

    [Header("Active Search")]
    public float searchRadius = 3f;
    public float searchPointWaitTime = 1.2f;

    [Header("Vision")]
    public float visionRange = 16f;

    [Range(0f, 360f)]
    public float visionAngle = 100f;

    [Header("Chase Vision")]
    public float chaseVisionRange = 23f;

    [Range(0f, 360f)]
    public float chaseVisionAngle = 160f;

    public float detectionSpeed = 0.7f;
    public float detectionDecreaseSpeed = 0.4f;
    public float suspiciousThreshold = 0.15f;
    public float detectionThreshold = 1f;
}