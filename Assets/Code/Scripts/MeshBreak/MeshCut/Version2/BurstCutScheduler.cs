using Unity.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

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
            maxVertices = Mathf.Max(maxVertices, mesh[i].vertexCount);
        }

        MeshCutContext context = new
        (
            verticesCount,
            objectCount,
            triangleCount,
            transforms,
            Allocator.TempJob
        );

        #region 配列初期化

        Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

        int start = 0;
        NativeList<float3> verticesAndNormalsBuffer = new(0, Allocator.Temp);
        NativeList<float2> uvBuffer = new(0, Allocator.Temp);
        verticesAndNormalsBuffer.ResizeUninitialized(maxVertices);
        uvBuffer.ResizeUninitialized(maxVertices);
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


        return context;
    }
}