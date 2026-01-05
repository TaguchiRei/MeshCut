using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace XenoSiteFactory.Editor
{
    /// <summary>
    /// InputActionAssetからActionMapとActionのenumを自動生成するエディタ拡張
    /// </summary>
    public class InputActionEnumGenerator : EditorWindow
    {
        /// <summary>
        /// 指定された設定でenumファイルを生成します。
        /// </summary>
        /// <param name="asset">対象のInputActionAsset</param>
        /// <param name="folderPath">出力先フォルダパス (例: "Assets/Code/Generated")</param>
        /// <param name="ns">名前空間</param>
        /// <returns>成功したかどうか</returns>
        public static bool Generate(InputActionAsset asset, string folderPath, string ns)
        {
            if (asset == null) return false;

            string assetName = asset.name;
            string directoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folderPath));
            string filePath = Path.Combine(directoryPath, $"ActionMapEnum.cs");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var sb = new StringBuilder();

            sb.AppendLine("// 自動生成ファイルの為、手動での編集は上書きされます。");

            // 1. ActionMapのenumを生成
            string actionMapEnumName = $"ActionMaps";
            sb.AppendLine($"public enum {actionMapEnumName}");
            sb.AppendLine("{");
            int i = 0;
            foreach (var map in asset.actionMaps)
            {
                sb.AppendLine($"    {SanitizeName(map.name)} = {i},");
                i++;
            }

            sb.AppendLine("}");
            sb.AppendLine();

            // 2. 各ActionMapに対応するActionのenumを生成
            foreach (var map in asset.actionMaps)
            {
                string actionEnumName = $"{SanitizeName(map.name)}Actions";
                sb.AppendLine($"public enum {actionEnumName}");
                sb.AppendLine("{");

                i = 0;
                foreach (var action in map.actions)
                {
                    sb.AppendLine($"    {SanitizeName(action.name)} = {i},");
                    i++;
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }

            try
            {
                File.WriteAllText(filePath, sb.ToString());
                Debug.Log($"[InputActionEnumGenerator] Generated enums at: {filePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InputActionEnumGenerator] Failed to write file at {filePath}. Exception: {e}");
                return false;
            }
        }

        private static string SanitizeName(string name)
        {
            string sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
            if (string.IsNullOrEmpty(sanitized))
            {
                return "_";
            }

            if (char.IsDigit(sanitized[0]))
            {
                return "_" + sanitized;
            }

            return sanitized;
        }
    }
}