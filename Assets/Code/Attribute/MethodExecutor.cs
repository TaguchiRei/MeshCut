using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UsefulAttribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodExecutorAttribute : Attribute
    {
        public string ButtonName { get; }
        public bool CanExecuteInEditMode { get; }

        public MethodExecutorAttribute(string buttonName, bool canExecuteInEditMode)
        {
            ButtonName = buttonName;
            CanExecuteInEditMode = canExecuteInEditMode;
        }

        public MethodExecutorAttribute()
        {
            ButtonName = "TestMethod";
            CanExecuteInEditMode = false;
        }

        public MethodExecutorAttribute(bool canExecuteInEditMode)
        {
            ButtonName = "TestMethod";
            CanExecuteInEditMode = canExecuteInEditMode;
        }

        public MethodExecutorAttribute(string buttonName)
        {
            ButtonName = buttonName;
            CanExecuteInEditMode = false;
        }
    }


    [CustomEditor(typeof(MonoBehaviour), true)]
    public class InspectorButtonEditor : Editor
    {
#if UNITY_EDITOR
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mono = target as MonoBehaviour;
            var methods = mono.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MethodExecutorAttribute>();
                if (attr == null) continue;

                bool canExecute = Application.isPlaying || attr.CanExecuteInEditMode;
                GUI.enabled = canExecute;

                string buttonLabel = attr.ButtonName ?? method.Name;
                if (GUILayout.Button(buttonLabel))
                {
                    method.Invoke(mono, null);
                }

                if (!canExecute)
                {
                    EditorGUILayout.HelpBox($"{method.Name} このメソッドはランタイム中のみ実行できます", MessageType.Info);
                }

                GUI.enabled = true; // 元に戻す
            }
        }
#endif
    }
}