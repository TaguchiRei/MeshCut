using Unity.Mathematics;
using UnityEngine;

public struct TransformData
{
    public float3 Position;
    public quaternion Rotation;
    public float3 Scale;

    public TransformData(Transform t)
    {
        Position = t.position;
        Rotation = t.rotation;
        Scale = t.localScale;
    }
}