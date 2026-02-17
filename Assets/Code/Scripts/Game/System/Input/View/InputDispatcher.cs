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

    private void OnDestroy()
    {
        ServiceLocator.Instance.UnregisterService<IInputDispatcher>();
    }

    public void ChangeActionRegistrationStart(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        if (registration == Registration.Register)
        {
            inputAction.started += action;
        }
        else
        {
            inputAction.started -= action;
        }
    }

    public void ChangeActionRegistrationPerformed(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        if (registration == Registration.Register)
        {
            inputAction.performed += action;
        }
        else
        {
            inputAction.performed -= action;
        }
    }

    public void ChangeActionRegistrationCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        if (registration == Registration.Register)
        {
            inputAction.canceled += action;
        }
        else
        {
            inputAction.canceled -= action;
        }
    }

    public void ChangeActionRegistrationAll(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        if (registration == Registration.Register)
        {
            inputAction.started += action;
            inputAction.performed += action;
            inputAction.canceled += action;
        }
        else
        {
            inputAction.started -= action;
            inputAction.performed -= action;
            inputAction.canceled -= action;
        }
    }

    public void ChangeActionRegistrationStartCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration)
    {
        var inputAction = GetAction(actionMap, actionName);
        if (inputAction == null) return;
        if (registration == Registration.Register)
        {
            inputAction.started += action;
            inputAction.canceled += action;
        }
        else
        {
            inputAction.started -= action;
            inputAction.canceled -= action;
        }
    }
    
    private InputAction GetAction(string actionMap, string actionName)
    {
        if (_playerInput == null) return null;
        
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
        if (_playerInput == null) return;
        _playerInput.SwitchCurrentActionMap(actionMap);
    }

    public int GetActiveActionMap()
    {
        if (_playerInput == null) return 0;
        Enum.TryParse<ActionMaps>(_playerInput.currentActionMap.name, out var parseMap);
        return (int)parseMap;
    }
}