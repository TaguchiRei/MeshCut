using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct VertexGetSideJob : IJobParallelFor
{
    [ReadOnly] NativeArray<float3> Vertices;
    [ReadOnly] NativeArray<NativePlane> Blades;

    [WriteOnly] public NativeArray<int> Results;

    public void Execute(int index)
    {
        
    }
}