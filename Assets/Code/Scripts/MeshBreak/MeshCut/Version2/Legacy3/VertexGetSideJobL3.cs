using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VertexGetSideJobL3 : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Vertices;
    [ReadOnly] public NativeArray<NativePlane> Blades;
    [ReadOnly] public NativeArray<int> BladeIndex;

    [WriteOnly] public NativeArray<int> Results;

    public void Execute(int index)
    {
        var blade = Blades[BladeIndex[index]];
        Results[index] = math.dot(Vertices[index] - blade.Position, blade.Normal) > 0f ? 1 : 0;
    }
}