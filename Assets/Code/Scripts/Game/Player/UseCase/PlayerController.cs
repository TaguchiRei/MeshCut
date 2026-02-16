using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController
{
    private readonly IInputDispatcher _inputDispatcher;
    private readonly IPlayerMove _playerMove;
    private readonly ICameraMove _cameraMove;
    private readonly PlayerInputState _playerInputState;

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

    public void SetActiveInput(bool active)
    {
        var register = active ? Registration.Register : Registration.UnRegister;
        // 移動アクションの登録
        _inputDispatcher.ChangeActionRegistrationStartCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Move),
            OnMoveInput,
            register
        );

        // ジャンプアクションの登録
        _inputDispatcher.ChangeActionRegistrationStart(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Jump),
            OnJumpStarted,
            register
        );

        _inputDispatcher.ChangeActionRegistrationPerformed(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Look),
            OnLookInput,
            register);

        _inputDispatcher.ChangeActionRegistrationStartCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Sprint),
            OnSprintInput,
            register);

        _inputDispatcher.ChangeActionRegistrationStartCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Aim),
            OnAimInput,
            register);

        // Playerアクションマップを有効にする
        _inputDispatcher.SwitchActionMap(nameof(ActionMaps.Player));
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        _playerInputState.MoveInput = context.ReadValue<Vector2>();
        _playerMove.Move(new Vector3(_playerInputState.MoveInput.x, 0f, _playerInputState.MoveInput.y));
    }

    private void OnSprintInput(InputAction.CallbackContext context)
    {
        _playerMove.Running = context.started || context.performed;
    }

    private void OnAimInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _playerMove.AimStart();
            _cameraMove.LockCamera(_playerInputState.AimTIme);
        }
        else if (context.canceled)
        {
            _playerMove.AimEnd();
            _cameraMove.ResumeCamera();
        }
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
    }

    private void OnLookInput(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>();
        _cameraMove.Look(input);
    }
}