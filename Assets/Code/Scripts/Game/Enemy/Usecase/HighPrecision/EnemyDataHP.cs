using UnityEngine;

public struct EnemyDataHP
{
    public Vector3 Position;
    public Vector3 Velocity { get; private set; }
    public Vector3 TargetPositionOffset;
    public float MoveStartDistance;
    public float MoveSpeedModifier;
    public bool IsMoving;

    /// <summary>
    /// 敵をRigidBodyに頼らず動かす
    /// </summary>
    /// <param name="targetPosition">目標地点</param>
    /// <param name="baseSpeed">速度</param>
    /// <param name="acceleration">加速度</param>
    /// <param name="deltaTime">Time.deltaTime</param>
    /// <param name="minBounds">移動下限座標</param>
    /// <param name="maxBounds">移動上限座標</param>
    public void UpdateMovement(
        Vector3 targetPosition,
        float baseSpeed,
        float acceleration,
        float deltaTime,
        Vector3 minBounds,
        Vector3 maxBounds)
    {
        Vector3 toTarget = targetPosition - Position;

        float sqrDistance = toTarget.sqrMagnitude;

        if (sqrDistance < 0.0001f)
        {
            Velocity = Vector3.zero;
            return;
        }

        // 移動方向の正規化ベクトルを取得
        Vector3 desiredDirection = toTarget / Mathf.Sqrt(sqrDistance);

        float finalSpeed = baseSpeed * MoveSpeedModifier;

        Vector3 desiredVelocity = desiredDirection * finalSpeed;

        // フレームレート非依存指数補間
        float lerpFactor = 1f - Mathf.Exp(-acceleration * deltaTime);

        Velocity = Vector3.Lerp(Velocity, desiredVelocity, lerpFactor);

        Position += Velocity * deltaTime;

        //移動範囲に制限を書ける

        Vector3 clampedPosition;
        clampedPosition.x = Mathf.Clamp(Position.x, minBounds.x, maxBounds.x);
        clampedPosition.y = Mathf.Clamp(Position.y, minBounds.y, maxBounds.y);
        clampedPosition.z = Mathf.Clamp(Position.z, minBounds.z, maxBounds.z);

        // 壁に当たった軸の速度を止める
        if (!Mathf.Approximately(clampedPosition.x, Position.x))
        {
            Velocity = new(0, Velocity.y, Velocity.z);
        }

        if (!Mathf.Approximately(clampedPosition.y, Position.y))
        {
            Velocity = new(Velocity.x, 0, Velocity.z);
        }

        if (!Mathf.Approximately(clampedPosition.z, Position.z))
        {
            Velocity = new(Velocity.x, Velocity.y, 0);
        }

        Position = clampedPosition;
    }
}