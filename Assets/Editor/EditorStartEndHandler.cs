using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorStartupHandler
{
    private const string InitKey = "MyTool_Initialized";

    static EditorStartupHandler()
    {
        if (SessionState.GetBool(InitKey, false))
            return; // このエディタセッション中に既に実行済みならスキップ

        SessionState.SetBool(InitKey, true);
        Debug.Log("Unityエディタ起動時にのみ実行されました。");

    }

    private static void OnEditorQuit()
    {
        Debug.Log("Unityエディタ終了時に実行されました。");
    }
}