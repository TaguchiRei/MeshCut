using MeshBreak;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public static class MeshDataSupport
{
    /// <summary>
    /// メッシュ情報をNativeArrayの形で取得できるメソッドです
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="vertices"></param>
    /// <param name="normals"></param>
    /// <param name="uvs"></param>
    public static void ReadMeshDataSafely(
        Mesh mesh,
        NativeArray<float3> vertices,
        NativeArray<float3> normals,
        NativeArray<float2> uvs,
        NativeParallelMultiHashMap<int, int3> subIndices)
    {
        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        meshDataArray[0].GetVertices(vertices.Reinterpret<Vector3>());
        meshDataArray[0].GetNormals(normals.Reinterpret<Vector3>());
        meshDataArray[0].GetUVs(0, uvs.Reinterpret<Vector2>());
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var subMesh = mesh.GetTriangles(i);
            for (int j = 0; j < subMesh.Length; j += 3)
            {
                subIndices.Add(i, subMesh[j]);
            }
        }
    }
    public static Mesh ToMesh(NativeBreakMeshDataL nativeBreakMeshData, string meshName = "mesh")
    {
        Mesh mesh = new()
        {
            name = meshName
        };

        if (nativeBreakMeshData.Vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(nativeBreakMeshData.Vertices.AsArray());
        mesh.SetNormals(nativeBreakMeshData.Normals.AsArray());
        mesh.SetUVs(0, nativeBreakMeshData.Uvs.AsArray());
        int subMeshCount = nativeBreakMeshData.SubIndices.Count() + 1;

        for (int i = 0; i < subMeshCount; i++)
        {
            using NativeArray<int3> triangles = nativeBreakMeshData.SubIndices.GetElements(i, Allocator.TempJob);

            if (triangles.Length == 0) continue;
            NativeArray<int> indices = triangles.Reinterpret<int>(UnsafeUtility.SizeOf<int3>());
            mesh.SetIndices(indices, MeshTopology.Triangles, i);
        }
        mesh.RecalculateBounds();
        return mesh;
    }
    
    public static Mesh ToMesh(BreakMeshData breakMeshData, string meshName = "mesh")
    {
        Mesh mesh = new()
        {
            name = meshName
        };

        if (breakMeshData.Vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(breakMeshData.Vertices);
        mesh.SetNormals(breakMeshData.Normals);
        mesh.SetUVs(0, breakMeshData.Uvs);
        mesh.subMeshCount = breakMeshData.SubIndices.Count;
        for (int i = 0; i < breakMeshData.SubIndices.Count; i++)
        {
            mesh.SetIndices(breakMeshData.SubIndices[i], MeshTopology.Triangles, i);
        }

        return mesh;
    }
}