using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BurstCutSchedulerL
{
    private const int TRIANGLES_CLASSIFY = 8;

    private MeshCutContextL _heavyMeshCutContextL;

    /// <summary>
    /// 軽量なメッシュを切断するためのコード
    /// </summary>
    /// <param name="blade"></param>
    /// <param name="cuttables"></param>
    /// <returns></returns>
    public MeshCutContextL SchedulingCutLight(NativePlane blade, CuttableObjectL[] cuttables, int batchCount)
    {
        Stopwatch st = Stopwatch.StartNew();

        // Contextの生成
        MeshCutContextL contextL = new MeshCutContextL(cuttables.Length);

        int objectCount = cuttables.Length;

        // 各要素の NativeArray 参照を収集する配列を作成
        var baseTriangles = new NativeArray<NativeTriangleL>[objectCount];
        var transforms = new NativeTransform[objectCount];

        Mesh[] meshes = new Mesh[objectCount];
        for (int i = 0; i < objectCount; i++)
        {
            meshes[i] = cuttables[i].mesh;
        }

        Debug.Log($"キャッシュ用配列作成 {st.ElapsedMilliseconds}ms");
        st.Restart();

        Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(meshes);

        for (int i = 0; i < objectCount; i++)
        {
            var obj = cuttables[i];

            var meshData = meshDataArray[i];
            contextL.BaseVerticesArray[i] = new(meshData.vertexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            contextL.BaseNormalsArray[i] = new(meshData.vertexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            contextL.BaseUvsArray[i] =
                new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            meshData.GetVertices(contextL.BaseVerticesArray[i].Reinterpret<Vector3>());
            meshData.GetNormals(contextL.BaseNormalsArray[i].Reinterpret<Vector3>());
            meshData.GetUVs(0, contextL.BaseUvsArray[i].Reinterpret<Vector2>());

            baseTriangles[i] = obj.Triangles;
            transforms[i] = obj.GetNativeTransform();
        }

        Debug.Log($"頂点群を配列にキャッシュ {st.ElapsedMilliseconds}ms");
        st.Restart();

        // ここで NativeMultiArrayView が作成される
        contextL.InitializeContext(baseTriangles, transforms, Allocator.TempJob);

        for (int i = 0; i < cuttables.Length; i++)
        {
            quaternion invRot = math.inverse(contextL.Transforms[i].Rotation);
            float3 reciprocal = math.rcp(contextL.Transforms[i].Scale);

            float3 position = blade.Position - contextL.Transforms[i].Position;
            position = math.mul(invRot, position);
            position *= reciprocal;

            float3 normal = math.mul(invRot, blade.Normal);
            normal *= reciprocal;

            contextL.Blades[i] = new NativePlane(position, normal);
        }

        Debug.Log($"Context生成　{st.ElapsedMilliseconds}ms");

        #region 各頂点が面に対してどこにあるのかを調べる

        var vertexGetSideJob = new VertexGetSideJobL
        {
            BaseVertices = contextL.BaseVertices,
            Blades = contextL.Blades,
            VerticesSide = contextL.VerticesSide,
        };

        JobHandle vertexGetSideHandle = vertexGetSideJob.Schedule(contextL.BaseVertices.Length, batchCount);

        contextL.CutJobHandle = vertexGetSideHandle;

        vertexGetSideHandle.Complete();
        Debug.Log($"VertexGetSide {st.ElapsedMilliseconds}ms");

        #endregion


        return contextL;
    }
}