using UnityEngine;
using Unity.Collections;
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
        NativeArray<float2> uvs)
    {
        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        meshDataArray[0].GetVertices(vertices.Reinterpret<Vector3>());
        meshDataArray[0].GetNormals(normals.Reinterpret<Vector3>());
        meshDataArray[0].GetUVs(0, uvs.Reinterpret<Vector2>());
    }
}