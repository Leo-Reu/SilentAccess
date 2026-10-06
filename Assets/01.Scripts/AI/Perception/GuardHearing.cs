using System;
using UnityEngine;

public class GuardHearing : MonoBehaviour
{
    public event Action<NoiseData> OnNoiseHeard;

    private void OnEnable()
    {
        NoiseSystem.OnNoiseGenerated += CheckNoise;
    }

    private void OnDisable()
    {
        NoiseSystem.OnNoiseGenerated -= CheckNoise;
    }

    private void CheckNoise(NoiseData noiseData)
    {
        float distance = Vector3.Distance(transform.position, noiseData.position);

        if (distance > noiseData.radius)
        {
            return;
        }

        OnNoiseHeard?.Invoke(noiseData);

#if UNITY_EDITOR
        Debug.Log($"{name} heard {noiseData.type} noise / Distance: {distance:F1}");
#endif
    }
}