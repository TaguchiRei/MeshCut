using UnityEngine;

public class EnemyMovementCalculation
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
    public void UpdateEnemyVelocity(
        ref EnemyData[] enemies,
        Vector3[] playerPosition,
        EnemyGroupContext context,
        Plane groundPlane,
        float baseSpeed,
        float acceleration,
        float deltaTime)
    {
        int maxIndex = enemies.Length;

        Vector3 projectedCurrent =
            groundPlane.ClosestPointOnPlane(playerPosition[0]);

        Vector3 projectedPast =
            groundPlane.ClosestPointOnPlane(playerPosition[1]);

        for (int i = 0; i < maxIndex; i++)
        {
            ref EnemyData enemy = ref enemies[i];

            //過去と現在で最も近いプレイヤーの座標を目的地にする
            float sqrDistCurrent = (projectedCurrent - enemy.Position).sqrMagnitude;
            float sqrDistPast = (projectedPast - enemy.Position).sqrMagnitude;
            Vector3 chosenPlayerPos =
                (sqrDistCurrent < sqrDistPast)
                    ? projectedCurrent
                    : projectedPast;

            Vector3 offset = CalculatePosition(
                i,
                context.GlobalIndex,
                maxIndex,
                context.VertexSpace,
                context.MinRadius,
                context.MaxRadius);

            var targetPosition = chosenPlayerPos + offset + enemy.TargetPositionOffset;

            //目標地点に向かうベクトルの長さの２乗
            float sqrDist = (targetPosition - enemy.Position).sqrMagnitude;

            if (!enemy.IsMoving)
            {
                //sqrDistは距離の2乗なのでMoveStartDistanceも２乗することで√を求めずに計算できる
                if (sqrDist > enemy.MoveStartDistance * enemy.MoveStartDistance)
                {
                    enemy.IsMoving = true;
                }
            }

            if (enemy.IsMoving)
            {
                enemy.UpdateVelocity(
                    baseSpeed,
                    acceleration,
                    deltaTime,
                    targetPosition);
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
        

        int segmentIndex = (int)(t * DIRECTION_SEGMENT_COUNT);

        //渦の方向を周期的に反転する
        float dir = (segmentIndex % 4 == 0) ? -1f : 1f;

        float baseTheta = Mathf.Lerp(thetaStart, thetaEnd, t);
        float radius = vortexSpace * baseTheta;

        // theta をラジアンとしてそのまま使用（dirによる反転を削除）
        // 地面（XZ平面）に合わせる
        float s = Mathf.Sin(baseTheta);
        float c = Mathf.Cos(baseTheta);

        return new Vector3(radius * c, 0f, radius * s);
    }
}