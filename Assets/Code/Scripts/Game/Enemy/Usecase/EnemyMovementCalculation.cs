using UnityEngine;

public class EnemyMovementCalculation
{
    public void MoveEnemy(
        ref EnemyData[] enemies,
        Vector3 playerPosition,
        int globalIndex, float vertexSpace,
        float minRadius, float maxRadius)
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            ref EnemyData enemy = ref enemies[i];
            
            
        }
    }

    /// <summary>
    /// インデックスに基づいて座標を計算する
    /// </summary>
    /// <param name="index">インデックス</param>
    /// <param name="globalIndex">全体のインデックスオフセット</param>
    /// <param name="maxIndex">最大数</param>
    /// <param name="vortexSpace">渦ごとの間隔</param>
    /// <param name="minR">最小半径</param>
    /// <param name="maxR">最大半径</param>
    /// <returns></returns>
    private Vector3 CalculatePosition(
        int index,
        int globalIndex,
        int maxIndex,
        float vortexSpace,
        float minR,
        float maxR)
    {
        float fIdx = index;
        float fGidx = globalIndex;
        float fMidx = maxIndex;

        float u = fIdx + fGidx;

        float m = Mathf.Max(1.0f, fMidx);

        // HLSL: fmod(fmod(u, m) + m, m)
        float looped = Mathf.Repeat(Mathf.Repeat(u, m) + m, m);

        float denom = Mathf.Max(1.0f, fMidx - 1.0f);
        float t = looped / denom;

        float safeVortex = Mathf.Max(0.001f, vortexSpace);

        float thetaStart = minR / safeVortex;
        float thetaEnd = maxR / safeVortex;

        float baseTheta = Mathf.Lerp(thetaStart, thetaEnd, t);
        float radius = safeVortex * baseTheta;

        float zigzagCount = 100.0f;
        float k = Mathf.Floor(t * zigzagCount);

        // HLSL: (fmod(k, 4.0) == 0.0)
        float dir = (Mathf.Repeat(k, 4.0f) == 0.0f) ? -1.0f : 1.0f;

        float theta = dir * baseTheta;

        float s = Mathf.Sin(theta);
        float c = Mathf.Cos(theta);

        float posX = radius * c;
        float posY = radius * s;

        return new Vector3(posX, posY, 0.0f);
    }
}