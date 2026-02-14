using System;
using UnityEngine.InputSystem;

public interface IInputDispatcher
{
    /// <summary>
    /// Actionの登録を行う
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions　のEnumをstringにパースして使う</param>
    /// <param name="action"></param>
    public void RegisterActionStart(string actionMap, string actionName, Action<InputAction.CallbackContext> action);

    /// <summary>
    /// Actionの登録を行う
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions　のEnumをstringにパースして使う</param>
    /// <param name="action"></param>
    public void RegisterActionPerformed(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action);

    /// <summary>
    /// Actionの登録を行う
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions　のEnumをstringにパースして使う</param>
    /// <param name="action"></param>
    public void RegisterActionCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action);

    /// <summary>
    /// Actionの登録解除を行う
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions　のEnumをstringにパースして使う</param>
    /// <param name="action"></param>
    public void UnRegisterActionStart(string actionMap, string actionName, Action<InputAction.CallbackContext> action);

    /// <summary>
    /// Actionの登録解除を行う
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions　のEnumをstringにパースして使う</param>
    /// <param name="action"></param>
    public void UnRegisterActionPerformed(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action);

    /// <summary>
    /// Actionの登録解除を行う
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions　のEnumをstringにパースして使う</param>
    /// <param name="action"></param>
    public void UnRegisterActionCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action);

    /// <summary>
    /// ActionMapを変える
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    public void SwitchActionMap(string actionMap);

    /// <summary>
    /// 現在有効なActionMapのIDを取得する
    /// </summary>
    /// <returns>ActionMapにパースする</returns>
    public int GetActiveActionMap();
}