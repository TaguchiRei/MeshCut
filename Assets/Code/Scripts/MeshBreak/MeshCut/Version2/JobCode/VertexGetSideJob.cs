using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VertexGetSideJob : IJobParallelFor
{
    public NativeArray<float3> Vertices;
    public NativeArray<NativePlane> Blades;

    public NativeArray<int> VerticesSide;

    public void Execute(int index)
    {
        for (int i = 0; i < Blades.Length; i++)
        {
            VerticesSide[index] = math.dot(Vertices[index] - Blades[i].Position, Blades[i].Normal) > 0.0f ? 1 : 0;
        }
    }
}