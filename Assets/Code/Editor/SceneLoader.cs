using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : EditorWindow
{
    private string[] onListScenes;
    private string[] outListScenes;
    private Vector2 onListScroll;
    private Vector2 outListScroll;

    [MenuItem("Window/UsefulTools/Scene Loader")]
    public static void ShowWindow()
    {
        GetWindow<SceneLoader>("Scene Loader");
    }


    private void OnGUI()
    {
        if (GUILayout.Button("シーンリストを更新"))
        {
            onListScenes = Enum.GetNames(typeof(InListSceneName));
            outListScenes = Enum.GetNames(typeof(OutListSceneName));
        }

        if (onListScenes == null) return;
        // 左側
        EditorGUILayout.LabelField("On List Scenes", EditorStyles.boldLabel);
        onListScroll = EditorGUILayout.BeginScrollView(onListScroll);

        if (onListScenes != null)
        {
            foreach (var scene in onListScenes)
            {
                if (GUILayout.Button(scene))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Level/Scenes/{scene}.unity");
                }
            }
        }

        EditorGUILayout.EndScrollView();

        // 右側
        EditorGUILayout.LabelField("Out List Scenes", EditorStyles.boldLabel);
        outListScroll = EditorGUILayout.BeginScrollView(outListScroll);

        if (outListScenes != null)
        {
            foreach (var scene in outListScenes)
            {
                if (GUILayout.Button(scene))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Level/Scenes/{scene}.unity");
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }
}