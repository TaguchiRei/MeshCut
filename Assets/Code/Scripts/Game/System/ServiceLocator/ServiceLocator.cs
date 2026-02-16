using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator : MonoBehaviour
{
    public static ServiceLocator Instance { get; private set; }

    private Dictionary<Type, object> _container = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool TryGetService<T>(out T service)
    {
#if UNITY_EDITOR
        if (!typeof(T).IsInterface)
        {
            throw new ArgumentException("T must be an interface");
        }
#endif
        var result = _container.TryGetValue(typeof(T), out var serviceObject);
        if (!result)
        {
            service = default;
            return false;
        }

        service = (T)serviceObject;
        return true;
    }

    public void RegisterService<T>(T service)
    {
#if UNITY_EDITOR
        if (!typeof(T).IsInterface)
        {
            throw new ArgumentException("T must be an interface");
        }
#endif
        //.Addなので複数同時登録しようとするとエラーになる
        _container.Add(typeof(T), service);
    }

    public void UnregisterService<T>()
    {
#if UNITY_EDITOR
        if (!typeof(T).IsInterface)
        {
            throw new ArgumentException("T must be an interface");
        }
#endif
        _container.Remove(typeof(T));
    }
}