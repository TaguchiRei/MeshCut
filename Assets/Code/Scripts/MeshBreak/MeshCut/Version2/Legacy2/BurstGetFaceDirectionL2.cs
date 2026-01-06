/*
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct BurstGetFaceDirectionL2 : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> VerticesSide;
    public NativeMeshDataL2 BaseMesh;
    public int Quantize;

    public NativeMeshDataParallel FrontSideMesh;
    public NativeMeshDataParallel BackSideMesh;
    public NativeList<NativeTriangleDetailData> OverlapFrontDominant;
    public NativeList<NativeTriangleDetailData> OverlapBackDominant;

    /// <summary>
    /// サブメッシュ配列の長さ分実行させる
    /// </summary>
    /// <param name="index"></param>
    public void Execute(int index)
    {
        var submeshData = BaseMesh.SubMesh[index];

        var p0 = submeshData.Index0;
        var p1 = submeshData.Index1;
        var p2 = submeshData.Index2;

        NativeVertexData v0 = new NativeVertexData
        {
            Vertex = BaseMesh.Vertices[p0],
            Normal = BaseMesh.Normals[p0],
            Uv = BaseMesh.Uvs[p0],
        };
        NativeVertexData v1 = new NativeVertexData
        {
            Vertex = BaseMesh.Vertices[p1],
            Normal = BaseMesh.Normals[p1],
            Uv = BaseMesh.Uvs[p1],
        };
        NativeVertexData v2 = new NativeVertexData
        {
            Vertex = BaseMesh.Vertices[p2],
            Normal = BaseMesh.Normals[p2],
            Uv = BaseMesh.Uvs[p2],
        };

        var count = VerticesSide[p0] * 100 + VerticesSide[p1] * 10 + VerticesSide[p2];

        switch (count)
        {
            case 0:
                BackSideMesh.GetParallelWriter().AddTriangle(v0, v1, v2, submeshData.SubmeshId, Quantize);
                break;
            case 111:
                FrontSideMesh.GetParallelWriter().AddTriangle(v0, v1, v2, submeshData.SubmeshId, Quantize);
                break;
            //以下孤立している頂点が表側の時
            case 100:
                OverlapBackDominant.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 0));
                break;
            case 010:
                OverlapBackDominant.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 1));
                break;
            case 001:
                OverlapBackDominant.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 2));
                break;
            //以下孤立している側が裏側の時
            case 011:
                OverlapFrontDominant.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 0));
                break;
            case 101:
                OverlapFrontDominant.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 1));
                break;
            default: //case110
                OverlapFrontDominant.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 2));
                break;
        }
    }

    [BurstCompile]
    public static void CalculateFaceDirectionDirect(
        in NativeMeshDataL2 baseMesh,
        [ReadOnly] NativeArray<int> verticesSide,
        int quantize,
        NativeMeshDataParallel.ParallelWriter frontSideMesh,
        NativeMeshDataParallel.ParallelWriter backSideMesh,
        NativeList<NativeTriangleDetailData>.ParallelWriter overlapFront,
        NativeList<NativeTriangleDetailData>.ParallelWriter overlapBack)
    {
        // 全三角形をループ
        for (int i = 0; i < baseMesh.SubMesh.Length; i++)
        {
            var submeshData = baseMesh.SubMesh[i];

            // 頂点情報の取得（ここはJobと同じ）
            var p0 = submeshData.Index0;
            var p1 = submeshData.Index1;
            var p2 = submeshData.Index2;

            // 計算コスト削減のため、Sideだけ先に評価
            var s0 = verticesSide[p0];
            var s1 = verticesSide[p1];
            var s2 = verticesSide[p2];
            var count = s0 * 100 + s1 * 10 + s2;

            // 共通の頂点取得用ローカル関数
            NativeVertexData v0 = new NativeVertexData
                { Vertex = baseMesh.Vertices[p0], Normal = baseMesh.Normals[p0], Uv = baseMesh.Uvs[p0] };
            NativeVertexData v1 = new NativeVertexData
                { Vertex = baseMesh.Vertices[p1], Normal = baseMesh.Normals[p1], Uv = baseMesh.Uvs[p1] };
            NativeVertexData v2 = new NativeVertexData
                { Vertex = baseMesh.Vertices[p2], Normal = baseMesh.Normals[p2], Uv = baseMesh.Uvs[p2] };

            switch (count)
            {
                case 0:
                    backSideMesh.AddTriangle(v0, v1, v2, submeshData.SubmeshId, quantize);
                    break;
                case 111:
                    frontSideMesh.AddTriangle(v0, v1, v2, submeshData.SubmeshId, quantize);
                    break;
                // 表が孤立（裏が多い）
                case 100: overlapBack.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 0)); break;
                case 010: overlapBack.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 1)); break;
                case 001: overlapBack.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 2)); break;
                // 裏が孤立（表が多い）
                case 011: overlapFront.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 0)); break;
                case 101: overlapFront.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 1)); break;
                default: overlapFront.AddNoResize(new(v0, v1, v2, submeshData.SubmeshId, 2)); break;
            }
        }
    }
}*/