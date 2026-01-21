using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BurstCutScheduler
{
    private const int TRIANGLES_CLASSIFY = 8;

    private MeshCutContext heavyMeshCutContext;

    /// <summary>
    /// 軽量なメッシュを切断するためのコード
    /// </summary>
    /// <param name="blade"></param>
    /// <param name="mesh"></param>
    /// <param name="transforms"></param>
    /// <returns></returns>
    public MeshCutContext SchedulingCutLight(NativePlane blade, CuttableObject[] cuttables)
    {
        Stopwatch st = Stopwatch.StartNew();

        // Contextの生成
        MeshCutContext context = new MeshCutContext(cuttables.Length);

        int objectCount = cuttables.Length;

        // 各要素の NativeArray 参照を収集する配列を作成
        var baseTriangles = new NativeArray<NativeTriangle>[objectCount];
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
            context.BaseVerticesArray[i] = new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            context.BaseNormalsArray[i] = new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            context.BaseUvsArray[i] = new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            meshData.GetVertices(context.BaseVerticesArray[i].Reinterpret<Vector3>());
            meshData.GetNormals(context.BaseNormalsArray[i].Reinterpret<Vector3>());
            meshData.GetUVs(0, context.BaseUvsArray[i].Reinterpret<Vector2>());

            baseTriangles[i] = obj.Triangles;
            transforms[i] = obj.GetNativeTransform();
        }

        Debug.Log($"頂点群を配列にキャッシュ {st.ElapsedMilliseconds}ms");
        st.Restart();

        // ここで NativeMultiArrayView が作成される
        context.InitializeContext(baseTriangles, transforms, Allocator.TempJob);

        Debug.Log($"Context生成　{st.ElapsedMilliseconds}ms");

        return context;
    }
}