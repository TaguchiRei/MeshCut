using UnityEditor;
using UnityEngine.InputSystem;

namespace XenoSiteFactory.Editor
{
    /// <summary>
    /// .inputactions ファイルの変更を検知し、enumの自動生成をトリガーする
    /// </summary>
    public class InputActionAssetPostprocessor : AssetPostprocessor
    {
        private const string DefaultOutputFolderPath = @"Assets\Code\AutoGenerate";
        private const string DefaultNamespace = "XenoSiteFactory.Input.Generated";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool needsRefresh = false;
            foreach (string path in importedAssets)
            {
                if (path.EndsWith(".inputactions"))
                {
                    InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                    if (asset != null)
                    {
                        // 静的メソッドを呼び出してenumを生成
                        if (InputActionEnumGenerator.Generate(asset, DefaultOutputFolderPath, DefaultNamespace))
                        {
                            needsRefresh = true;
                        }
                    }
                }
            }

            // 複数のファイルが更新された場合でも、最後に一度だけリフレッシュを実行する
            if (needsRefresh)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
