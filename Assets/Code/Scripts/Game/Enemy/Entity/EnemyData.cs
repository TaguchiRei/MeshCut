using UnityEngine;

public struct EnemyData
{
    public Vector3 Position;
    public Vector3 Velocity { get; private set; }
    public Vector3 TargetPositionOffset;
    public float MoveStartDistance;
    public float MoveSpeedModifier;
    public bool IsMoving;

    public void UpdateMovement(
        Vector3 targetPosition,
        float baseSpeed,
        float acceleration,
        float deltaTime)
    {
        Vector3 toTarget = targetPosition - Position;

        float sqrDistance = toTarget.sqrMagnitude;

        if (sqrDistance < 0.0001f)
        {
            Velocity = Vector3.zero;
            return;
        }

        // 正規化（sqrtはここ1回のみ）
        Vector3 desiredDirection = toTarget / Mathf.Sqrt(sqrDistance);

        float finalSpeed = baseSpeed * MoveSpeedModifier;

        Vector3 desiredVelocity = desiredDirection * finalSpeed;

        // フレームレート非依存指数補間
        float lerpFactor = 1f - Mathf.Exp(-acceleration * deltaTime);

        Velocity = Vector3.Lerp(Velocity, desiredVelocity, lerpFactor);

        Position += Velocity * deltaTime;
    }
}