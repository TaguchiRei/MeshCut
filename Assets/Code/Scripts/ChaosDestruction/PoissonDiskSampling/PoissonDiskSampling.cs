using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PoissonDiskSampling : MonoBehaviour
{
    [SerializeField] private float _radius;
    private List<Vector3> _rangeVerts;
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private float scale = 1.0f;
    [SerializeField] private int _tryCheck = 18;
    [SerializeField] private int _maxGenerate = 1000;

    private void Start()
    {
        _rangeVerts = new();
        float xMax = 20f;
        float xMin = 0f;
        float yMax = 20f;
        float yMin = 0f;
        float zMax = 20f;
        float zMin = 0f;

        SamplingVector3(_radius, new Vector3(xMax, yMax, zMax), new Vector3(xMin, yMin, zMin));
    }

    private void SamplingVector3(float radius,Vector3 maxPosition, Vector3 minPosition)
    {
        List<Vector3> generatedVerts = new();//生成された頂点
        Dictionary<Vector3Int, List<Vector3>> checkGrid = new();//グリッドごとに設置の可否を保存する。
        List<Vector3> activeVerts = new();//未探査の頂点

        Vector3 firstVert = new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z));
        
        generatedVerts.Add(firstVert);
        activeVerts.Add(firstVert);
        
    }
}