using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MultiMeshCut
{
    public bool Complete { private set; get; }

    private CancellationTokenSource _cts = new();

    public void Cut(BreakableObject[] breakables)
    {
    }

    private async UniTask CutAsync(BreakableObject[] breakables, CancellationToken ct, NativePlane blade,
        int batchCount = 32)
    {
        Mesh[] mesh = new Mesh[breakables.Length];
        MultiCutContext context = new MultiCutContext(breakables.Length);

        //MeshDataArrayを取得
        for (int i = 0; i < breakables.Length; i++)
        {
            mesh[i] = breakables[i].Mesh;
        }

        context.BaseMeshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

        //合計頂点数等取得
        int totalVerticesCount = 0;
        int maxVerticesCount = 0;
        for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
        {
            var vertexCount = context.BaseMeshDataArray[i].vertexCount;
            totalVerticesCount += vertexCount;
            if (vertexCount > maxVerticesCount)
            {
                maxVerticesCount = vertexCount;
            }
        }

        //ベースのデータを保持する配列を初期化
        context.BaseVertices =
            new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.Blades =
            new(context.BaseMeshDataArray.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.ObjectIndex =
            new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        int startIndex = 0;
        NativeList<float3> vertexAndNormalBuffer = new NativeList<float3>(maxVerticesCount, Allocator.Temp);
        NativeList<float2> uvBuffer = new NativeList<float2>(maxVerticesCount, Allocator.Temp);

        //オブジェクト毎にループする初期化を行う
        for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
        {
            #region Base頂点配列初期化

            var data = context.BaseMeshDataArray[i];
            vertexAndNormalBuffer.ResizeUninitialized(data.vertexCount);
            //NativeSliceには直接書き込めない
            data.GetVertices(vertexAndNormalBuffer.AsArray().Reinterpret<Vector3>());
            context.BaseVertices.Slice(startIndex, data.vertexCount).CopyFrom(vertexAndNormalBuffer.AsArray());
            data.GetNormals(vertexAndNormalBuffer.AsArray().Reinterpret<Vector3>());
            context.BaseNormals.Slice(startIndex, data.vertexCount).CopyFrom(vertexAndNormalBuffer.AsArray());
            data.GetUVs(0, vertexAndNormalBuffer.AsArray().Reinterpret<Vector2>());
            context.BaseUvs.Slice(startIndex, data.vertexCount).CopyFrom(uvBuffer.AsArray());

            #endregion

            #region Blades初期化

            //Bladeと頂点の対応をとるための配列
            for (int j = 0; j < data.vertexCount; j++)
            {
                context.ObjectIndex[startIndex + j] = i;
            }

            //オブジェクトごとにBladeの位置と回転、スケールにオフセットを掛ける
            var cutObject = breakables[i].gameObject;
            quaternion invRot = math.inverse(cutObject.transform.rotation);
            float3 reciprocal = math.rcp(cutObject.transform.localScale);
            float3 position = blade.Position - (float3)cutObject.transform.position;
            position = math.mul(invRot, position);
            position *= reciprocal;
            float3 normal = math.mul(invRot, blade.Normal);
            normal *= reciprocal;
            context.Blades[i] = new NativePlane(position, normal);

            #endregion

            context.StartIndex[i] = startIndex;
            context.Length[i] = data.vertexCount;

            //次ループの結合頂点配列の開始インデックスとして扱える
            startIndex += data.vertexCount;
        }

        vertexAndNormalBuffer.Dispose();
        uvBuffer.Dispose();

        #region 頂点のサイドを取得

        var vertexGetSideJob = new VertexGetSideJob
        {
            Vertices = context.BaseVertices,
            BladeIndex = context.ObjectIndex,
            Blades = context.Blades,
            VertexSides = context.BaseVertexSide
        };

        JobHandle vertexGetSideHandle = vertexGetSideJob.Schedule(context.BaseVertices.Length, batchCount);
        vertexGetSideHandle.Complete();

        #endregion

        #region 左右分け

        List<BurstBreakMesh> breakMeshes = new();
        List<int> triangleObjectTable = new();
        context.CutFaces = new();

        var vertices = context.BaseVertices;
        var normals = context.BaseNormals;
        var uvs = context.BaseUvs;

        //オブジェクト数分ループ
        for (int objIndex = 0; objIndex < context.BaseMeshDataArray.Length; objIndex++)
        {
            var objectStartIndex = context.StartIndex;
            var meshData = context.BaseMeshDataArray[objIndex];
            var triangles = meshData.GetIndexData<int>();
            BurstBreakMesh frontSide = new BurstBreakMesh(meshData.vertexCount);
            BurstBreakMesh backSide = new BurstBreakMesh(meshData.vertexCount);

            //サブメッシュ数分ループ
            for (int submesh = 0; submesh < meshData.subMeshCount; submesh++)
            {
                SubMeshDescriptor subMeshDesc = meshData.GetSubMesh(submesh);
                int start = subMeshDesc.indexStart;
                int count = subMeshDesc.indexCount;

                var indexData = triangles.GetSubArray(start, count);

                //三角形ごとにループ
                for (int i = 0; i < indexData.Length; i += 3)
                {
                    //ここで取得できるのはオブジェクトごとのインデックス番号なので、開始位置でオフセットを書ける
                    var p1 = indexData[i + 0] + objectStartIndex[objIndex];
                    var p2 = indexData[i + 1] + objectStartIndex[objIndex];
                    var p3 = indexData[i + 2] + objectStartIndex[objIndex];

                    int result =
                        (1 << context.BaseVertexSide[p1]) |
                        (1 << context.BaseVertexSide[p2]) |
                        (1 << context.BaseVertexSide[p3]);

                    switch (result)
                    {
                        case 0: //0なら裏側
                            backSide.AddTriangleLegacyIndex(
                                p1, p2, p3,
                                vertices[p1], vertices[p2], vertices[p3],
                                normals[p1], normals[p2], normals[p3],
                                uvs[p1], uvs[p2], uvs[p3],
                                submesh);
                            break;
                        case 7:
                            frontSide.AddTriangleLegacyIndex(
                                p1, p2, p3,
                                vertices[p1], vertices[p2], vertices[p3],
                                normals[p1], normals[p2], normals[p3],
                                uvs[p1], uvs[p2], uvs[p3],
                                submesh);
                            break;
                        default:
                            triangleObjectTable.Add(objIndex);
                            context.CutFaces.Add(new(p1, p2, p3));
                            context.CutStatus.Add(result);
                            break;
                    }
                }
            }
        }

        #endregion

        context.Dispose();
    }
}