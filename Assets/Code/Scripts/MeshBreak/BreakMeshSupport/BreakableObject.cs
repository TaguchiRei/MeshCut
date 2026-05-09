using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UsefulAttribute;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace MeshBreak
{
    public class BreakableObject : MonoBehaviour, IRecyclable
    {
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;
        public MeshCollider MeshCollider;
        public Material CutFaceMaterial;
        public int MeshId = 0;

        [SerializeField] private int _maxSamplingVert = 300;
        [SerializeField] private int _colliderNum;
        [SerializeField] private float _centerRate = 0.7f;
        [SerializeField] private Rigidbody _rigidbody;

        private readonly List<SphereCollider> _colliders = new();
        private readonly List<Vector3> _clusteringSamples = new();
        private readonly List<Vector3> _centers = new();
        private readonly List<List<int>> _nearVertex = new();

        /// <summary>
        /// 切断済み断片かどうか。
        /// trueの場合はMeshDataCacheではなくMeshFilter.meshから直接データを読む。
        /// </summary>
        public bool IsCutFragment { get; private set; } = false;


        private void Awake()
        {
            _colliderNum = Mathf.Max(_colliderNum, 10);

            for (int i = 0; i < _colliderNum; i++)
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.enabled = false;
                _colliders.Add(col);
                _nearVertex.Add(new List<int>());
            }
        }

        private void Start()
        {
            var mesh = MeshFilter.mesh;
            Debug.Log($"オブジェクト{gameObject.name}  頂点数{mesh.vertexCount}　三角形の数{mesh.triangles.Length / 3}");
        }

        /// <summary>
        /// 切断済み断片としてマークする。断片生成時に呼ぶ。
        /// </summary>
        public void MarkAsCutFragment()
        {
            IsCutFragment = true;
        }

        #region 当たり判定の単純化

        public bool ColliderWeightReduction(Vector3[] verts, List<Vector3> cutFaceCenterPos = null,
            List<Vector3> oldCutFaces = null)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _clusteringSamples.Clear();

            if (verts.Length >= _maxSamplingVert)
            {
                for (int i = 0; i < verts.Length; i += verts.Length / _maxSamplingVert)
                {
                    _clusteringSamples.Add(verts[i]);
                }

                Debug.Log(_clusteringSamples.Count + "サンプリングした頂点数");
            }
            else
            {
                return false;
            }

            if (cutFaceCenterPos != null)
            {
                _clusteringSamples.AddRange(cutFaceCenterPos);
            }

            ClusteringVerts(_clusteringSamples);

            Vector3 center = Vector3.zero;
            for (int i = 0; i < _clusteringSamples.Count; i++)
            {
                center += _clusteringSamples[i];
            }

            center /= _clusteringSamples.Count;

            int index = 0;

            foreach (var clusterCenter in _centers)
            {
                if (index >= _colliders.Count) break;

                Vector3 colliderCenter = Vector3.Lerp(clusterCenter, center, _centerRate);

                Vector3 mostNearVertexPos = Vector3.zero;
                float distance = float.MaxValue;

                foreach (var vert in _clusteringSamples)
                {
                    float newDistance = Vector3.Distance(vert, colliderCenter);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        mostNearVertexPos = vert;
                    }
                }

                float radius = (colliderCenter - mostNearVertexPos).magnitude;

                var sphereCollider = _colliders[index];
                sphereCollider.center = colliderCenter;
                sphereCollider.radius = radius;
                sphereCollider.enabled = true;

                index++;
            }

            for (int i = index; i < _colliders.Count; i++)
            {
                _colliders[i].enabled = false;
            }

            MeshCollider.enabled = false;
            Debug.Log($"コライダー軽量化完了。処理時間{stopwatch.ElapsedMilliseconds}ms");
            return true;
        }

        [MethodExecutor("当たり判定を単純化する", false), Obsolete("List<Vector3>を利用する形式は旧型式です。配列を指定してください")]
        public bool ColliderWeightReduction(List<Vector3> verts, List<Vector3> cutFaceCenterPos = null,
            List<Vector3> oldCutFaces = null)
        {
            var vertsArr = verts.ToArray();
            return ColliderWeightReduction(vertsArr, cutFaceCenterPos, oldCutFaces);
        }

        private void ClusteringVerts(List<Vector3> clusteringSample)
        {
            _centers.Clear();
            foreach (var nears in _nearVertex) nears.Clear();

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            foreach (var sample in clusteringSample)
            {
                if (maxX < sample.x) maxX = sample.x;
                if (maxY < sample.y) maxY = sample.y;
                if (maxZ < sample.z) maxZ = sample.z;

                if (minX > sample.x) minX = sample.x;
                if (minY > sample.y) minY = sample.y;
                if (minZ > sample.z) minZ = sample.z;
            }

            for (int i = 0; i < _colliderNum - 6; i++)
            {
                _centers.Add(new Vector3(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY),
                    Random.Range(minZ, maxZ)
                ));
            }

            float midX = (minX + maxX) / 2;
            float midY = (minY + maxY) / 2;
            float midZ = (minZ + maxZ) / 2;

            _centers.Add(new Vector3(midX, midY, maxZ));
            _centers.Add(new Vector3(midX, midY, minZ));
            _centers.Add(new Vector3(midX, maxY, midZ));
            _centers.Add(new Vector3(midX, minY, midZ));
            _centers.Add(new Vector3(maxX, midY, midZ));
            _centers.Add(new Vector3(minX, midY, midZ));

            for (int k = 0; k < 20; k++)
            {
                foreach (var nears in _nearVertex) nears.Clear();

                for (int i = 0; i < clusteringSample.Count; i++)
                {
                    int mostNear = 0;
                    float distance = float.MaxValue;

                    for (int j = 0; j < _centers.Count; j++)
                    {
                        float newDistance = Vector3.Distance(_centers[j], clusteringSample[i]);
                        if (distance > newDistance)
                        {
                            distance = newDistance;
                            mostNear = j;
                        }
                    }

                    _nearVertex[mostNear].Add(i);
                }

                bool changePosition = false;

                for (int i = 0; i < _centers.Count; i++)
                {
                    if (_nearVertex[i].Count == 0) continue;

                    Vector3 newPosition = Vector3.zero;

                    for (int j = 0; j < _nearVertex[i].Count; j++)
                    {
                        newPosition += clusteringSample[_nearVertex[i][j]];
                    }

                    newPosition /= _nearVertex[i].Count;

                    if ((newPosition - _centers[i]).sqrMagnitude > 1e-6f)
                    {
                        changePosition = true;
                        _centers[i] = newPosition;
                    }
                }

                if (!changePosition) return;
            }
        }

        #endregion

        public int RecycleId { get; set; }

        public void OnRecycle()
        {
            foreach (var col in _colliders) col.enabled = false;

            if (MeshCollider != null) MeshCollider.enabled = true;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            // リサイクル時に断片フラグをリセット
            IsCutFragment = false;

            gameObject.SetActive(false);
        }
    }
}