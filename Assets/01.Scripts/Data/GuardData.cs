using UnityEngine;

[CreateAssetMenu(fileName = "GuardData", menuName = "Data/Guard Data")]
public class GuardData : ScriptableObject
{
    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed = 4.5f;
    public float stoppingDistance = 0.2f;

    [Header("State")]
    public float patrolWaitTime = 1.5f;
    public float investigateTime = 3f;

    [Header("Vision")]
    public float visionRange = 12f;

    [Range(0f, 360f)]
    public float visionAngle = 90f;

    public float detectionSpeed = 0.7f;
    public float detectionDecreaseSpeed = 0.4f;
    public float suspiciousThreshold = 0.15f;
    public float detectionThreshold = 1f;
}