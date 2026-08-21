using UnityEngine;

namespace UsefulMesh.Application
{
    /// <summary>
    /// 単一ボクセルのSDF計算ロジックを担当するドメインサービス
    /// </summary>
    public static class SDFVoxelCalculator
    {
        /// <summary>
        /// ボクセル中心点からポリゴン（三角形）への最短距離の2乗を計算
        /// </summary>
        public static float CalculateMinDistanceSq(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ca = a - c;

            Vector3 ap = p - a;
            Vector3 bp = p - b;
            Vector3 cp = p - c;

            Vector3 nor = Vector3.Cross(ab, ca);

            // 各辺の外側チェック
            if (Vector3.Dot(Vector3.Cross(ab, nor), ap) > 0)
                return ProjectOnSegment(p, a, b);
            if (Vector3.Dot(Vector3.Cross(bc, nor), bp) > 0)
                return ProjectOnSegment(p, b, c);
            if (Vector3.Dot(Vector3.Cross(ca, nor), cp) > 0)
                return ProjectOnSegment(p, c, a);

            // 三角形の内側（平面への投影距離）
            float d = Vector3.Dot(nor, ap);
            return (d * d) / nor.sqrMagnitude;
        }

        private static float ProjectOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            Vector3 ap = p - a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            return (ap - ab * t).sqrMagnitude;
        }

        /// <summary>
        /// ボクセルインデックス(x,y,z)から、Bounds空間内のローカル座標を算出
        /// </summary>
        public static Vector3 GetVoxelCenter(int x, int y, int z, Vector3 min, Vector3 max, int resolution)
        {
            float tx = (x + 0.5f) / resolution;
            float ty = (y + 0.5f) / resolution;
            float tz = (z + 0.5f) / resolution;

            return new Vector3(
                Mathf.Lerp(min.x, max.x, tx),
                Mathf.Lerp(min.y, max.y, ty),
                Mathf.Lerp(min.z, max.z, tz)
            );
        }
    }
}