using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Version2
{
    public struct CutObjectData
    {
        private readonly List<Vector3> _leftVertices;
        private readonly List<Vector3> _leftNormals;
        private readonly List<Vector2> _leftUvs;
        private readonly List<List<int>> _leftSubIndices;
        private readonly int[] _leftAddVerticesArray;

        private readonly List<Vector3> _rightVertices;
        private readonly List<Vector3> _rightNormals;
        private readonly List<Vector2> _rightUvs;
        private readonly List<List<int>> _rightSubIndices;
        private readonly int[] _rightAddVerticesArray;

        private readonly List<Vector3> _centers;
        private readonly Dictionary<Vector3, List<Vector3>> _capConnections;
        
        private readonly Vector3[] _baseVertices;
        private readonly Vector3[] _baseNormals;
        private readonly Vector2[] _baseUVs;

        private bool[] _baseVerticesSide;
        
        
        public CutObjectData(int baseVerticesLength)
        {
            _leftNormals = new List<Vector3>();
            _leftUvs = new List<Vector2>();
            _leftSubIndices = new List<List<int>>();
            _rightVertices = new List<Vector3>();
            _rightNormals = new List<Vector3>();
            _rightUvs = new List<Vector2>();
            _rightSubIndices = new List<List<int>>();
            _centers = new List<Vector3>();
            _capConnections = new Dictionary<Vector3, List<Vector3>>();
            _leftVertices = new List<Vector3>();
            
            _leftAddVerticesArray = new int[baseVerticesLength];
            _rightAddVerticesArray = new int[baseVerticesLength];

            Array.Fill(_leftAddVerticesArray, -1);
            Array.Fill(_rightAddVerticesArray, -1);
            _baseVertices = new Vector3[] { };
            _baseNormals = new Vector3[] { };
            _baseUVs = new Vector2[] { };
            _baseVerticesSide = new bool[] { };
        }

        /// <summary>
        /// すべてのリストを初期化
        /// </summary>
        private void ClearAll()
        {
            _leftVertices.Clear();
            _leftNormals.Clear();
            _leftUvs.Clear();
            _leftSubIndices.Clear();

            _rightVertices.Clear();
            _rightNormals.Clear();
            _rightUvs.Clear();
            _rightSubIndices.Clear();
            _centers.Clear();
        }

        #region 左側の処理

        private int GetOrAddVertexLeft(int index)
        {
            if (_leftAddVerticesArray[index] != -1)
            {
                return _leftAddVerticesArray[index];
            }

            int newIndex = _leftVertices.Count;
            _leftAddVerticesArray[index] = newIndex;

            _leftVertices.Add(_baseVertices[index]);
            _leftNormals.Add(_baseNormals[index]);
            _leftUvs.Add(_baseUVs[index]);

            return newIndex;
        }

        private void AddTriangleLeft(int p1, int p2, int p3, int submesh)
        {
            int p1Index = GetOrAddVertexLeft(p1);
            int p2Index = GetOrAddVertexLeft(p2);
            int p3Index = GetOrAddVertexLeft(p3);

            _leftSubIndices[submesh].Add(p1Index);
            _leftSubIndices[submesh].Add(p2Index);
            _leftSubIndices[submesh].Add(p3Index);
        }

        private void AddTriangleLeft(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal,
            int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                points3[1] - points3[0],
                points3[2] - points3[0]);

            int p1 = 0;
            int p2 = 1;
            int p3 = 2;

            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
                p1 = 2;
                p2 = 1;
                p3 = 0;
            }

            int baseIndex = _leftVertices.Count;

            _leftSubIndices[submesh].Add(baseIndex + 0);
            _leftSubIndices[submesh].Add(baseIndex + 1);
            _leftSubIndices[submesh].Add(baseIndex + 2);

            _leftVertices.Add(points3[p1]);
            _leftVertices.Add(points3[p2]);
            _leftVertices.Add(points3[p3]);

            _leftNormals.Add(normals3[p1]);
            _leftNormals.Add(normals3[p2]);
            _leftNormals.Add(normals3[p3]);

            _leftUvs.Add(uvs3[p1]);
            _leftUvs.Add(uvs3[p2]);
            _leftUvs.Add(uvs3[p3]);
        }

        #endregion

        #region 右側の処理

        private int GetOrAddVertexRight(int index)
        {
            if (_rightAddVerticesArray[index] != -1)
            {
                return _rightAddVerticesArray[index];
            }

            int newIndex = _rightVertices.Count;
            _rightAddVerticesArray[index] = newIndex;

            _rightVertices.Add(_baseVertices[index]);
            _rightNormals.Add(_baseNormals[index]);
            _rightUvs.Add(_baseUVs[index]);

            return newIndex;
        }

        private void AddTriangleRight(int p1, int p2, int p3, int submesh)
        {
            int p1Index = GetOrAddVertexRight(p1);
            int p2Index = GetOrAddVertexRight(p2);
            int p3Index = GetOrAddVertexRight(p3);

            _rightSubIndices[submesh].Add(p1Index);
            _rightSubIndices[submesh].Add(p2Index);
            _rightSubIndices[submesh].Add(p3Index);
        }

        private void AddTriangleRight(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal,
            int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                (points3[1] - points3[0]).normalized,
                (points3[2] - points3[0]).normalized);

            int p1 = 0;
            int p2 = 1;
            int p3 = 2;

            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
                p1 = 2;
                p2 = 1;
                p3 = 0;
            }

            int baseIndex = _rightVertices.Count;

            _rightSubIndices[submesh].Add(baseIndex + 0);
            _rightSubIndices[submesh].Add(baseIndex + 1);
            _rightSubIndices[submesh].Add(baseIndex + 2);

            _rightVertices.Add(points3[p1]);
            _rightVertices.Add(points3[p2]);
            _rightVertices.Add(points3[p3]);

            _rightNormals.Add(normals3[p1]);
            _rightNormals.Add(normals3[p2]);
            _rightNormals.Add(normals3[p3]);

            _rightUvs.Add(uvs3[p1]);
            _rightUvs.Add(uvs3[p2]);
            _rightUvs.Add(uvs3[p3]);
        }

        #endregion
    }
}