using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BurstCutSchedulerL4
{
    private const int TRIANGLES_CLASSIFY = 8;

    private MeshCutContextL4 _heavyMeshCutContextL4;

    /// <summary>
    /// 軽量なメッシュを切断するためのコード
    /// </summary>
    /// <param name="blade"></param>
    /// <param name="cuttables"></param>
    /// <returns></returns>
    public MeshCutContextL4 SchedulingCutLight(NativePlane blade, CuttableObjectL[] cuttables, int batchCount)
    {
        Stopwatch st = Stopwatch.StartNew();

        // Contextの生成
        MeshCutContextL4 contextL4 = new MeshCutContextL4(cuttables.Length);

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
            contextL4.BaseVerticesArray[i] = new(meshData.vertexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            contextL4.BaseNormalsArray[i] = new(meshData.vertexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            contextL4.BaseUvsArray[i] =
                new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            meshData.GetVertices(contextL4.BaseVerticesArray[i].Reinterpret<Vector3>());
            meshData.GetNormals(contextL4.BaseNormalsArray[i].Reinterpret<Vector3>());
            meshData.GetUVs(0, contextL4.BaseUvsArray[i].Reinterpret<Vector2>());

            baseTriangles[i] = obj.Triangles;
            transforms[i] = obj.GetNativeTransform();
        }

        Debug.Log($"頂点群を配列にキャッシュ {st.ElapsedMilliseconds}ms");
        st.Restart();

        // ここで NativeMultiArrayView が作成される
        contextL4.InitializeContext(baseTriangles, transforms, Allocator.TempJob);

        for (int i = 0; i < cuttables.Length; i++)
        {
            quaternion invRot = math.inverse(contextL4.Transforms[i].Rotation);
            float3 reciprocal = math.rcp(contextL4.Transforms[i].Scale);

            float3 position = blade.Position - contextL4.Transforms[i].Position;
            position = math.mul(invRot, position);
            position *= reciprocal;

            float3 normal = math.mul(invRot, blade.Normal);
            normal *= reciprocal;

            contextL4.Blades[i] = new NativePlane(position, normal);
        }

        Debug.Log($"Context生成　{st.ElapsedMilliseconds}ms");

        #region 各頂点が面に対してどこにあるのかを調べる

        var vertexGetSideJob = new VertexGetSideJobL
        {
            BaseVertices = contextL4.BaseVertices,
            Blades = contextL4.Blades,
            VerticesSide = contextL4.VerticesSide,
        };

        JobHandle vertexGetSideHandle = vertexGetSideJob.Schedule(contextL4.BaseVertices.Length, batchCount);

        contextL4.CutJobHandle = vertexGetSideHandle;

        vertexGetSideHandle.Complete();
        Debug.Log($"VertexGetSide {st.ElapsedMilliseconds}ms");

        #endregion


        return contextL4;
    }
}