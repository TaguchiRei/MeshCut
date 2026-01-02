using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using MeshBreak;
using MeshBreak.MeshBooleanOperator;
using UnityEngine;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

namespace Code.Scripts.MeshBreak.MeshBooleanOperator
{
    public class MeshBooleanOperatorClassic : MonoBehaviour
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _target2;
        [SerializeField] private Material _capMat;

        private BreakMeshData _insideMeshData;
        private BreakMeshData _outsideMeshData;

        private Mesh _targetMesh;
        private Vector3[] _targetVertices;
        private Vector3[] _targetNormals;
        private Vector2[] _targetUVs;

        private Mesh _booleanMesh;
        private Vector3[] _booleanVertices;
        private Vector3[] _booleanNormals;
        private Vector2[] _booleanUVs;

        private bool[] _baseVerticesIsInside;

        private TriangleData _triangleData;


        [MethodExecutor]
        private void Test()
        {
            Boolean(_target, _target2, _capMat).Forget();
        }

        public async UniTask<GameObject[]> Boolean(GameObject target, GameObject booleanMesh, Material capMaterial)
        {
            _booleanMesh = booleanMesh.GetComponent<MeshFilter>().mesh;
            _targetMesh = target.GetComponent<MeshFilter>().mesh;

            _targetVertices = _targetMesh.vertices;
            _targetNormals = _targetMesh.normals;
            _targetUVs = _targetMesh.uv;

            _booleanVertices = _booleanMesh.vertices;
            _booleanNormals = _booleanMesh.normals;
            _booleanUVs = _booleanMesh.uv;

            //そこまで大きくないメッシュですらこの時点で平均5秒前後、最悪10秒程度かかっている
            Debug.Log(_targetVertices.Length);
            float a = 0;
            foreach (var t in _targetVertices)
            {
#if UNITY_EDITOR
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
#endif
                await MeshCalculationSupport.CheckInsideMesh(_booleanVertices, _booleanMesh.triangles, t);

                stopwatch.Stop();
                a += stopwatch.ElapsedMilliseconds;
            }

            Debug.Log($"平均{a / _targetMesh.vertices.Length}ms");

            return null;
        }
    }
}