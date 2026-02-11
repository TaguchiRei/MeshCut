using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
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
        MultiCutContext context = new MultiCutContext();

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
        NativeList<float3> vertexBuffer = new NativeList<float3>(maxVerticesCount, Allocator.Temp);

        //オブジェクト毎にループする初期化を行う
        for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
        {
            #region Base頂点配列初期化

            var data = context.BaseMeshDataArray[i];
            vertexBuffer.ResizeUninitialized(data.vertexCount);
            //NativeSliceには直接書き込めない
            data.GetVertices(vertexBuffer.AsArray().Reinterpret<Vector3>());
            context.BaseVertices.Slice(startIndex, data.vertexCount).CopyFrom(vertexBuffer.AsArray());

            #endregion

            #region 三角形配列取得

            

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

            //次ループの結合頂点配列の開始インデックスとして扱える
            startIndex += data.vertexCount;
        }

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

        BurstBreakMesh bbm = new();
        //オブジェクト数分ループ
        for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
        {
            var triangles =  context.Triangles[i];
            for (int j = 0; j < triangles.Length; j++)
            {
                
            }
        }

        context.Dispose();
    }
}