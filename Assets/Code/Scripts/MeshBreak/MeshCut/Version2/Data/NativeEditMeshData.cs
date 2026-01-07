using Unity.Collections;
using Unity.Mathematics;

public struct NativeEditMeshData
{
    /// <summary> 各配列の開始地点、長さ、どのオブジェクトに属しているかを保持。 </summary>
    public NativeList<int3> ListStartLengthID;

    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;

    /// <summary> 各三角形配列の開始地点、長さ、どのオブジェクトに属しているかを保持 </summary>
    public NativeList<int3> TrianglesStartLengthID;

    public NativeList<SubmeshTriangle> Triangles;
}