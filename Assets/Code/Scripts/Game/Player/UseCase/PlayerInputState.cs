using UnityEngine;

/// <summary>
/// プレイヤーの入力状態の一時状態を保持するクラス
/// </summary>
public class PlayerInputState
{
    public float LongPressDuration { get; private set; }
    public float AimTime { get; private set; }
    public float JumpPressTime;
    public Vector2 MoveInput;

    public PlayerInputState(float longPressDuration, float aimTime)
    {
        LongPressDuration = longPressDuration;
        AimTime = aimTime;
    }
}