using System.Collections.Generic;
using UnityEngine;

public static class FastEarClipping
{
    // reflex判定などに使うEPS
    const float EPS = 1e-6f;
    // 共線判定用EPS（座標スケールに応じて調整可能）
    const float COLLINEAR_EPS = 1e-6f;

    public static List<int> Triangulate(IList<Vector2> vertices)
    {
        // まず共線頂点を除去
        var simpleVertices = RemoveCollinear(vertices);

        int n = simpleVertices.Count;
        if (n < 3) return new List<int>();

        var result = new List<int>(n * 3);

        int[] prev = new int[n];
        int[] next = new int[n];
        bool[] isReflex = new bool[n];
        bool[] isEar = new bool[n];
        bool[] removed = new bool[n];

        for (int i = 0; i < n; i++)
        {
            prev[i] = (i - 1 + n) % n;
            next[i] = (i + 1) % n;
        }

        // 初期凸凹判定
        for (int i = 0; i < n; i++)
            isReflex[i] = IsReflex(simpleVertices, prev[i], i, next[i]);

        // 初期耳判定
        for (int i = 0; i < n; i++)
            isEar[i] = IsEar(i, simpleVertices, prev, next, isReflex, removed);

        int remaining = n;

        while (remaining > 3)
        {
            bool earFound = false;

            for (int i = 0; i < n; i++)
            {
                if (removed[i] || !isEar[i])
                    continue;

                int a = prev[i];
                int b = i;
                int c = next[i];

                result.Add(a);
                result.Add(b);
                result.Add(c);

                removed[i] = true;
                remaining--;

                // 隣接する頂点のprev/nextを更新して、削除された頂点iをスキップする
                next[a] = c; // aの次の頂点をcにする
                prev[c] = a; // cの前の頂点をaにする

                // 隣接のみ再評価
                isReflex[a] = IsReflex(simpleVertices, prev[a], a, next[a]);
                isReflex[c] = IsReflex(simpleVertices, prev[c], c, next[c]);

                isEar[a] = IsEar(a, simpleVertices, prev, next, isReflex, removed);
                isEar[c] = IsEar(c, simpleVertices, prev, next, isReflex, removed);

                earFound = true;
                break;
            }
        }

        // 最後の三角形（順序保証）
        int v0 = -1;
        for (int i = 0; i < n; i++)
        {
            if (!removed[i])
            {
                v0 = i;
                break;
            }
        }
        int v1 = next[v0];
        int v2 = next[v1];
        result.Add(v0);
        result.Add(v1);
        result.Add(v2);

        return result;
    }

    // 共線頂点を除去
    static List<Vector2> RemoveCollinear(IList<Vector2> vertices)
    {
        int n = vertices.Count;
        if (n < 3) return new List<Vector2>(vertices);

        var result = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = vertices[(i - 1 + n) % n];
            Vector2 curr = vertices[i];
            Vector2 next = vertices[(i + 1) % n];

            if (Mathf.Abs(Cross(prev, curr, next)) > COLLINEAR_EPS)
            {
                result.Add(curr);
            }
        }
        return result;
    }

    static bool IsReflex(IList<Vector2> v, int a, int b, int c)
    {
        return Cross(v[a], v[b], v[c]) < -EPS;
    }

    static bool IsEar(int i, IList<Vector2> v, int[] prev, int[] next, bool[] isReflex, bool[] removed)
    {
        if (removed[i] || isReflex[i])
            return false;

        int a = prev[i];
        int b = i;
        int c = next[i];

        Vector2 A = v[a];
        Vector2 B = v[b];
        Vector2 C = v[c];

        // AABBフィルタ
        float minX = Mathf.Min(A.x, Mathf.Min(B.x, C.x));
        float maxX = Mathf.Max(A.x, Mathf.Max(B.x, C.x));
        float minY = Mathf.Min(A.y, Mathf.Min(B.y, C.y));
        float maxY = Mathf.Max(A.y, Mathf.Max(B.y, C.y));

        for (int j = 0; j < v.Count; j++)
        {
            if (removed[j] || !isReflex[j])
                continue;
            if (j == a || j == b || j == c)
                continue;

            Vector2 P = v[j];
            if (P.x < minX || P.x > maxX || P.y < minY || P.y > maxY)
                continue;

            if (PointInTriangle(P, A, B, C))
                return false;
        }

        return true;
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
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
}
