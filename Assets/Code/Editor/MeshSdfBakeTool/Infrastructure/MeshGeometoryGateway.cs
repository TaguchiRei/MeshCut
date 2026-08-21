using UnityEngine;
using UsefulMesh.Application;

namespace UsefulMesh.Infrastructure
{
    public class MeshGeometryGateway : IMeshGeometryGateway
    {
        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private Vector3[] _triangleWorldCache; // 計算用ローカル空間頂点配列

        public void SetTargetMesh(Mesh mesh)
        {
            _mesh = mesh;
            _vertices = mesh.vertices;
            _triangles = mesh.triangles;

            // 三角形ごとの独立した頂点キャッシュを構築
            _triangleWorldCache = new Vector3[_triangles.Length];
            for (int i = 0; i < _triangles.Length; i++)
            {
                _triangleWorldCache[i] = _vertices[_triangles[i]];
            }
        }

        public Vector3[] GetTriangles() => _triangleWorldCache;

        public Bounds GetBounds() => _mesh.bounds;

        /// <summary>
        /// 要件定義4: +X方向へレイを照射し、交差数が奇数なら内側、偶数なら外側
        /// </summary>
        public bool IsInside(Vector3 position)
        {
            int intersectionCount = 0;
            int triangleCount = _triangleWorldCache.Length / 3;

            for (int i = 0; i < triangleCount; i++)
            {
                Vector3 a = _triangleWorldCache[i * 3];
                Vector3 b = _triangleWorldCache[i * 3 + 1];
                Vector3 c = _triangleWorldCache[i * 3 + 2];

                if (RayTriangleIntersectPlusX(position, a, b, c))
                {
                    intersectionCount++;
                }
            }

            return (intersectionCount % 2) != 0;
        }

        /// <summary>
        /// 点から+X方向への半直線と三角形の交差判定 (Möller–Trumbore演算法の+X最適化版)
        /// </summary>
        private bool RayTriangleIntersectPlusX(Vector3 orig, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 dir = Vector3.right; // +X方向のレイ
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 pvec = Vector3.Cross(dir, edge2);
            float det = Vector3.Dot(edge1, pvec);

            // バックフェースクリップはしない（Closed Mesh前提のため両面カウントする）
            if (Mathf.Approximately(det, 0f)) return false;
            float invDet = 1f / det;

            Vector3 tvec = orig - v0;
            float u = Vector3.Dot(tvec, pvec) * invDet;
            if (u < 0f || u > 1f) return false;

            Vector3 qvec = Vector3.Cross(tvec, edge1);
            float v = Vector3.Dot(dir, qvec) * invDet;
            if (v < 0f || u + v > 1f) return false;

            float t = Vector3.Dot(edge2, qvec) * invDet;
            
            // t > 0 であれば+X方向の前方で交差している
            return t > 0.00001f;
        }
    }
}