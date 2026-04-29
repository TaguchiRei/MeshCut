using UnityEngine;

public class EnemyMovementCalculationHP
{
    private const float DIRECTION_SEGMENT_COUNT = 100.0f;

    /// <summary>
    /// 敵の移動計算と座標更新を一括で行う
    /// </summary>
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
        Vector3 projectedCurrent = groundPlane.ClosestPointOnPlane(playerPosition[0]);
        Vector3 projectedPast = groundPlane.ClosestPointOnPlane(playerPosition[1]);

        for (int i = 0; i < maxIndex; i++)
        {
            ref EnemyDataHP enemy = ref enemies[i];

            // プレイヤーの現在と過去の近い方の位置をターゲットの起点にする
            float sqrDistCurrent = (projectedCurrent - enemy.Position).sqrMagnitude;
            float sqrDistPast = (projectedPast - enemy.Position).sqrMagnitude;
            Vector3 chosenPlayerPos = (sqrDistCurrent < sqrDistPast) ? projectedCurrent : projectedPast;

            // 螺旋オフセットの計算
            Vector3 offset = CalculatePosition(
                i,
                context.GlobalIndex,
                maxIndex,
                context.VertexSpace,
                context.MinRadius,
                context.MaxRadius);

            Vector3 target = chosenPlayerPos + offset + enemy.TargetPositionOffset;
            float sqrDist = (target - enemy.Position).sqrMagnitude;

            // 移動開始判定
            if (!enemy.IsMoving)
            {
                if (sqrDist > enemy.MoveStartDistance * enemy.MoveStartDistance)
                    enemy.IsMoving = true;
            }

            if (enemy.IsMoving)
            {
                // 指数補間を用いたスムーズな移動更新
                enemy.UpdateMovement(
                    target,
                    baseSpeed,
                    acceleration,
                    deltaTime,
                    context.MaxBounds, // EnemyGroupContextに合わせて引数順を調整
                    context.MinBounds);
            }
        }
    }

    private Vector3 CalculatePosition(
        int index, int globalIndex,
        int maxIndex, float vortexSpace,
        float minR, float maxR)
    {
        float calculationIndex = index + globalIndex;
        float looped = Mathf.Repeat(calculationIndex, Mathf.Max(1, maxIndex));
        float t = looped / Mathf.Max(1, maxIndex - 1);

        float thetaStart = minR / vortexSpace;
        float thetaEnd = maxR / vortexSpace;
        
        float baseTheta = Mathf.Lerp(thetaStart, thetaEnd, t);
        float radius = vortexSpace * baseTheta;

        // --- 周期的な反転ロジック ---
        int segmentIndex = (int)(t * DIRECTION_SEGMENT_COUNT);
        float dir = (segmentIndex % 4 == 0) ? -1f : 1f;
        float theta = dir * baseTheta; 
        // ------------------------

        // 地面（XZ平面）に合わせて出力
        float s = Mathf.Sin(theta);
        float c = Mathf.Cos(theta);

        return new Vector3(radius * c, 0f, radius * s);
    }
}