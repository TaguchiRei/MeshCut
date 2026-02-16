using UnityEngine;

public class EnemyMovementCalculation
{
    const float DIRECTION_SEGMENT_COUNT = 100.0f;

    public void MoveEnemy(
        ref EnemyData[] enemies,
        Vector3[] playerPosition,
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
        int index, int globalIndex,
        int maxIndex, float vortexSpace,
        float minR, float maxR)
    {
#if UNITY_EDITOR
        if (maxIndex <= 0)
        {
            Debug.LogError("敵は1体以上必要です");
            return Vector3.zero;
        }

        if (vortexSpace < 0.01f)
        {
            Debug.LogWarning("vortexSpaceが小さすぎます");
            return Vector3.zero;
        }
#endif

        float calculationIndex = index + globalIndex;

        float looped = Mathf.Repeat(calculationIndex, maxIndex);

        float t = looped / Mathf.Max(1, maxIndex - 1);

        float thetaStart = minR / vortexSpace;
        float thetaEnd = maxR / vortexSpace;

        float baseTheta = Mathf.Lerp(thetaStart, thetaEnd, t);
        float radius = vortexSpace * baseTheta;

        int segmentIndex = (int)(t * DIRECTION_SEGMENT_COUNT);

        //渦の方向を周期的に反転する
        float dir = (segmentIndex % 4 == 0) ? -1f : 1f;

        float theta = dir * baseTheta;

        float s = Mathf.Sin(theta);
        float c = Mathf.Cos(theta);

        return new Vector3(radius * c, radius * s, 0f);
    }
}