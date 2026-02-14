using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private IInputDispatcher _inputDispatcher;

    private void Start()
    {
        ServiceLocator.Instance.TryGetService(out _inputDispatcher);
    }
}