/*
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public static class NativeMeshDataUtilityL2
{
    /// <summary>
    /// メッシュ情報をNativeArrayの形で取得できるメソッドです
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="vertices"></param>
    /// <param name="normals"></param>
    /// <param name="uvs"></param>
    /// <param name="submesh"></param>
    public static void ReadMeshDataSafely(
        Mesh mesh,
        NativeArray<float3> vertices,
        NativeArray<float3> normals,
        NativeArray<float2> uvs,
        NativeList<SubmeshTriangleData> submesh)
    {
        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        meshDataArray[0].GetVertices(vertices.Reinterpret<Vector3>());
        meshDataArray[0].GetNormals(normals.Reinterpret<Vector3>());
        meshDataArray[0].GetUVs(0, uvs.Reinterpret<Vector2>());


        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            SubMeshDescriptor submeshDesc = meshDataArray[0].GetSubMesh(i);
            int indexCount = submeshDesc.indexCount;

            NativeArray<int> indices = new(indexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            meshDataArray[0].GetIndices(indices, i);

            for (int j = 0; j < indexCount; j += 3)
            {
                var triangle = new SubmeshTriangleData
                {
                    Index0 = indices[j],
                    Index1 = indices[j + 1],
                    Index2 = indices[j + 2],
                    SubmeshId = i
                };
                submesh.Add(triangle);
            }

            indices.Dispose();
        }
    }
}
*/