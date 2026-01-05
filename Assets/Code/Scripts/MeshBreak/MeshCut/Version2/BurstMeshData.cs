using Unity.Collections;
using Unity.Mathematics;

public struct BurstMeshData
{
    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;
    public NativeList<SubmeshTriangleData> SubMesh;

    public BurstMeshData(
        NativeList<float3> vertices, NativeList<float3> normals, NativeList<float2> uvs,
        NativeList<SubmeshTriangleData> subMesh)
    {
        Vertices = vertices;
        Normals = normals;
        Uvs = uvs;
        SubMesh = subMesh;
    }
}

public struct SubmeshTriangleData
{
    public float3 Position0;
    public float3 Position1;
    public float3 Position2;

    public int SubmeshId;
}