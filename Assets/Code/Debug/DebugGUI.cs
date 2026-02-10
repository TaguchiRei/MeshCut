using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{
    [SerializeField] private Vector2 _position;
    [SerializeField, Range(20, 100)] private int _fontSize = 20;
    [SerializeField, Min(5)] private int _averageFPSSampling = 10;
    [SerializeField] private bool _removeMissingReferences;

    private readonly List<(string, Func<object>)> _getValueFunc = new();
    private readonly List<float> _averageFps = new();
    private readonly List<int> _notFindIndexes = new();

    private static DebugGUI _instance;

    private GUIStyle _debugStyle;
    private GUIStyle _errorStyle;
    private Rect _rect;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        _instance = null;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (Event.current.type == UnityEngine.EventType.Layout)
        {
            _debugStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize
            };

            _errorStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize,
                normal =
                {
                    textColor = Color.red
                }
            };
        }
        if (!Mathf.Approximately(_rect.width, Screen.width) || !Mathf.Approximately(_rect.height, Screen.height))
        {
            _rect = new Rect(_position.x, _position.y, Screen.width, Screen.height);
        }

        GUILayout.BeginVertical();
        GUI.BeginGroup(_rect);


        GUILayout.Label($"FPS : {(1.0f / Time.deltaTime):000.0}", _debugStyle);
        GUILayout.Label($"Average FPS : {GetAverageFPS():000.0}", _debugStyle);

        int index = 0;
        foreach (var refAndName in _getValueFunc)
        {
            try
            {
                GUILayout.Label($"{refAndName.Item1} : {refAndName.Item2.Invoke()}", _debugStyle);
            }
            catch
            {
                GUILayout.Label($"{refAndName.Item1} : 値が見つかりません。インスタンスはすでに破棄された可能性があります", _errorStyle);

                if (_removeMissingReferences)
                {
                    _notFindIndexes.Add(index);
                }
            }

            index++;
        }

        if (_removeMissingReferences)
        {
            _notFindIndexes.Sort();

            _notFindIndexes.Reverse();

            foreach (var t in _notFindIndexes)
            {
                _getValueFunc.RemoveAt(t);
            }

            _notFindIndexes.Clear();
        }

        GUI.EndGroup();
        GUILayout.EndVertical();
    }

    private void Update()
    {
        _averageFps.Add(Time.deltaTime);
        if (_averageFps.Count > _averageFPSSampling)
        {
            _averageFps.RemoveAt(0);
        }
    }
#endif

    private float GetAverageFPS()
    {
        if (_averageFps.Count == 0) return 0f;
        float average = 0;
        foreach (var delta in _averageFps)
        {
            average += delta;
        }

        average /= _averageFps.Count;
        return 1f / average;
    }


    public static void Register(string name, Func<object> debugValue)
    {
#if UNITY_EDITOR
        if (_instance == null)
        {
            Debug.LogWarning("DebugGUIの初期化前に登録メソッドが呼ばれました");
            return;
        }

        _instance._getValueFunc.Add((name, debugValue));
#endif
    }
}