using Unity.Collections;
using Unity.Mathematics;

public struct NativeEditMeshData
{
    /// <summary> 各配列の開始地点、長さ、どのオブジェクトに属しているかを保持。 </summary>
    public NativeArray<int3> ListStartLengthID;

    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeArray<float2> Uvs;

    /// <summary> 各三角形配列の開始地点、長さ、どのオブジェクトに属しているかを保持 </summary>
    public NativeArray<int3> TrianglesStartLengthID;

    public NativeArray<SubmeshTriangle> Triangles;
}