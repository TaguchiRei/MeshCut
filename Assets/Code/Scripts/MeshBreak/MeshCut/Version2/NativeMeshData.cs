using Unity.Collections;
using Unity.Mathematics;

public struct NativeMeshData
{
    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;
    public NativeList<SubmeshTriangleData> SubMesh;

    public NativeMeshData(
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

public struct NativePlane
{
    public float3 Position;
    public float3 Normal;

    /// <summary>
    /// 面の法線方向にあれば
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public readonly int GetSide(float3 position)
    {
        float d = math.dot(position - Position, Normal);
        return d > 0.0f ? 1 : 0;
        // return math.select(0, 1, d > 0);
    }
}