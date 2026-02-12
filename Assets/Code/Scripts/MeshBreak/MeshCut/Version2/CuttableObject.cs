using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UsefulAttribute;
using Random = UnityEngine.Random;

public class CuttableObject : MonoBehaviour
{
    public Action ReuseAction;

    public Material CapMaterial;
    public Mesh Mesh;

    [SerializeField] private PhysicsMaterial _physicsMaterial;
    [SerializeField] private int _colliderNum;

    [Header("Collider Radius Settings")] [SerializeField, Range(0.5f, 1f), Tooltip("基本縮小率")]
    private float _baseShrink = 0.95f;

    [SerializeField, Range(0.5f, 1f), Tooltip("低密度なクラスタの差異の最小縮小率")]
    private float _densityShrinkMin = 0.85f;

    [SerializeField, Min(1), Tooltip("密度閾値")]
    private int _densityThreshold = 10;

    [SerializeField, Min(0f), Tooltip("最大半径制限")]
    private float _maxRadius = 0.5f;


    private List<SphereCollider> _colliders;

    private void Start()
    {
        if (gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            Mesh = meshFilter.sharedMesh;
        }

        _colliders = new();
        for (int i = 0; i < _colliderNum; i++)
        {
            _colliders.Add(gameObject.AddComponent<SphereCollider>());
        }
    }

    [MethodExecutor]
    private void Test()
    {
        SetMesh(Mesh, Mesh.vertices.ToList(), new NativePlane(transform), _physicsMaterial);
    }

    public void SetMesh(
        Mesh mesh,
        List<Vector3> samplingVerts,
        NativePlane localBlade,
        PhysicsMaterial newFaceMat)
    {
        gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        //重心
        var centers = ClusteringVerts(samplingVerts);

        int sampleCount = samplingVerts.Count;
        int clusterCount = centers.Count;

        // 各頂点の所属クラスタ捜索
        int[] belongCluster = new int[sampleCount];
        int[] clusterVertCount = new int[clusterCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float minDist = float.MaxValue;
            int nearest = 0;

            for (int j = 0; j < clusterCount; j++)
            {
                float dist = (centers[j] - samplingVerts[i]).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = j;
                }
            }

            belongCluster[i] = nearest;
            clusterVertCount[nearest]++;
        }

        for (int i = 0; i < clusterCount; i++)
        {
            float minDistSq = float.MaxValue;

            for (int v = 0; v < sampleCount; v++)
            {
                if (belongCluster[v] != i) continue;

                float distSq = (centers[i] - samplingVerts[v]).sqrMagnitude;
                if (distSq < minDistSq)
                    minDistSq = distSq;
            }

            if (Mathf.Approximately(minDistSq, float.MaxValue))
                continue;

            float radius = Mathf.Sqrt(minDistSq);

            // 基本縮小
            radius *= _baseShrink;

            // 頂点密度による追加縮小
            if (clusterVertCount[i] < _densityThreshold)
            {
                float t = 1f - (clusterVertCount[i] / (float)_densityThreshold);
                float densityShrink = Mathf.Lerp(_baseShrink, _densityShrinkMin, t);
                radius *= densityShrink;
            }

            // 最大半径制限
            radius = Mathf.Min(radius, _maxRadius);

            // コライダー生成
            var col = _colliders[i];
            col.center = centers[i];
            col.radius = radius;
            col.material = newFaceMat;
        }

        for (int i = clusterCount; i < _colliders.Count; i++)
        {
            _colliders[i].enabled = false;
        }
    }

    /// <summary>
    /// クラスタリングを利用してコライダーの適切な位置を指定
    /// </summary>
    /// <param name="clusteringSample"></param>
    /// <returns></returns>
    private List<Vector3> ClusteringVerts(List<Vector3> clusteringSample)
    {
        //_colliderNum = Mathf.Max(_colliderNum, 10);

        int sampleCount = clusteringSample.Count;
        int clusterCount = _colliderNum;

        List<Vector3> centers = new(clusterCount);

        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float maxZ = float.MinValue;
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;

        for (int i = 0; i < sampleCount; i++)
        {
            var s = clusteringSample[i];

            if (s.x > maxX) maxX = s.x;
            if (s.y > maxY) maxY = s.y;
            if (s.z > maxZ) maxZ = s.z;

            if (s.x < minX) minX = s.x;
            if (s.y < minY) minY = s.y;
            if (s.z < minZ) minZ = s.z;
        }

        // ランダムな中心を作成
        for (int i = 0; i < clusterCount - 6; i++)
        {
            centers.Add(new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                Random.Range(minZ, maxZ)
            ));
        }

        float midX = (minX + maxX) * 0.5f;
        float midY = (minY + maxY) * 0.5f;
        float midZ = (minZ + maxZ) * 0.5f;

        centers.Add(new Vector3(midX, midY, maxZ));
        centers.Add(new Vector3(midX, midY, minZ));
        centers.Add(new Vector3(midX, maxY, midZ));
        centers.Add(new Vector3(midX, minY, midZ));
        centers.Add(new Vector3(maxX, midY, midZ));
        centers.Add(new Vector3(minX, midY, midZ));

        int[] belongCluster = new int[sampleCount];
        Vector3[] sum = new Vector3[clusterCount];
        int[] count = new int[clusterCount];

        const int maxIteration = 20;
        const float epsilon = 1e-6f;

        for (int iter = 0; iter < maxIteration; iter++)
        {
            // 初期化
            for (int i = 0; i < clusterCount; i++)
            {
                sum[i] = Vector3.zero;
                count[i] = 0;
            }

            // 近傍のクラスタを捜索
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 point = clusteringSample[i];

                float minDist = float.MaxValue;
                int nearest = 0;

                for (int j = 0; j < clusterCount; j++)
                {
                    float dist = (centers[j] - point).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = j;
                    }
                }

                belongCluster[i] = nearest;
                sum[nearest] += point;
                count[nearest]++;
            }

            // 重心移動
            bool moved = false;

            for (int i = 0; i < clusterCount; i++)
            {
                if (count[i] == 0) continue;

                Vector3 newCenter = sum[i] / count[i];

                if ((newCenter - centers[i]).sqrMagnitude > epsilon)
                {
                    centers[i] = newCenter;
                    moved = true;
                }
            }

            if (!moved)
                break;
        }

        return centers;
    }
}