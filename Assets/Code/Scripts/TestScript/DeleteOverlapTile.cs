using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DeleteOverlapTile : MonoBehaviour
{
    [SerializeField] private GameObject _blade;
    [SerializeField] private MeshFilter _meshFilter;
    private Mesh _targetMesh;
    private Plane _cutFace;


    private void Start()
    {
        _cutFace = new Plane(
            _meshFilter.gameObject.transform.InverseTransformDirection(_blade.transform.up),
            _meshFilter.gameObject.transform.InverseTransformPoint(_blade.transform.position));
        _targetMesh = _meshFilter.mesh;
        DeleteOverlap();
    }

    /// <summary>
    /// 計算用クラス
    /// </summary>
    private class Triangle
    {
        private Vector3 vertex1;
        private Vector3 vertex2;
        private Vector3 vertex3;

        public Vector3[] vertices;

        public void SetVertex(int index1, int index2, int index3)
        {
            vertex1 = vertices[index1];
            vertex2 = vertices[index2];
            vertex3 = vertices[index3];
        }

        public bool CheckOverlap(Plane plane)
        {
            bool v1 = plane.GetSide(vertex1);
            bool v2 = plane.GetSide(vertex2);
            bool v3 = plane.GetSide(vertex3);
            return v1 == v2 && v2 == v3;
        }
    }

    private void DeleteOverlap()
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        
        var triangles = _targetMesh.triangles.ToList();
        List<int> deleteIndex = new List<int>();
        Triangle triangle = new();
        triangle.vertices = _targetMesh.vertices;

        for (int i = 0; i < triangles.Count; i += 3)
        {
            triangle.SetVertex(triangles[i], triangles[i + 1], triangles[i + 2]);
            if (!triangle.CheckOverlap(_cutFace)) deleteIndex.Add(i);
        }

        Debug.Log(stopWatch.ElapsedMilliseconds);
        for (int i = deleteIndex.Count - 1; i >= 0; i--)
        {
            int index = deleteIndex[i];
            triangles.RemoveRange(index, 3);
        }

        Debug.Log(stopWatch.ElapsedMilliseconds);
        Mesh newMesh = new Mesh
        {
            vertices = triangle.vertices,
            triangles = triangles.ToArray()
        };
        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();
        _meshFilter.mesh = newMesh;
        Debug.Log(stopWatch.ElapsedMilliseconds);
    }
}