using UnityEngine;

[CreateAssetMenu(fileName = "GuardData", menuName = "Data/Guard Data")]
public class GuardData : ScriptableObject
{
    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float stoppingDistance = 0.2f;

    [Header("State")]
    public float patrolWaitTime = 1.5f;
    public float investigateTime = 3f;
}