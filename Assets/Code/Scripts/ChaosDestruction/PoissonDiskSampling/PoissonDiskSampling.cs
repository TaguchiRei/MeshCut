using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class PoissonDiskSampling
{
    #region Variables

    private readonly int _tryCheck;
    private readonly float _density;
    private readonly float _maxRadiusMagnitude;
    private float _minRadius;
    private float _maxRadius;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="tryCheck">チェック回数</param>
    /// <param name="density">密度</param>
    /// <param name="maxRadiusMagnification">密度の上限</param>
    public PoissonDiskSampling(int tryCheck, float density, float maxRadiusMagnification = 1)
    {
        _tryCheck = tryCheck;
        _density = density;
        _maxRadiusMagnitude = maxRadiusMagnification;
    }

    /// <summary>
    /// 点分布生成を行う
    /// </summary>
    /// <param name="maxPosition"></param>
    /// <param name="minPosition"></param>
    /// <returns></returns>
    public List<Vector3> Sampling(Vector3 maxPosition, Vector3 minPosition)
    {
        float xLength = Mathf.Abs(maxPosition.x - minPosition.x);
        float yLength = Mathf.Abs(maxPosition.y - minPosition.y);
        float zLength = Mathf.Abs(maxPosition.z - minPosition.z);
        float volume = xLength * yLength * zLength; //面積

        // 1個あたりの体積
        float singleVolume = 1f / _density;

        // 半径を計算
        _minRadius = Mathf.Pow((3f * singleVolume) / (4f * Mathf.PI), 1f / 3f);
        _maxRadius = _minRadius * _maxRadiusMagnitude;
        
        return SamplingVector3(maxPosition, minPosition);
    }

    private List<Vector3> SamplingVector3( Vector3 maxPosition, Vector3 minPosition)
    {
        if (_minRadius < 0 || _maxRadius < 0)
        {
            throw new Exception("minRadius and maxRadius are required");
        }

        List<Vector3> generatedVerts = new(); //生成された頂点
        Dictionary<Vector3Int, List<Vector3>> checkGrid = new(); //グリッドごとに設置の可否を保存する。
        List<Vector3> activeVerts = new(); //未探査の頂点

        float cellSize = _minRadius / Mathf.Sqrt(3); //グリッドのセルサイズ

        Vector3 firstVert = new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z));

        generatedVerts.Add(firstVert);
        activeVerts.Add(firstVert);
        checkGrid[GetGridPos(firstVert, cellSize)] = new List<Vector3> { firstVert };

        while (activeVerts.Count > 0)
        {
            int index = Random.Range(0, activeVerts.Count);
            Vector3 currentPos = activeVerts[index];
            bool found = false;
            for (int i = 0; i < _tryCheck; i++)
            {
                Vector3 newVert = GenerateRandomVert(currentPos, _minRadius, _maxRadius);

                if (IsInBounds(newVert, minPosition, maxPosition) &&
                    !IsTooClose(newVert, checkGrid, cellSize, _minRadius))
                {
                    generatedVerts.Add(newVert);
                    activeVerts.Add(newVert);
                    Vector3Int key = GetGridPos(newVert, cellSize);
                    if (!checkGrid.ContainsKey(key))
                        checkGrid[key] = new List<Vector3>();
                    checkGrid[key].Add(newVert);
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

    /// <summary>
    /// 範囲内にあるかを調べる
    /// </summary>
    /// <param name="vert"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
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

    #endregion
}