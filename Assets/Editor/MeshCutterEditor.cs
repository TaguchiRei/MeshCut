using UnityEditor;
using UnityEngine;

// この属性でTestMeshCutter専用のInspectorを作る
[CustomEditor(typeof(TestMeshCutter))]
public class MeshCutterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 元のInspector描画を維持
        DrawDefaultInspector();
        
        // ボタン追加
        TestMeshCutter cutter = (TestMeshCutter)target;
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("メッシュをカット"))
            {
                cutter.CutMesh();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("PlayMode中にのみ実行できます", MessageType.Info);
        }
    }
}