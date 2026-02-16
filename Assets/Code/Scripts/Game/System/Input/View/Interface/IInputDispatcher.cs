using System;
using UnityEngine.InputSystem;

public interface IInputDispatcher
{
    /// <summary>
    /// Actionの登録状態を変更する
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions のEnumをstringにパースして使う</param>
    /// <param name="action">登録／解除するAction</param>
    /// <param name="registration">Registerで登録、UnRegisterで解除</param>
    public void ChangeActionRegistrationStart(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration);

    /// <summary>
    /// Actionの登録状態を変更する
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions のEnumをstringにパースして使う</param>
    /// <param name="action">登録／解除するAction</param>
    /// <param name="registration">Registerで登録、UnRegisterで解除</param>
    public void ChangeActionRegistrationPerformed(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration);

    /// <summary>
    /// Actionの登録状態を変更する
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions のEnumをstringにパースして使う</param>
    /// <param name="action">登録／解除するAction</param>
    /// <param name="registration">Registerで登録、UnRegisterで解除</param>
    public void ChangeActionRegistrationCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration);

    /// <summary>
    /// Actionの登録状態を変更する
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions のEnumをstringにパースして使う</param>
    /// <param name="action">登録／解除するAction</param>
    /// <param name="registration">Registerで登録、UnRegisterで解除</param>
    public void ChangeActionRegistrationAll(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration);

    /// <summary>
    /// Actionの登録状態を変更する
    /// </summary>
    /// <param name="actionMap">ActionMapsをstringにパースする</param>
    /// <param name="actionName">ActionMap + Actions のEnumをstringにパースして使う</param>
    /// <param name="action">登録／解除するAction</param>
    /// <param name="registration">Registerで登録、UnRegisterで解除</param>
    public void ChangeActionRegistrationStartCancelled(string actionMap, string actionName,
        Action<InputAction.CallbackContext> action, Registration registration);


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

public enum Registration
{
    Register,
    UnRegister
}