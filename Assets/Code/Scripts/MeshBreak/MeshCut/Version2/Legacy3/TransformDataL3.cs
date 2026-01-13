using Unity.Mathematics;
using UnityEngine;

public struct TransformDataL3
{
    public float3 Position;
    public quaternion Rotation;
    public float3 Scale;

    public TransformDataL3(Transform t)
    {
        Position = t.position;
        Rotation = t.rotation;
        Scale = t.localScale;
    }
}