using System;
using System.Collections.Generic;
using System.Linq;
using MeshBreak.MeshBooleanOperator;
using UnityEditor;
using UnityEngine;

public class MeshEdgeUtilityWindow : EditorWindow
{
    private Mesh _mesh;
    private MeshEdgeDataList _meshEdgeDataList;

    [MenuItem("Window/MeshEdgeUtility")]
    private static void ShowWindow()
    {
        GetWindow<MeshEdgeUtilityWindow>();
    }

    private void OnGUI()
    {
        _mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", _mesh, typeof(Mesh), true);
        _meshEdgeDataList = (MeshEdgeDataList)EditorGUILayout.ObjectField(("MeshEdgeDataList"), _meshEdgeDataList,
            typeof(MeshEdgeDataList), true);

        if (_mesh || _meshEdgeDataList) return;

        if (GUILayout.Button("Bake"))
        {
            Undo.RecordObject(_meshEdgeDataList, "Bake MeshEdgeData");
            
            var newData = new MeshEdgeData();
            newData.Edges = BakeMeshEdge();
            _meshEdgeDataList.data.Add(newData);

            EditorUtility.SetDirty(_meshEdgeDataList);
        }
    }

    private List<EdgeData> BakeMeshEdge()
    {
        HashSet<EdgeData> edges = new HashSet<EdgeData>();
        int subMeshCount = _mesh.subMeshCount;

        for (int i = 0; i < subMeshCount; i++)
        {
            var subMesh = _mesh.GetTriangles(i);

            for (int j = 0; j < subMesh.Length; j += 3)
            {
                int v1 = subMesh[j];
                int v2 = subMesh[j + 1];
                int v3 = subMesh[j + 2];

                edges.Add(new EdgeData(v1, v2));
                edges.Add(new EdgeData(v1, v3));
                edges.Add(new EdgeData(v2, v3));
            }
        }

        return edges.ToList();
    }
}