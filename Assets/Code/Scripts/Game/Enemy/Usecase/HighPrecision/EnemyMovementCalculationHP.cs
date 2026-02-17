using UnityEngine;

public class EnemyMovementCalculationHP
{
    const float DIRECTION_SEGMENT_COUNT = 100.0f;

    /// <summary>
    /// 敵の配列を受け取り、配列内のオブジェクト群の位置と目標地点を
    /// </summary>
    /// <param name="enemies"></param>
    /// <param name="playerPosition"></param>
    /// <param name="context"></param>
    /// <param name="groundPlane"></param>
    /// <param name="baseSpeed"></param>
    /// <param name="acceleration"></param>
    /// <param name="deltaTime"></param>
    public void MoveEnemy(
        ref EnemyDataHP[] enemies,
        Vector3[] playerPosition,
        EnemyGroupContext context,
        Plane groundPlane,
        float baseSpeed,
        float acceleration,
        float deltaTime)
    {
        int maxIndex = enemies.Length;

        // プレイヤーの現在位置と過去の位置をPlane上に投影
        Vector3 projectedCurrent = groundPlane.ClosestPointOnPlane(playerPosition[0]);
        Vector3 projectedPast = groundPlane.ClosestPointOnPlane(playerPosition[1]);

        for (int i = 0; i < maxIndex; i++)
        {
            ref EnemyDataHP enemy = ref enemies[i];

            // プレイヤーの現在と過去の近い方の位置を利用
            float sqrDistCurrent =
                (projectedCurrent - enemy.Position).sqrMagnitude;
            float sqrDistPast =
                (projectedPast - enemy.Position).sqrMagnitude;

            Vector3 chosenPlayerPos =
                (sqrDistCurrent < sqrDistPast)
                    ? projectedCurrent
                    : projectedPast;

            // 目的座標を改造したアルキメデスの螺旋上に並ぶように計算
            Vector3 offset = CalculatePosition(
                i,
                context.GlobalIndex,
                maxIndex,
                context.VertexSpace,
                context.MinRadius,
                context.MaxRadius);

            Vector3 target =
                chosenPlayerPos + offset + enemy.TargetPositionOffset;

            // 移動を開始するかを判定する
            float sqrDist = (target - enemy.Position).sqrMagnitude;

            if (!enemy.IsMoving)
            {
                //sqrMagnitudeは2上の値なのでMoveStartDistanceも2乗すれば√を外さずに計算できる
                if (sqrDist > enemy.MoveStartDistance * enemy.MoveStartDistance)
                    enemy.IsMoving = true;
            }

            if (enemy.IsMoving)
            {
                enemy.UpdateMovement(
                    target,
                    baseSpeed,
                    acceleration,
                    deltaTime,
                    context.MinBounds,
                    context.MaxBounds);
            }
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