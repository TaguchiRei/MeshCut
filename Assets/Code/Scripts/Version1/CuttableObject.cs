using System.Collections.Generic;
using System.Diagnostics;
using Attribute;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class CuttableObject : MonoBehaviour
{
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;
    public MeshCollider MeshCollider;
    public Material CutFaceMaterial;

    private List<SphereCollider> _colliders = new();

    [SerializeField] private int _maxSamplingVert = 300;
    [SerializeField] private int _colliderNum;
    [SerializeField] private float _centerRate = 0.7f;

    private int parentHash;

    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        Debug.Log($"オブジェクト{gameObject.name}  頂点数{mesh.vertexCount}");
    }

    public void SetParentHash(int hash)
    {
        parentHash = hash;
    }


    [MethodExecutor("当たり判定を単純化する", false)]
    public bool ColliderWeightReduction(List<Vector3> verts, Vector3[] cutFaceCenterPos = null)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        List<Vector3> clusteringSamples = new();
        if (verts.Count >= _maxSamplingVert)
        {
            for (int i = 0; i < verts.Count; i += verts.Count / _maxSamplingVert)
            {
                clusteringSamples.Add(verts[i]);
            }

            Debug.Log(clusteringSamples.Count + "サンプリングした頂点数");
        }
        else
        {
            return false;
        }

        if (cutFaceCenterPos != null)
        {
            clusteringSamples.AddRange(cutFaceCenterPos);
        }

        var centers = ClusteringVerts(clusteringSamples);
        Vector3 center = Vector3.zero;

        for (int i = 0; i < clusteringSamples.Count; i++)
        {
            center += clusteringSamples[i];
        }

        center /= clusteringSamples.Count;

        foreach (var clusterCenter in centers)
        {
            Vector3 colliderCenter = Vector3.Lerp(clusterCenter, center, _centerRate);

            Vector3 mostNearVertexPos = Vector3.zero;
            float distance = float.MaxValue;

            foreach (var vert in clusteringSamples)
            {
                float newDistance = Vector3.Distance(vert, colliderCenter);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    mostNearVertexPos = vert;
                }
            }

            float radius = (colliderCenter - mostNearVertexPos).magnitude;

            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = radius;
            sphereCollider.center = colliderCenter;
            _colliders.Add(sphereCollider);
        }

        MeshCollider.enabled = false;
        Debug.Log($"コライダー軽量化完了。処理時間{stopwatch.ElapsedMilliseconds}ms");
        return true;
    }

    private List<Vector3> ClusteringVerts(List<Vector3> clusteringSample)
    {
        _colliderNum = Mathf.Max(_colliderNum, 10);

        List<Vector3> centers = new();
        List<List<int>> nearVertex = new();

        //クラスタリングの範囲を捜索するための変数
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float maxZ = float.MinValue;
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;

        //クラスタリングの範囲の最大値と最小値をサンプルの各座標の最大値と最小値から取得する
        foreach (var sample in clusteringSample)
        {
            if (maxX < sample.x) maxX = sample.x;
            if (maxY < sample.y) maxY = sample.y;
            if (maxZ < sample.z) maxZ = sample.z;

            if (minX > sample.x) minX = sample.x;
            if (minY > sample.y) minY = sample.y;
            if (minZ > sample.z) minZ = sample.z;
        }

        for (int i = 0; i < _colliderNum - 8; i++)
        {
            centers.Add(new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), Random.Range(minZ, maxZ)));
            nearVertex.Add(new List<int>());
        }

        for (int i = 0; i < 8; i++)
        {
            nearVertex.Add(new List<int>());
        }

        //8この隅には必ず重心を作成する
        float midX = (minX + maxX) / 2;
        float midY = (minY + maxY) / 2;
        float midZ = (minZ + maxZ) / 2;

        // 十字型に6個配置（中心は midX, midY, midZ）
        centers.Add(new Vector3(midX, midY, maxZ)); // Z方向正
        centers.Add(new Vector3(midX, midY, minZ)); // Z方向負
        centers.Add(new Vector3(midX, maxY, midZ)); // Y方向正
        centers.Add(new Vector3(midX, minY, midZ)); // Y方向負
        centers.Add(new Vector3(maxX, midY, midZ)); // X方向正
        centers.Add(new Vector3(minX, midY, midZ)); // X方向負


        while (true)
        {
            foreach (var nears in nearVertex)
            {
                nears.Clear();
            }

            //重心と頂点を紐づける
            for (int i = 0; i < clusteringSample.Count; i++)
            {
                int mostNear = 0;
                float distance = float.MaxValue;
                for (int j = 0; j < centers.Count; j++)
                {
                    float newDistance = Vector3.Distance(centers[j], clusteringSample[i]);
                    if (distance > newDistance)
                    {
                        distance = newDistance;
                        mostNear = j;
                    }
                }

                nearVertex[mostNear].Add(i);
            }

            //重心の位置を頂点の重心に移動する
            bool changePosition = false;
            for (int i = 0; i < centers.Count; i++)
            {
                if (nearVertex[i].Count == 0) continue;

                var center = centers[i];
                Vector3 newPosition = Vector3.zero;

                for (int j = 0; j < nearVertex[i].Count; j++)
                {
                    newPosition += clusteringSample[nearVertex[i][j]];
                }

                newPosition /= nearVertex[i].Count;

                if ((newPosition - center).sqrMagnitude > 1e-6f)
                {
                    changePosition = true;
                    centers[i] = newPosition;
                }
            }

            if (!changePosition)
            {
                return centers;
            }
        }
    }
}