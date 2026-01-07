using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct TriangleSideCountJob 
{
    public NativeArray<float3> Vertices;
    public NativeArray<TriangleData> Triangles;
}
