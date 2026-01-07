using Unity.Collections;
using Unity.Mathematics;

public struct NativeEditMeshData
{
    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeArray<float2> Uvs;
    public NativeArray<SubmeshTriangle> Triangles;

    public NativeArray<int> ObjectIDs;
}