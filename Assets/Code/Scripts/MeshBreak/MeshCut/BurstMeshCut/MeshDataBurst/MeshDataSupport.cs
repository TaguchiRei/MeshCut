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
        NativeArray<float2> uvs,
        NativeParallelMultiHashMap<int, int> subIndices)
    {
        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        var data = meshDataArray[0];
        data.GetVertices(vertices.Reinterpret<Vector3>());
        data.GetNormals(normals.Reinterpret<Vector3>());
        data.GetUVs(0, uvs.Reinterpret<Vector2>());

        for (int i = 0; i < data.subMeshCount; i++)
        {
            // サブメッシュごとのインデックス数を取得
            int indexCount = data.GetSubMesh(i).indexCount;

            // 一時的な NativeArray としてインデックスバッファを参照（コピーが発生しない）
            using var indices = new NativeArray<int>(indexCount, Allocator.Temp);
            data.GetIndices(indices, i);

            // MultiHashMapに追加
            foreach (var vertIndex in indices)
            {
                subIndices.Add(i, vertIndex);
            }
        }
    }
}