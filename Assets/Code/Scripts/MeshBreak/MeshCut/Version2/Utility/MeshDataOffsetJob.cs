using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MeshDataOffsetJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> AllVertex;
    [ReadOnly] public NativeArray<int> ObjectIds;
    [ReadOnly] public NativeArray<float4x4> ObjectMatrices;

    public NativeArray<float3> WorldPosVertexPositions;

    public void Execute(int index)
    {
        var localPos = new float4(AllVertex[index], 1f);
        var matrix = ObjectMatrices[ObjectIds[index]];

        var worldPos = math.mul(matrix, localPos);

        WorldPosVertexPositions[index] = worldPos.xyz;
    }
}

public struct NativeTransform
{
    public float3 Position;
    public float4 Rotation;
    public float3 Scale;

    public NativeTransform(float3 position, float3 scale, float4 rotation)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
    }
}