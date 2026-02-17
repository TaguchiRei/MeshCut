using UnityEngine;

public struct EnemyData
{
    public Vector3 Position;
    public Vector3 Velocity { get; private set; }
    public Vector3 TargetPositionOffset;
    public float MoveStartDistance;
    public float MoveSpeedModifier;
    public bool IsMoving;

    /// <summary>
    /// 移動方向を更新する
    /// </summary>
    /// <param name="baseSpeed"></param>
    /// <param name="acceleration"></param>
    /// <param name="deltaTime"></param>
    /// <param name="targetPosition"></param>
    public void UpdateVelocity(
        float baseSpeed,
        float acceleration,
        float deltaTime,
        Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - Position;

        float sqrDistance = toTarget.sqrMagnitude;

        if (sqrDistance < 0.0001f)
        {
            Velocity = Vector3.zero;
            return;
        }

        Vector3 desiredDirection = toTarget / Mathf.Sqrt(sqrDistance);
        float finalSpeed = baseSpeed * MoveSpeedModifier;
        Vector3 desiredVelocity = desiredDirection * finalSpeed;

        float lerpFactor = 1f - Mathf.Exp(-acceleration * deltaTime);

        Velocity = Vector3.Lerp(Velocity, desiredVelocity, lerpFactor);
    }
    
    /// <summary>
    /// 座標を更新する
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="minBounds"></param>
    /// <param name="maxBounds"></param>
    public void UpdatePosition(
        float deltaTime,
        Vector3 minBounds,
        Vector3 maxBounds)
    {
        Position += Velocity * deltaTime;

        Vector3 clamped;
        clamped.x = Mathf.Clamp(Position.x, minBounds.x, maxBounds.x);
        clamped.y = Mathf.Clamp(Position.y, minBounds.y, maxBounds.y);
        clamped.z = Mathf.Clamp(Position.z, minBounds.z, maxBounds.z);

        if (!Mathf.Approximately(clamped.x, Position.x))
            Velocity = new Vector3(0, Velocity.y, Velocity.z);

        if (!Mathf.Approximately(clamped.y, Position.y))
            Velocity = new Vector3(Velocity.x, 0, Velocity.z);

        if (!Mathf.Approximately(clamped.z, Position.z))
            Velocity = new Vector3(Velocity.x, Velocity.y, 0);

        Position = clamped;
    }
}