using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TriangleGetSideJob : IJobParallelFor
{
    NativeArray<int3> ListStartLengthID;
    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeArray<float2> Uvs;
    public NativeArray<int3> TrianglesStartLengthID;
    public NativeArray<SubmeshTriangle> Triangles;

    public NativeArray<int> VerticesSpace;

    //必要数のVertexDataをnewして割り当てておく。長さは元の頂点数と同じ数にしておく
    public NativeArray<TriangleData> CutFaceToNotCutFaces;

    public void Execute(int index)
    {
        //三角形の各頂点のインデックスを取得するのに必要
        SubmeshTriangle triangle = Triangles[index];

        var v0 = new VertexData()
        {
            ObjectId = ListStartLengthID[triangle.Index0].z,
            SpaceId = VerticesSpace[triangle.Index0],
            SubmeshId = triangle.SubmeshIndex,
            Position = triangle.Index0,
            Normal = triangle.Index0,
            Uv = triangle.Index0,
        };

        var v1 = new VertexData()
        {
            ObjectId = ListStartLengthID[triangle.Index1].z,
            SpaceId = VerticesSpace[triangle.Index1],
            SubmeshId = triangle.SubmeshIndex,
            Position = triangle.Index1,
            Normal = triangle.Index1,
            Uv = triangle.Index1
        };

        var v2 = new VertexData()
        {
            ObjectId = ListStartLengthID[triangle.Index2].z,
            SpaceId = VerticesSpace[triangle.Index2],
            SubmeshId = triangle.SubmeshIndex,
            Position = triangle.Index2,
            Normal = triangle.Index2,
            Uv = triangle.Index2
        };

        // すべて同じであれば0になるビット演算で同じ面空間にいるかどうかを調べる
        int needCutBit = (v0.SpaceId == v1.SpaceId && v1.SpaceId == v2.SpaceId) ? 0 : 1;
        int writeIndex = index + needCutBit * Vertices.Length;
        CutFaceToNotCutFaces[writeIndex] = new TriangleData(v0, v1, v2);
    }
}