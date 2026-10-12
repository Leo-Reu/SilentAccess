using UnityEngine;

[CreateAssetMenu(fileName = "CCTVData", menuName = "Data/CCTV Data")]
public class CCTVData : ScriptableObject
{
    [Header("Vision")]
    public float visionRange = 15f;

    [Range(0f, 360f)]
    public float visionAngle = 80f;

    public float detectionSpeed = 0.8f;
    public float detectionDecreaseSpeed = 0.6f;
    public float detectionThreshold = 1f;

    [Header("Alert")]
    public float alertRadius = 20f;
}