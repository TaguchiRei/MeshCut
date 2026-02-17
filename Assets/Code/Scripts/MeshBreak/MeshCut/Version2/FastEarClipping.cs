using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 改良版：単純化（共線・重複除去）を行いつつ元のインデックスマッピングを保持、
/// Triangulate の結果を元インデックスに戻して返す関数を提供します。
/// </summary>
public static class FastEarClipping
{
    const float EPS = 1e-6f;
    const float COLLINEAR_EPS = 1e-6f;
    const float DUPLICATE_EPS_SQR = 1e-10f; // 重複判定（位置差の二乗閾値）

    // メイン：projectedVertices（loop上の2D座標）と
    // originalLoopIndices（FillCapForLoopの loop をそのまま渡す）を受け、
    // 戻り値は「元の loop のインデックス」の順で三角形インデックスのリストを返す
    public static List<int> TriangulateMapped(IList<Vector2> projectedVertices, IList<int> originalLoopIndices)
    {
        // 1) 単純化と同時に元インデックスを保持
        var (simpleVerts, simpleToOriginal) = RemoveCollinearAndDuplicatesWithMapping(projectedVertices, originalLoopIndices);

        int n = simpleVerts.Count;
        if (n < 3) return new List<int>();

        // 2) 向き（CW/CCW）をチェック。Ear clipping は CCW 前提が多いので反転する
        if (SignedArea(simpleVerts) < 0f)
        {
            simpleVerts.Reverse();
            simpleToOriginal.Reverse();
        }

        // 3) 通常の耳切り（内部は simpleVerts のインデックス基準）
        var resultSimple = TriangulateSimple(simpleVerts);

        // 4) simpleインデックスを元インデックスに変換して返す
        var resultMapped = new List<int>(resultSimple.Count);
        for (int i = 0; i < resultSimple.Count; i++)
        {
            int simpleIdx = resultSimple[i];
            resultMapped.Add(simpleToOriginal[simpleIdx]);
        }

        return resultMapped;
    }

    // 単純化のみでインデックスマッピングも返す
    private static (List<Vector2> simpleVerts, List<int> simpleToOriginal) RemoveCollinearAndDuplicatesWithMapping(IList<Vector2> verts, IList<int> originalIndices)
    {
        int n = verts.Count;
        var simple = new List<Vector2>(n);
        var map = new List<int>(n);

        for (int i = 0; i < n; i++)
        {
            Vector2 prev = verts[(i - 1 + n) % n];
            Vector2 curr = verts[i];
            Vector2 next = verts[(i + 1) % n];

            // 重複（ほぼ同じ位置）の除去（直前追加と比較）
            if (simple.Count > 0)
            {
                Vector2 lastAdded = simple[simple.Count - 1];
                if ((curr - lastAdded).sqrMagnitude <= DUPLICATE_EPS_SQR)
                    continue;
            }

            // 共線（直線上にある）なら除去
            if (Mathf.Abs(Cross(prev, curr, next)) <= COLLINEAR_EPS)
            {
                // ただし、共線のうち端点は残す（必要ならロジックを調整）
                // ここでは中間点を飛ばすことでノイズを除去
                continue;
            }

            simple.Add(curr);
            map.Add(originalIndices[i]);
        }

        // ループ閉鎖で最初と最後が近すぎる場合は最後を取り除く
        if (simple.Count >= 2)
        {
            if ((simple[0] - simple[simple.Count - 1]).sqrMagnitude <= DUPLICATE_EPS_SQR)
            {
                simple.RemoveAt(simple.Count - 1);
                map.RemoveAt(map.Count - 1);
            }
        }

        return (simple, map);
    }

    // simpleVerts に対する耳切り本体（返り値は simpleVerts のインデックス）
    private static List<int> TriangulateSimple(IList<Vector2> v)
    {
        int n = v.Count;
        var result = new List<int>(n * 3);

        if (n < 3) return result;
        if (n == 3)
        {
            result.Add(0); result.Add(1); result.Add(2);
            return result;
        }

        int[] prev = new int[n];
        int[] next = new int[n];
        bool[] removed = new bool[n];
        bool[] isReflex = new bool[n];
        bool[] isEar = new bool[n];

        for (int i = 0; i < n; i++)
        {
            prev[i] = (i - 1 + n) % n;
            next[i] = (i + 1) % n;
        }

        for (int i = 0; i < n; i++) isReflex[i] = IsReflex(v, prev[i], i, next[i]);
        for (int i = 0; i < n; i++) isEar[i] = IsEar(i, v, prev, next, isReflex, removed);

        int remaining = n;
        int safe = 0;
        while (remaining > 3 && safe < n * 10)
        {
            bool earFound = false;
            for (int i = 0; i < n; i++)
            {
                if (removed[i] || !isEar[i]) continue;

                int a = prev[i], b = i, c = next[i];

                // 出力 (a,b,c)
                result.Add(a); result.Add(b); result.Add(c);

                // 削除
                removed[i] = true;
                remaining--;

                // リンク更新
                next[a] = c;
                prev[c] = a;

                // 再評価（隣接のみ）
                isReflex[a] = IsReflex(v, prev[a], a, next[a]);
                isReflex[c] = IsReflex(v, prev[c], c, next[c]);

                isEar[a] = IsEar(a, v, prev, next, isReflex, removed);
                isEar[c] = IsEar(c, v, prev, next, isReflex, removed);

                earFound = true;
                break;
            }

            if (!earFound)
            {
                // 予防措置：浮動小数点で耳が見つからない場合はループを小さくしてみる（数値安定化）
                for (int i = 0; i < n; i++) if (!removed[i]) { isEar[i] = true; break; }
            }

            safe++;
        }

        // 残った最後の三角形を追加
        int v0 = -1;
        for (int i = 0; i < n; i++) if (!removed[i]) { v0 = i; break; }
        int v1 = next[v0];
        int v2 = next[v1];
        result.Add(v0); result.Add(v1); result.Add(v2);

        return result;
    }

    // ヘルパー群（既存ロジックと同等）
    static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    static bool IsReflex(IList<Vector2> v, int a, int b, int c)
    {
        return Cross(v[a], v[b], v[c]) < -EPS;
    }

    static bool IsEar(int i, IList<Vector2> v, int[] prev, int[] next, bool[] isReflex, bool[] removed)
    {
        if (removed[i] || isReflex[i]) return false;

        int a = prev[i], b = i, c = next[i];
        Vector2 A = v[a], B = v[b], C = v[c];

        float minX = Mathf.Min(A.x, Mathf.Min(B.x, C.x));
        float maxX = Mathf.Max(A.x, Mathf.Max(B.x, C.x));
        float minY = Mathf.Min(A.y, Mathf.Min(B.y, C.y));
        float maxY = Mathf.Max(A.y, Mathf.Max(B.y, C.y));

        for (int j = 0; j < v.Count; j++)
        {
            if (removed[j]) continue;
            if (j == a || j == b || j == c) continue;
            if (!isReflex[j]) continue; // 凸頂点はスキップ

            Vector2 P = v[j];
            if (P.x < minX || P.x > maxX || P.y < minY || P.y > maxY) continue;
            if (PointInTriangle(P, A, B, C)) return false;
        }

        return true;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float c1 = Cross(a, b, p);
        float c2 = Cross(b, c, p);
        float c3 = Cross(c, a, p);

        bool hasNeg = c1 < -EPS || c2 < -EPS || c3 < -EPS;
        bool hasPos = c1 > EPS || c2 > EPS || c3 > EPS;

        return !(hasNeg && hasPos);
    }

    static float SignedArea(IList<Vector2> v)
    {
        int n = v.Count;
        double area = 0.0;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = v[i];
            Vector2 b = v[(i + 1) % n];
            area += (double)a.x * b.y - (double)b.x * a.y;
        }
        return (float)(area * 0.5);
    }
}
