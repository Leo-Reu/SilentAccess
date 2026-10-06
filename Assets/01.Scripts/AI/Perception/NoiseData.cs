using UnityEngine;

public enum NoiseType
{
    Footstep,
    ThrownObject
}

public struct NoiseData
{
    public Vector3 position;
    public float radius;
    public NoiseType type;

    public NoiseData(Vector3 position, float radius, NoiseType type)
    {
        this.position = position;
        this.radius = radius;
        this.type = type;
    }
}