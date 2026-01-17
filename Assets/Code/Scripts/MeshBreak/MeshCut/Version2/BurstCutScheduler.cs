using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
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

        int objectCount = cuttables.Length;

        // 各要素の NativeArray 参照を収集する配列を作成
        var baseVertices = new NativeArray<float3>[objectCount];
        var baseNormals = new NativeArray<float3>[objectCount];
        var baseUvs = new NativeArray<float2>[objectCount];
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
            baseVertices[i] = new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseNormals[i] = new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseUvs[i] = new(meshData.vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            meshData.GetVertices(baseVertices[i].Reinterpret<Vector3>());
            meshData.GetNormals(baseNormals[i].Reinterpret<Vector3>());
            meshData.GetUVs(0, baseUvs[i].Reinterpret<Vector2>());

            baseTriangles[i] = obj.Triangles;
            transforms[i] = obj.GetNativeTransform();
        }

        Debug.Log($"頂点群を配列にキャッシュ {st.ElapsedMilliseconds}ms");
        st.Restart();


        // Context の初期化 (ここで NativeMultiArrayView が作成される)
        MeshCutContext context = new MeshCutContext
        (
            baseVertices,
            baseNormals,
            baseUvs,
            baseTriangles,
            transforms,
            Allocator.TempJob
        );

        Debug.Log($"Context生成　{st.ElapsedMilliseconds}ms");

        return context;
    }
}