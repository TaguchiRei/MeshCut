using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController
{
    private readonly IInputDispatcher _inputDispatcher;
    private readonly IPlayerMove _playerMove;
    private readonly ICameraMove _cameraMove;
    private Vector2 _moveInput;

    public PlayerController(IInputDispatcher inputDispatcher, IPlayerMove playerMove, ICameraMove cameraMove)
    {
        _inputDispatcher = inputDispatcher;
        _playerMove = playerMove;
        _cameraMove = cameraMove;
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
            OnJumpInput
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
        _inputDispatcher.UnRegisterActionPerformed(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Jump),
            OnJumpInput
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

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if (!_playerMove.OnGround)
            return;
        _playerMove.Jump();
    }

    private void OnLookInput(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>();
        _cameraMove.Look(input);
    }
}