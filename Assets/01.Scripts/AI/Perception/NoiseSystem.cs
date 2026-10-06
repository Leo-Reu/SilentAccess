using System;
using UnityEngine;

public static class NoiseSystem
{
    public static event Action<NoiseData> OnNoiseGenerated;

    public static void GenerateNoise(Vector3 position, float radius, NoiseType type)
    {
        NoiseData noiseData = new NoiseData(position, radius, type);
        OnNoiseGenerated?.Invoke(noiseData);
    }
}