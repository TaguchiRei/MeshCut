using System;
using MeshBreak;
using UnityEngine;

namespace MeshBreak.MeshBooleanOperator
{
    public static class MeshCalculationSupport
    {
        /// <summary>
        /// 任意の三角形と任意の線分が交差しているかを調べる。
        /// </summary>
        /// <param name="triangle">三角形情報</param>
        /// <param name="start">開始地点</param>
        /// <param name="end">終了地点</param>
        /// <param name="point">交差座標</param>
        /// <returns>交差しているか</returns>
        public static bool RayCast(TriangleData triangle, Vector3 start, Vector3 end, out Vector3 point)
        {
            /*
            三角形上の任意の点を求める式は　v0+(v0,v1)ベクトル*係数+(v0,v2)ベクトル*係数2
            E1とE2にそれぞれ係数をかけてベクトルを合成すれば三角形の位置する面のすべての座標が示せる

            線分上の任意の点を求める式は　start + 正規化direction * 距離

            なのでこの二つの式を=でつなぐことでその交点を求められる。
            */
            Vector3 direction = (end - start);
            float segmentLength = direction.magnitude;

            // directionを正規化
            direction = direction.normalized;

            //E1 = V1-V0 E2 = V2 - V0
            Vector3 edgeVector1 = triangle.Vertex1 - triangle.Vertex0;
            Vector3 edgeVector2 = triangle.Vertex2 - triangle.Vertex0;

            //浮動小数点誤差に対応するための動的最小値の設定
            Vector3 edgeVector3 = triangle.Vertex1 - triangle.Vertex2;
            float averageLength = (edgeVector1.magnitude + edgeVector2.magnitude + edgeVector3.magnitude) / 3;
            float parallelEpsilon = averageLength * 1e-6f;
            float boundaryEpsilon = averageLength * 1e-7f;

            if (segmentLength < 1e-5f || segmentLength >= 1000)
            {
                Debug.LogWarning("極端に巨大または小さいレイはレイキャストの精度を低下させます");
            }

            if (edgeVector1.magnitude < 1e-5f || edgeVector2.magnitude < 1e-5f || edgeVector3.magnitude < 1e-5f ||
                edgeVector1.magnitude > 300 || edgeVector2.magnitude > 300 || edgeVector3.magnitude > 300)
            {
                Debug.LogWarning("極端に巨大または小さいな三角形はレイキャストの精度を低下させます");
            }


            //補助ベクトルH。　H = D×E2　クロス積 
            Vector3 rayV2Cross = Vector3.Cross(direction, edgeVector2);
            //係数a。　a = E1・H  レイと平面が十分交差しうる角度であるかを調べる(極端に水平に近いと浮動小数点誤差が致命的になったり水平だとつながらないため)
            float planeParallel = Vector3.Dot(edgeVector1, rayV2Cross);

            //両面判定のため絶対値評価
            //水平に近ければ足切り。
            if (Mathf.Abs(planeParallel) < parallelEpsilon)
            {
                /*
                 三角形のスケールが大きい場合、1e-8 の判定では実際には十分な角度があるのに「平行」と誤判定特に長距離レイで顕著
                 小さな三角形や接線に近いレイで、本来はぎりぎり大丈夫な範囲の角度のものも「平行」と判定されてしまう。つまり 粗すぎる閾値は接触判定を壊す。
                 ので1e-7fを利用している。
                 */
                point = Vector3.zero;
                return false;
            }

            //逆数を取得
            float oneMinus = 1.0f / planeParallel;

            //レイの座標系をV0を基準とした座標系に直す
            Vector3 v0RayStartPos = start - triangle.Vertex0;

            //三角形上の衝突点がどこにあるかを求める
            float u = Vector3.Dot(v0RayStartPos, rayV2Cross) * oneMinus;

            if (u < -boundaryEpsilon || u > 1.0f + boundaryEpsilon)
            {
                point = Vector3.zero;
                return false;
            }

            Vector3 supportVector = Vector3.Cross(v0RayStartPos, edgeVector1);
            float v = Vector3.Dot(direction, supportVector) * oneMinus;
            if (v < -boundaryEpsilon || (u + v) > 1.0f + boundaryEpsilon)
            {
                point = Vector3.zero;
                return false;
            }

            //正規化されたレイ上の衝突点までの距離を求める
            float rayDistance = Vector3.Dot(edgeVector2, supportVector) * oneMinus;

            // 線分の範囲内にあるかをチェック（0 <= rayDistance <= segmentLength）
            if (rayDistance < -boundaryEpsilon || rayDistance > segmentLength + boundaryEpsilon)
            {
                point = Vector3.zero;
                return false;
            }

            point = start + rayDistance * direction;
            return true;
        }


        /// <summary>
        /// 指定した頂点座標が閉じたメッシュ内にあるかを調べる
        /// </summary>
        /// <param name="vertexes"></param>
        /// <param name="triangleData"></param>
        /// <param name="vert"></param>
        /// <returns></returns>
        public static bool CheckInsideMesh(Vector3[] vertexes, int[] triangleData, Vector3 vert)
        {
            TriangleData triangle = new TriangleData();
            int hitCount = 0;

            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var vertex in vertexes)
            {
                if (vertex.y < minY) minY = vertex.y;
                if (vertex.y > maxY) maxY = vertex.y;
            }

            float rayLength = maxY - minY;

            for (int i = 0; i < triangleData.Length; i += 3)
            {
                triangle.SetVertexes(vertexes[triangleData[i]], vertexes[triangleData[i + 1]],
                    vertexes[triangleData[i + 2]]);

                if (RayCast(triangle, vert, vert + Vector3.up * rayLength, out var point))
                {
                    hitCount++;
                }
            }

            return hitCount % 2 != 0;
        }
    }
}