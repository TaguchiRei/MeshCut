using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    /// <summary>
    /// 切断されたメッシュのデータを保持するためのクラス。
    /// 頂点の重複防止機能を備える。
    /// </summary>
    public class CutMeshData
    {
        public Vector3[] Vertices { get; private set; }
        public Vector3[] Normals { get; private set; }
        public Vector2[] Uvs { get; private set; }
        public int VertexCount { get; private set; }
        public List<List<int>> SubIndices { get; private set; }

        private readonly Vector3[] _baseMeshVertices;
        private readonly Vector3[] _baseMeshNormals;
        private readonly Vector2[] _baseUvs;
        private readonly int[] _indexMap;

        public CutMeshData(
            Vector3[] baseMeshVertices, Vector3[] baseMeshNormals, Vector2[] baseUvs,
            Vector3[] vertexBuffer, Vector3[] normalBuffer, Vector2[] uvBuffer)
        {
            Vertices = vertexBuffer;
            Normals = normalBuffer;
            Uvs = uvBuffer;
            VertexCount = 0;
            SubIndices = new List<List<int>>();

            _baseMeshVertices = baseMeshVertices;
            _baseMeshNormals = baseMeshNormals;
            _baseUvs = baseUvs;

            _indexMap = new int[baseMeshVertices.Length];
            Array.Fill(_indexMap, -1);
        }

        public void AddSubMesh()
        {
            SubIndices.Add(new List<int>());
        }

        public void AddTriangle(int p1, int p2, int p3, int submesh)
        {
            SubIndices[submesh].Add(GetOrAddVertex(p1));
            SubIndices[submesh].Add(GetOrAddVertex(p2));
            SubIndices[submesh].Add(GetOrAddVertex(p3));
        }

        public void AddTriangle(TriangleData triangleData, Vector3 faceNormal, int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                triangleData.Vertex1 - triangleData.Vertex0,
                triangleData.Vertex2 - triangleData.Vertex0);

            int baseIndex = VertexCount;

            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
                SubIndices[submesh].Add(baseIndex);
                SubIndices[submesh].Add(baseIndex + 1);
                SubIndices[submesh].Add(baseIndex + 2);

                Vertices[VertexCount] = triangleData.Vertex2;
                Vertices[VertexCount + 1] = triangleData.Vertex1;
                Vertices[VertexCount + 2] = triangleData.Vertex0;
                Normals[VertexCount] = triangleData.Normal2;
                Normals[VertexCount + 1] = triangleData.Normal1;
                Normals[VertexCount + 2] = triangleData.Normal0;
                Uvs[VertexCount] = triangleData.UV2;
                Uvs[VertexCount + 1] = triangleData.UV1;
                Uvs[VertexCount + 2] = triangleData.UV0;
            }
            else
            {
                SubIndices[submesh].Add(baseIndex);
                SubIndices[submesh].Add(baseIndex + 1);
                SubIndices[submesh].Add(baseIndex + 2);

                Vertices[VertexCount] = triangleData.Vertex0;
                Vertices[VertexCount + 1] = triangleData.Vertex1;
                Vertices[VertexCount + 2] = triangleData.Vertex2;
                Normals[VertexCount] = triangleData.Normal0;
                Normals[VertexCount + 1] = triangleData.Normal1;
                Normals[VertexCount + 2] = triangleData.Normal2;
                Uvs[VertexCount] = triangleData.UV0;
                Uvs[VertexCount + 1] = triangleData.UV1;
                Uvs[VertexCount + 2] = triangleData.UV2;
            }

            VertexCount += 3;
        }

        private int GetOrAddVertex(int originalIndex)
        {
            if (_indexMap[originalIndex] != -1)
                return _indexMap[originalIndex];

            int newIndex = VertexCount++;
            _indexMap[originalIndex] = newIndex;

            Vertices[newIndex] = _baseMeshVertices[originalIndex];
            Normals[newIndex] = _baseMeshNormals[originalIndex];
            Uvs[newIndex] = _baseUvs[originalIndex];

            return newIndex;
        }
    }
}