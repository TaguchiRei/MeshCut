using UnityEngine;

[System.Serializable]
public struct EnemyDataHP
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 TargetPositionOffset;
    public float MoveStartDistance;
    public float MoveSpeedModifier;
    public bool IsMoving;

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

        Vector3 desiredDirection = toTarget / Mathf.Sqrt(sqrDistance);
        float finalSpeed = baseSpeed * MoveSpeedModifier;
        Vector3 desiredVelocity = desiredDirection * finalSpeed;

        // フレームレート非依存の加速処理
        float lerpFactor = 1f - Mathf.Exp(-acceleration * deltaTime);
        Velocity = Vector3.Lerp(Velocity, desiredVelocity, lerpFactor);

        Position += Velocity * deltaTime;

        // 移動範囲制限
        Vector3 clampedPosition = new Vector3(
            Mathf.Clamp(Position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(Position.y, minBounds.y, maxBounds.y),
            Mathf.Clamp(Position.z, minBounds.z, maxBounds.z)
        );

        // 壁に衝突した際の速度減衰
        if (!Mathf.Approximately(clampedPosition.x, Position.x)) Velocity.x = 0;
        if (!Mathf.Approximately(clampedPosition.y, Position.y)) Velocity.y = 0;
        if (!Mathf.Approximately(clampedPosition.z, Position.z)) Velocity.z = 0;

        Position = clampedPosition;
    }
}