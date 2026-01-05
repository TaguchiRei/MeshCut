using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{
    [SerializeField] private Vector2 _position;
    [SerializeField, Range(20, 100)] private int _fontSize = 20;
    [SerializeField, Min(5)] private int _averageFPSSampling = 10;

    private List<(string, Func<object>)> _objectsRefs = new();
    private List<float> _averageFPS = new();

    private static DebugGUI _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = _fontSize;


        GUILayout.BeginVertical();
        GUI.BeginGroup(new Rect(_position.x, _position.y, Screen.width, Screen.height));


        GUILayout.Label($"FPS : {(1.0f / Time.deltaTime):000.0}", style);
        GUILayout.Label($"Average FPS : {GetAverageFPS():000.0}", style);

        foreach (var refAndName in _objectsRefs)
        {
            GUILayout.Label($"{refAndName.Item1} : {refAndName.Item2.Invoke()}", style);
        }

        GUI.EndGroup();
        GUILayout.EndVertical();
    }

    private void Update()
    {
        _averageFPS.Add(Time.deltaTime);
        if (_averageFPS.Count > _averageFPSSampling)
        {
            _averageFPS.RemoveAt(0);
        }
    }

    private float GetAverageFPS()
    {
        if (_averageFPS.Count == 0) return 0f;
        float average = 0;
        foreach (var delta in _averageFPS)
        {
            average += delta;
        }

        average /= _averageFPS.Count;
        return 1f / average;
    }


    public static void Register(string name, Func<object> debugValue)
    {
        _instance._objectsRefs.Add((name, debugValue));
    }
}