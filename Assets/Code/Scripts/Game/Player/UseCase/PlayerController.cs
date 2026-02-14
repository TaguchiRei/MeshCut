using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController
{
    private readonly IInputDispatcher _inputDispatcher;
    private readonly IPlayerMove _playerMove;
    private readonly ICameraMove _cameraMove;
    private readonly PlayerInputState _playerInputState;
    private Vector2 _moveInput;

    public PlayerController(
        IInputDispatcher inputDispatcher,
        IPlayerMove playerMove,
        ICameraMove cameraMove,
        PlayerInputState playerInputState)
    {
        _inputDispatcher = inputDispatcher;
        _playerMove = playerMove;
        _cameraMove = cameraMove;
        _playerInputState = playerInputState;
    }

    public void EnableInput()
    {
        // 移動アクションの登録
        _inputDispatcher.RegisterActionPerformed(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Move),
            OnMoveInput
        );
        _inputDispatcher.RegisterActionCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Move),
            OnMoveInput
        );

        // ジャンプアクションの登録
        _inputDispatcher.RegisterActionStart(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Jump),
            OnJumpStarted
        );
        _inputDispatcher.RegisterActionCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Jump),
            OnJumpCanceled
        );

        _inputDispatcher.RegisterActionPerformed(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Look),
            OnLookInput);

        // Playerアクションマップを有効にする
        _inputDispatcher.SwitchActionMap(nameof(ActionMaps.Player));
    }

    public void DisableInput()
    {
        // 移動アクションの登録解除
        _inputDispatcher.UnRegisterActionPerformed(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Move),
            OnMoveInput
        );
        _inputDispatcher.UnRegisterActionCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Move),
            OnMoveInput
        );

        // ジャンプアクションの登録解除
        _inputDispatcher.UnRegisterActionStart(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Jump),
            OnJumpStarted
        );
        _inputDispatcher.UnRegisterActionCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Jump),
            OnJumpCanceled
        );

        _inputDispatcher.UnRegisterActionPerformed(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Look),
            OnLookInput);
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
        _playerMove.Move(new Vector3(_moveInput.x, 0f, _moveInput.y));
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        // 空中にいた場合、重力を変える
        if (!_playerMove.OnGround)
        {
            _playerMove.ChangeGround();
            return;
        }

        _playerMove.Jump();
        _playerInputState.JumpPressTime = Time.time;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        // OnJumpStartedが呼ばれていない、または空中にいる場合は何もしない
        if (_playerInputState.JumpPressTime == 0f || !_playerMove.OnGround)
        {
            _playerInputState.JumpPressTime = 0f;
            return;
        }

        float pressDuration = Time.time - _playerInputState.JumpPressTime;
        _playerInputState.JumpPressTime = 0f; // タイマーをリセット

        float jumpMagnitude = 1.0f;
        if (pressDuration >= _playerInputState.LongPressDuration)
        {
            jumpMagnitude = 1.5f;
        }
    }

    private void OnLookInput(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>();
        _cameraMove.Look(input);
    }
}