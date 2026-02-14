using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VertexGetSideJobL : IJobParallelFor
{
    [ReadOnly] public NativeMultiArrayView<float3> BaseVertices;
    [ReadOnly] public NativeArray<NativePlane> Blades;

    public NativeArray<int> VerticesSide;

    public void Execute(int index)
    {
        int id = BaseVertices.GetArrayId(index);
        var blade = Blades[id];
        VerticesSide[index] = math.dot(BaseVertices[id, index] - blade.Position, blade.Normal) > 0f ? 1 : 0;
    }
}