using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class TestGetVertices : MonoBehaviour
{
    [SerializeField] private Mesh mesh;

    void Start()
    {
        if (mesh == null)
        {
            Debug.LogError("Mesh が設定されていません");
            return;
        }

        // MeshData を取得
        var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        var meshData = meshDataArray[0];

        // NativeArray にコピー
        var nativeVerts = new NativeArray<float3>(meshData.vertexCount * 2, Allocator.Temp);
        var nativeVertsSub = nativeVerts.GetSubArray(0, meshData.vertexCount);
        meshData.GetVertices(nativeVertsSub.Reinterpret<Vector3>());

        // 元の vertices と比較
        var originalVerts = mesh.vertices;
        bool allSame = true;

        for (int i = 0; i < meshData.vertexCount; i++)
        {
            if (Vector3.Distance(originalVerts[i], nativeVerts[i]) > 1e-6f)
            {
                allSame = false;
                Debug.LogWarning($"Vertex {i} mismatch. Original: {originalVerts[i]}, MeshData: {nativeVerts[i]}");
            }
        }

        if (allSame)
        {
            Debug.Log("すべての頂点が一致しました。");
        }

        // メモリ解放
        nativeVerts.Dispose();
        meshDataArray.Dispose();
    }
}