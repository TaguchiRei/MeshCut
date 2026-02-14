using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// メッシュの情報をキャッシュしておくためのクラス。Jobを呼ぶときに直接構造体として渡せないようにするためclassに
/// </summary>
public class NativeMeshDataL3 : IDisposable
{
    public readonly int HashCode;

    private NativeArray<float3> _vertices;
    private NativeArray<float3> _normals;
    private NativeArray<float2> _uvs;
    private NativeArray<SubmeshTriangleL3> _triangles;

    private Transform _transform;

    public NativeArray<float3> Vertices => _vertices;
    public NativeArray<float3> Normals => _normals;
    public NativeArray<float2> Uvs => _uvs;
    public NativeArray<SubmeshTriangleL3> Triangles => _triangles;
    public Transform Transform => _transform;

    public NativeMeshDataL3(Mesh mesh, Transform transform)
    {
        _vertices = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent);
        _normals = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent);
        _uvs = new NativeArray<float2>(mesh.vertexCount, Allocator.Persistent);
        _triangles = new NativeArray<SubmeshTriangleL3>(mesh.triangles.Length / 3, Allocator.Persistent);

        _transform = transform;

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
                        _triangles[index++] = new SubmeshTriangleL3
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

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < _vertices.Length; i += 3)
                hash = hash * 31 + _vertices[i].GetHashCode();
            for (int i = 0; i < _triangles.Length; i += 3)
                hash = hash * 31 + _triangles[i].GetHashCode();
            HashCode = hash;
        }
    }

    public NativeMeshDataL3(NativeMeshDataL3 editMeshDataL3)
    {
        _vertices = new NativeArray<float3>(editMeshDataL3.Vertices.Length, Allocator.Persistent);
        _normals = new NativeArray<float3>(editMeshDataL3.Normals.Length, Allocator.Persistent);
        _uvs = new NativeArray<float2>(editMeshDataL3.Uvs.Length, Allocator.Persistent);
        _triangles = new NativeArray<SubmeshTriangleL3>(editMeshDataL3.Triangles.Length, Allocator.Persistent);

        NativeArray<float3>.Copy(editMeshDataL3.Vertices, _vertices);
        NativeArray<float3>.Copy(editMeshDataL3.Normals, _normals);
        NativeArray<float2>.Copy(editMeshDataL3.Uvs, _uvs);
        NativeArray<SubmeshTriangleL3>.Copy(editMeshDataL3.Triangles, _triangles);

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < _vertices.Length; i++)
                hash = hash * 31 + _vertices[i].GetHashCode();
            for (int i = 0; i < _triangles.Length; i++)
                hash = hash * 31 + _triangles[i].GetHashCode();
            HashCode = hash;
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