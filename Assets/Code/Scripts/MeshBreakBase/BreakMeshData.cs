using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak
{
    /// <summary>
    /// 破壊したメッシュのデータを保持するためのクラス
    /// </summary>
    public class BreakMeshData
    {
        public List<Vector3> Vertices;
        public List<Vector3> Normals;
        public List<Vector2> Uvs;
        public List<List<int>> _subIndices;

        private readonly Vector3[] _baseMeshVertices;
        private readonly Vector3[] _baseMeshNormals;
        private readonly Vector2[] _baseUvs;

        /// <summary> 追加した頂点の配列。重複追加の防止</summary>
        private readonly int[] _addVerticesArray;

        public BreakMeshData(Vector3[] baseMeshVertices, Vector3[] baseMeshNormals, Vector2[] baseUvs)
        {
            _subIndices = new List<List<int>>();
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Uvs = new List<Vector2>();

            _baseMeshVertices = baseMeshVertices;
            _baseMeshNormals = baseMeshNormals;
            _baseUvs = baseUvs;

            _addVerticesArray = new int[baseMeshVertices.Length];
            Array.Fill(_addVerticesArray, -1);
        }

        public void AddSubMesh()
        {
            _subIndices.Add(new List<int>());
        }

        public void AddTriangle(int p1, int p2, int p3, int submesh)
        {
            _subIndices[submesh].Add(GetOrAddVertex(p1));
            _subIndices[submesh].Add(GetOrAddVertex(p2));
            _subIndices[submesh].Add(GetOrAddVertex(p3));
        }

        public void AddTriangle(TriangleData triangleData, Vector3 faceNormal, int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                triangleData.Vertex1 - triangleData.Vertex0,
                triangleData.Vertex2 - triangleData.Vertex0);
            
            
            int baseIndex = Vertices.Count;
            
            _subIndices[submesh].Add(baseIndex);
            _subIndices[submesh].Add(baseIndex + 1);
            _subIndices[submesh].Add(baseIndex + 2);

            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
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

        private int GetOrAddVertex(int index)
        {
            if (_addVerticesArray[index] == -1)
            {
                return _addVerticesArray[index] = index;
            }

            int newIndex = Vertices.Count;
            _addVerticesArray[index] = newIndex;
            Vertices.Add(_baseMeshVertices[index]);
            Normals.Add(_baseMeshNormals[index]);
            Uvs.Add(_baseUvs[index]);
            return newIndex;
        }

        public Mesh ToMesh(string meshName)
        {
            Mesh mesh = new()
            {
                name = meshName
            };

            if (Vertices.Count > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.subMeshCount = _subIndices.Count;
            for (int i = 0; i < _subIndices.Count; i++)
            {
                mesh.SetIndices(_subIndices[i], MeshTopology.Triangles, i);
            }

            return mesh;
        }
    }

    public class TriangleData
    {
        //座標
        public Vector3 Vertex0;
        public Vector3 Vertex1;
        public Vector3 Vertex2;

        //法線
        public Vector3 Normal0;
        public Vector3 Normal1;
        public Vector3 Normal2;

        //UV座標
        public Vector2 UV0;
        public Vector2 UV1;
        public Vector2 UV2;

        public void SetVertexes(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
        {
            Vertex0 = vertex0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
        }

        public void SetNormals(Vector3 normal0, Vector3 normal1, Vector3 normal2)
        {
            Normal0 = normal0;
            Normal1 = normal1;
            Normal2 = normal2;
        }

        public void SetUVs(Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            UV0 = uv0;
            UV1 = uv1;
            UV2 = uv2;
        }
    }
}