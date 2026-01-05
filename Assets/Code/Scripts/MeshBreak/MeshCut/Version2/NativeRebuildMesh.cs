using Unity.Collections;
using Unity.Mathematics;

public struct NativeRebuildMesh
{
    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;
    public NativeList<SubmeshTriangleData> SubMesh;
}
