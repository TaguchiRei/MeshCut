using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public class ProjectInitTool : EditorWindow
{
    [MenuItem("Window/ProjectInitTool")]
    public static void ShowWindow()
    {
        GetWindow<ProjectInitTool>("Project Init Tool");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("アセットを導入"))
        {
            InstallUniTask();
        }

        if (GUILayout.Button("ディレクトリ整理"))
        {
            DirectoryInit();
        }
    }

    #region ディレクトリ整理

    private void DirectoryInit()
    {
        CreateFolderUnderAssets("Assets", "Art");
        CreateFolderUnderAssets("Assets/Art", "Materials");
        CreateFolderUnderAssets("Assets/Art", "Models");
        CreateFolderUnderAssets("Assets/Art", "Textures");
        CreateFolderUnderAssets("Assets", "Audio");
        CreateFolderUnderAssets("Assets/Audio", "Music");
        CreateFolderUnderAssets("Assets/Audio", "Sound");
        CreateFolderUnderAssets("Assets", "Code");
        CreateFolderUnderAssets("Assets/Code", "Scripts");
        CreateFolderUnderAssets("Assets/Code", "Shaders");
        CreateFolderUnderAssets("Assets", "Docs");
        CreateFolderUnderAssets("Assets", "Level");
        CreateFolderUnderAssets("Assets/Level", "Prefabs");
        CreateFolderUnderAssets("Assets/Level", "Scenes");
        CreateFolderUnderAssets("Assets/Level", "UI");
        CreateFolderUnderAssets("Assets", "LocalAssets");
    }

    private static void CreateFolderUnderAssets(string folderPath,string folderName)
    {
        if (!AssetDatabase.IsValidFolder(folderPath + "/" +  folderName))
        {
            AssetDatabase.CreateFolder(folderPath, folderName);
            Debug.Log($"フォルダを作成しました: {folderPath}/{folderName}");
        }
        else
        {
            Debug.LogWarning($"すでに存在しています: {folderPath}/{folderName}");
        }
    }
    #endregion
    
    #region Asset導入

    private void InstallUniTask()
    {
        string packageUrl = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask";
        var request = Client.Add(packageUrl);
    }
    

    #endregion
}