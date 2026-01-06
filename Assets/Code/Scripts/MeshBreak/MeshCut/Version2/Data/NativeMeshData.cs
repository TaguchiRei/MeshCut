using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// メッシュの情報をキャッシュしておくための構造体
/// </summary>
public struct NativeMeshData : IDisposable
{
    private NativeArray<float3> _vertices;
    private NativeArray<float3> _normals;
    private NativeArray<float2> _uvs;
    private NativeArray<SubmeshTriangle> _triangles;

    public NativeArray<float3> Vertices => _vertices;
    public NativeArray<float3> Normals => _normals;
    public NativeArray<float2> UVs => _uvs;
    public NativeArray<SubmeshTriangle> Triangles => _triangles;

    public NativeMeshData(Mesh mesh)
    {
        _vertices = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent);
        _normals = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent);
        _uvs = new NativeArray<float2>(mesh.vertexCount, Allocator.Persistent);
        _triangles = new NativeArray<SubmeshTriangle>(mesh.triangles.Length / 3, Allocator.Persistent);

        Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        try
        {
            var data = meshDataArray[0];
            data.GetVertices(_vertices.Reinterpret<Vector3>());
            data.GetNormals(_normals.Reinterpret<Vector3>());
            data.GetUVs(0, _uvs.Reinterpret<Vector2>());

            int index = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                SubMeshDescriptor submeshDesc = data.GetSubMesh(i);
                int indexCount = submeshDesc.indexCount;
                NativeArray<int> indices = new(indexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                try
                {
                    data.GetIndices(indices, i);
                    for (int j = 0; j < indexCount; j += 3)
                    {
                        _triangles[index++] = new SubmeshTriangle
                        {
                            Index0 = indices[j],
                            Index1 = indices[j + 1],
                            Index2 = indices[j + 2],
                            SubmeshIndex = i
                        };
                    }
                }
                finally
                {
                    indices.Dispose();
                }
            }
        }
        finally
        {
            meshDataArray.Dispose();
        }
    }

    public void Dispose()
    {
        if (_vertices.IsCreated) _vertices.Dispose();
        if (_normals.IsCreated) _normals.Dispose();
        if (_uvs.IsCreated) _uvs.Dispose();
        if (_triangles.IsCreated) _triangles.Dispose();
    }
}
