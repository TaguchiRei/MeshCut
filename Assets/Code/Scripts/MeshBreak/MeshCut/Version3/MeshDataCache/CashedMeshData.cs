using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    /// <summary>
    /// ステージ開始時にUnityから取得したメッシュデータのキャッシュ
    /// </summary>
    public class CachedMeshData
    {
        public readonly int ModelId;
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector2[] UVs;
        public readonly int[][] SubMeshTriangles;
        public readonly int VertexCount;

        public CachedMeshData(int modelId, Mesh mesh)
        {
            ModelId  = modelId;
            Vertices = mesh.vertices;
            Normals  = mesh.normals;
            UVs      = mesh.uv;

            int subCount = mesh.subMeshCount;
            SubMeshTriangles = new int[subCount][];
            for (int i = 0; i < subCount; i++)
                SubMeshTriangles[i] = mesh.GetTriangles(i);

            VertexCount = mesh.vertexCount;
        }
    }
}