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

    private List<Vector3> SamplingVector3(float radius, Vector3 maxPosition, Vector3 minPosition)
    {
        List<Vector3> generatedVerts = new(); //生成された頂点
        Dictionary<Vector3Int, List<Vector3>> checkGrid = new(); //グリッドごとに設置の可否を保存する。
        List<Vector3> activeVerts = new(); //未探査の頂点

        float cellSize = radius / Mathf.Sqrt(3); //グリッドのセルサイズ

        Vector3 firstVert = new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z));

        generatedVerts.Add(firstVert);
        activeVerts.Add(firstVert);
        AddToGrid(checkGrid, firstVert, cellSize);

        while (activeVerts.Count > 0)
        {
            int index = Random.Range(0, activeVerts.Count);
            Vector3 currentPos = activeVerts[index];
            bool found = false;
            for (int i = 0; i < _tryCheck; i++)
            {
                
            }
        }
    }

    private void AddToGrid(Dictionary<Vector3Int, List<Vector3>> grid, Vector3 point, float cellSize)
    {
        Vector3Int key = GetGridPos(point, cellSize);
        if (!grid.ContainsKey(key))
            grid[key] = new List<Vector3>();
        grid[key].Add(point);
    }

    private Vector3Int GetGridPos(Vector3 point, float cellSize)
    {
        return new Vector3Int(
            Mathf.FloorToInt(point.x / cellSize),
            Mathf.FloorToInt(point.y / cellSize),
            Mathf.FloorToInt(point.z / cellSize)
        );
    }

    /// <summary>
    /// 指定座標を中心とした新たな座標を作成
    /// </summary>
    /// <param name="center">中心</param>
    /// <param name="minRadius">最小半径</param>
    /// <param name="maxRadius">最大半径</param>
    /// <returns></returns>
    private Vector3 GenerateRandomVert(Vector3 center, float minRadius, float maxRadius)
    {
        float r = Random.Range(minRadius, maxRadius);
        Vector3 randomDirection = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)) * Vector3.right;
        return center + randomDirection * r;
    }

    /// <summary>
    /// 指定座標を中心とした新たな座標を作成
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private Vector3 GenerateRandomVert(Vector3 center, float radius)
    {
        Vector3 randomDirection = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)) * Vector3.right;
        return center + randomDirection * radius;
    }
}