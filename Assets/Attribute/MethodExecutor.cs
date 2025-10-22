using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodExecutorAttribute : System.Attribute
    {
        public string ButtonName { get; }
        public bool CanExecuteInEditMode { get; }

        public MethodExecutorAttribute(string buttonName, bool canExecuteInEditMode)
        {
            ButtonName = buttonName;
            CanExecuteInEditMode = canExecuteInEditMode;
        }
    }


    [CustomEditor(typeof(MonoBehaviour), true)]
    public class InspectorButtonEditor : UnityEditor.Editor
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