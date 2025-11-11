using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class PoissonDiskSampling : MonoBehaviour
{
    [SerializeField] private float _radius;
    [SerializeField] private GameObject _positionPrefab;
    [SerializeField] private int _tryCheck = 18;

    private void Start()
    {
        float xMax = 5f;
        float xMin = 0f;
        float yMax = 5f;
        float yMin = 0f;
        float zMax = 5f;
        float zMin = 0f;

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var a = SamplingVector3(_radius, new Vector3(xMax, yMax, zMax), new Vector3(xMin, yMin, zMin));
        Debug.Log(stopwatch.ElapsedMilliseconds);
        foreach (var vector3 in a)
        {
            Instantiate(_positionPrefab, vector3, Quaternion.identity);
        }
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
                Vector3 newVert = GenerateRandomVert(currentPos, radius);

                if (IsInBounds(newVert, minPosition, maxPosition) && 
                    !IsTooClose(newVert, checkGrid, cellSize, radius))
                {
                    generatedVerts.Add(newVert);
                    activeVerts.Add(newVert);
                    AddToGrid(checkGrid, newVert, cellSize);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                activeVerts.RemoveAt(index);
            }
        }
        return generatedVerts;
    }

    /// <summary>
    /// グリッドに追加。
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="vert"></param>
    /// <param name="cellSize"></param>
    [Obsolete("副作用のあるメソッドなので要改善")]
    private void AddToGrid(Dictionary<Vector3Int, List<Vector3>> grid, Vector3 vert, float cellSize)
    {
        Vector3Int key = GetGridPos(vert, cellSize);
        if (!grid.ContainsKey(key))
            grid[key] = new List<Vector3>();
        grid[key].Add(vert);
    }

    /// <summary>
    /// グリッド座標を取得する
    /// </summary>
    /// <param name="vert"></param>
    /// <param name="cellSize"></param>
    /// <returns></returns>
    private Vector3Int GetGridPos(Vector3 vert, float cellSize)
    {
        return new Vector3Int(
            Mathf.FloorToInt(vert.x / cellSize),
            Mathf.FloorToInt(vert.y / cellSize),
            Mathf.FloorToInt(vert.z / cellSize)
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

    private bool IsInBounds(Vector3 vert, Vector3 min, Vector3 max)
    {
        return vert.x >= min.x && vert.x <= max.x &&
               vert.y >= min.y && vert.y <= max.y &&
               vert.z >= min.z && vert.z <= max.z;
    }
    
    /// <summary>
    /// 指定された頂点がほか頂点と近いかを調べるメソッド
    /// </summary>
    /// <param name="vertPos"></param>
    /// <param name="grid"></param>
    /// <param name="cellSize"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private bool IsTooClose(Vector3 vertPos, Dictionary<Vector3Int, List<Vector3>> grid, float cellSize, float radius)
    {
        Vector3Int key = GetGridPos(vertPos, cellSize);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int neighborKey = new Vector3Int(key.x + x, key.y + y, key.z + z);
                    if (grid.TryGetValue(neighborKey, out var value))
                    {
                        foreach (var p in value)
                        {
                            if (Vector3.Distance(vertPos, p) < radius)
                                return true;
                        }
                    }
                }
            }
        }
        return false;
    }
}