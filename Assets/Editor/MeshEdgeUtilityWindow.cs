using System;
using MeshBreak.MeshBooleanOperator;
using UnityEditor;
using UnityEngine;

public class MeshEdgeUtilityWindow : EditorWindow
{
    private Mesh _mesh;

    [MenuItem("Window/MeshEdgeUtility")]
    private static void ShowWindow()
    {
        GetWindow<MeshEdgeUtilityWindow>();
    }

    private void OnGUI()
    {
        _mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", _mesh, typeof(Mesh), true);

        if (_mesh == null) return;

        if (GUILayout.Button("Bake"))
        {
        }
    }

    private MeshEdgeData BakeMeshEdge()
    {
        MeshEdgeData data = new MeshEdgeData();

        int subMeshCount = _mesh.subMeshCount;

        for (int i = 0; i < subMeshCount; i++)
        {
        }

        return data;
    }
}