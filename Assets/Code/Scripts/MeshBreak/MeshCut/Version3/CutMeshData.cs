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
        public List<Vector3> Vertices { get; private set; }
        public List<Vector3> Normals { get; private set; }
        public List<Vector2> Uvs { get; private set; }
        public List<List<int>> SubIndices { get; private set; }

        private readonly Vector3[] _baseMeshVertices;
        private readonly Vector3[] _baseMeshNormals;
        private readonly Vector2[] _baseUvs;

        /// <summary> 元のメッシュのインデックスに対応する、新メッシュでのインデックスマップ </summary>
        private readonly int[] _indexMap;

        public CutMeshData(Vector3[] baseMeshVertices, Vector3[] baseMeshNormals, Vector2[] baseUvs)
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Uvs = new List<Vector2>();
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

        /// <summary>
        /// 元のメッシュのインデックスを使用して三角形を追加する
        /// </summary>
        public void AddTriangle(int p1, int p2, int p3, int submesh)
        {
            SubIndices[submesh].Add(GetOrAddVertex(p1));
            SubIndices[submesh].Add(GetOrAddVertex(p2));
            SubIndices[submesh].Add(GetOrAddVertex(p3));
        }

        /// <summary>
        /// 新しく生成された頂点データを使用して三角形を追加する（断面など）
        /// </summary>
        public void AddTriangle(TriangleData triangleData, Vector3 faceNormal, int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                triangleData.Vertex1 - triangleData.Vertex0,
                triangleData.Vertex2 - triangleData.Vertex0);

            int baseIndex = Vertices.Count;

            // 法線方向に基づいて頂点の順序（巻き）を調整
            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
                SubIndices[submesh].Add(baseIndex);
                SubIndices[submesh].Add(baseIndex + 1);
                SubIndices[submesh].Add(baseIndex + 2);

                Vertices.Add(triangleData.Vertex2);
                Vertices.Add(triangleData.Vertex1);
                Vertices.Add(triangleData.Vertex0);

                Normals.Add(triangleData.Normal2);
                Normals.Add(triangleData.Normal1);
                Normals.Add(triangleData.Normal0);

                Uvs.Add(triangleData.UV2);
                Uvs.Add(triangleData.UV1);
                Uvs.Add(triangleData.UV0);
            }
            else
            {
                SubIndices[submesh].Add(baseIndex);
                SubIndices[submesh].Add(baseIndex + 1);
                SubIndices[submesh].Add(baseIndex + 2);

                Vertices.Add(triangleData.Vertex0);
                Vertices.Add(triangleData.Vertex1);
                Vertices.Add(triangleData.Vertex2);

                Normals.Add(triangleData.Normal0);
                Normals.Add(triangleData.Normal1);
                Normals.Add(triangleData.Normal2);

                Uvs.Add(triangleData.UV0);
                Uvs.Add(triangleData.UV1);
                Uvs.Add(triangleData.UV2);
            }
        }

        /// <summary>
        /// 頂点重複を防止しつつ、新しいメッシュでのインデックスを返す
        /// </summary>
        private int GetOrAddVertex(int originalIndex)
        {
            if (_indexMap[originalIndex] != -1)
            {
                return _indexMap[originalIndex];
            }

            int newIndex = Vertices.Count;
            _indexMap[originalIndex] = newIndex;

            Vertices.Add(_baseMeshVertices[originalIndex]);
            Normals.Add(_baseMeshNormals[originalIndex]);
            Uvs.Add(_baseUvs[originalIndex]);

            return newIndex;
        }
    }
}