using UnityEngine;

namespace UsefulMesh.Application
{
    /// <summary>
    /// UnityのMeshデータアクセスや幾何学判定（Raycast等）の抽象化
    /// </summary>
    public interface IMeshGeometryGateway
    {
        void SetTargetMesh(Mesh mesh);
        Vector3[] GetTriangles(); // 3つずつで1ポリゴンを成す頂点配列
        Bounds GetBounds();
        bool IsInside(Vector3 position); // 内外判定（+X方向へのレイキャスト）
    }
}