using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputDispatcher : MonoBehaviour, IInputDispatcher
{
    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        SwitchActionMap(nameof(ActionMaps.Player));

        ServiceLocator.Instance.RegisterService<IInputDispatcher>(this);
    }

    public void RegisterActionStart(string actionMap, string actionName, Action<InputAction.CallbackContext> action)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        inputAction.started += action;
    }

    public void RegisterActionPerformed(string actionMap, string actionName, Action<InputAction.CallbackContext> action)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        inputAction.performed += action;
    }

    public void RegisterActionCancelled(string actionMap, string actionName, Action<InputAction.CallbackContext> action)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        inputAction.canceled += action;
    }

    public void UnRegisterActionStart(string actionMap, string actionName, Action<InputAction.CallbackContext> action)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        inputAction.started -= action;
    }

    public void UnRegisterActionPerformed(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        inputAction.performed -= action;
    }

    public void UnRegisterActionCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        inputAction.canceled -= action;
    }

    private InputAction GetAction(string actionMap, string actionName)
    {
        var map = _playerInput.actions.FindActionMap(actionMap);
        if (map == null)
        {
            throw new ArgumentException();
        }

        var action = map.FindAction(actionName);
        if (action == null)
        {
            throw new ArgumentException();
        }

        return action;
    }

    public void SwitchActionMap(string actionMap)
    {
        _playerInput.SwitchCurrentActionMap(actionMap);
    }

    public int GetActiveActionMap()
    {
        Enum.TryParse<ActionMaps>(_playerInput.currentActionMap.name, out var parseMap);
        return (int)parseMap;
    }
}