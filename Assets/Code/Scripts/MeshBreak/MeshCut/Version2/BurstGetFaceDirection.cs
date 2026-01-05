using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public struct BurstGetFaceDirection : IJobParallelFor
{
    [ReadOnly] public NativeMeshData BaseMesh;
    [ReadOnly] public NativeArray<int> VerticesSide;
    public int Quantize;

    public NativeMeshData.ParallelWriter FrontSideMesh;
    public NativeMeshData.ParallelWriter BackSideMesh;
    public NativeList<NativeTriangleDetailData>.ParallelWriter OverlapFrontDominant;
    public NativeList<NativeTriangleDetailData>.ParallelWriter OverlapBackDominant;

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
                BackSideMesh.AddTriangle(v0, v1, v2, submeshData.SubmeshId, Quantize);
                break;
            case 111:
                FrontSideMesh.AddTriangle(v0, v1, v2, submeshData.SubmeshId, Quantize);
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
}