using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BurstCutScheduler
{
    public MeshCutContext SchedulingCut(NativePlane blade, Mesh[] mesh, NativeTransform[] transforms)
    {
        int verticesCount = 0;
        int objectCount = mesh.Length;
        int triangleCount = 0;
        int maxVertices = 0;
        for (int i = 0; i < mesh.Length; i++)
        {
            verticesCount += mesh[i].vertexCount;
            objectCount += mesh[i].vertexCount;
            triangleCount += mesh[i].triangles.Length / 3;
            maxVertices = Mathf.Max(maxVertices, mesh[i].vertexCount);
        }

        Stopwatch st = Stopwatch.StartNew();

        #region メモリ確保

        MeshCutContext context = new
        (
            verticesCount,
            objectCount,
            triangleCount,
            transforms,
            Allocator.Persistent
        );

        #endregion

        Debug.Log($"メモリ確保 {st.ElapsedMilliseconds}ms");
        st.Restart();

        #region 結合配列作成

        Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

        int start = 0;
        NativeList<float3> verticesAndNormalsBuffer = new(0, Allocator.TempJob);
        NativeList<float2> uvBuffer = new(0, Allocator.TempJob);
        verticesAndNormalsBuffer.ResizeUninitialized(maxVertices + 1);
        uvBuffer.ResizeUninitialized(maxVertices + 1);
        for (int i = 0; i < meshDataArray.Length; i++)
        {
            var data = meshDataArray[i];
            var vertsLength = data.vertexCount;
            verticesAndNormalsBuffer.ResizeUninitialized(vertsLength);
            uvBuffer.ResizeUninitialized(vertsLength);
            data.GetVertices(verticesAndNormalsBuffer.AsArray().Reinterpret<Vector3>());
            context.Vertices.Slice(start, vertsLength).CopyFrom(verticesAndNormalsBuffer.AsArray());
            data.GetNormals(verticesAndNormalsBuffer.AsArray().Reinterpret<Vector3>());
            context.Normals.Slice(start, vertsLength).CopyFrom(verticesAndNormalsBuffer.AsArray());
            data.GetUVs(0, uvBuffer.AsArray().Reinterpret<Vector2>());
            context.Uvs.Slice(start, vertsLength).CopyFrom(uvBuffer.AsArray());

            start += vertsLength;
        }

        verticesAndNormalsBuffer.Dispose();
        uvBuffer.Dispose();

        #endregion

        Debug.Log($"結合配列作成 {st.ElapsedMilliseconds}ms");
        st.Restart();

        #region 配列初期化Job

        Debug.Log($"{transforms.Length} {meshDataArray.Length} {context.Triangles.Length} {context.Blades.Length}");

        var arrayInitJob = new ArrayInitializeJob
        {
            Transforms = context.Transforms,
            MeshDataArray = meshDataArray,
            Blade = blade,
            Triangles = context.Triangles,
            LocalBlades = context.Blades,
            VerticesObjectIdList = context.VerticesObjectIdList
        };

        JobHandle arrayInitHandle = arrayInitJob.Schedule();

        #endregion

        Debug.Log($"配列初期化時間 {st.ElapsedMilliseconds}ms");

        context.CutJobHandle = arrayInitHandle;
        return context;
    }
}