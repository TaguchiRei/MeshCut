using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class SceneEnumGenerate
{
    private const string OutputPath = "Assets/Code/AutoGenerate/SceneEnum.cs";

    static SceneEnumGenerate()
    {
        EditorBuildSettings.sceneListChanged += Generate;
        EditorSceneManager.newSceneCreated += OnNewSceneCreated;
        Generate();
    }

    private static void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.NewSceneSetup setup, UnityEditor.SceneManagement.NewSceneMode mode)
    {
        Generate();
    }

    private static void Generate()
    {
        // BuildSettings に登録されているシーン
        var buildScenes = EditorBuildSettings.scenes;
        var includedScenes = buildScenes
            .Where(s => s.enabled)
            .Select(s => Path.GetFileNameWithoutExtension(s.path))
            .Distinct()
            .ToArray();

        // プロジェクト内の全シーンファイル
        var allScenePaths = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
        var allScenes = allScenePaths
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .Distinct()
            .ToArray();

        // 除外されたシーン
        var excludedScenes = allScenes.Except(includedScenes).ToArray();

        if (includedScenes.Length == 0 && excludedScenes.Length == 0) return;

        // 名前を正規化（enum に使えない文字を置換）
        for (int i = 0; i < includedScenes.Length; i++)
            includedScenes[i] = Regex.Replace(includedScenes[i], @"[^a-zA-Z0-9_]", "_");

        for (int i = 0; i < excludedScenes.Length; i++)
            excludedScenes[i] = Regex.Replace(excludedScenes[i], @"[^a-zA-Z0-9_]", "_");

        // コード生成
        StringBuilder code = new StringBuilder();
        code.AppendLine("// 自動生成ファイルの為、手動での編集は上書きされます。");
        code.AppendLine("public enum InListSceneName");
        code.AppendLine("{");
        foreach (var scene in includedScenes)
            code.AppendLine($"    {scene},");
        code.AppendLine("}\n");

        code.AppendLine("public enum OutListSceneName");
        code.AppendLine("{");
        foreach (var scene in excludedScenes)
            code.AppendLine($"    {scene},");
        code.AppendLine("}");

        // 書き出し処理
        string dir = Path.GetDirectoryName(OutputPath);
        if (string.IsNullOrEmpty(dir))
        {
            CreateFolder();
            Debug.Log($"Directory Created : {OutputPath}");
            return;
        }

        Directory.CreateDirectory(dir);
        File.WriteAllText(OutputPath, code.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }

    private static void CreateFolder()
    {
        string relativePath = "Code/AutoGenerate";
        string fullPath = Path.Combine(Application.dataPath, relativePath);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log($"フォルダ作成: {fullPath}");
        }
        else
        {
            Debug.Log("フォルダはすでに存在します: " + fullPath);
        }
    }
}