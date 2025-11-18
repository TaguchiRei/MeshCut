using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace ChaosDestruction.PoissonDiskSampling
{
    /// <summary>
    /// 速度は遅いが、重ならない点をランダムに生成できる
    /// </summary>
    public class PoissonDiskSampling
    {
        private readonly Random _random = new();
        private readonly int _tryCheck;
        private readonly float _density;
        private readonly float _maxRadiusMagnitude;
        private float _minRadius;
        private float _maxRadius;

        private Vector3?[,,] _grid;

        /// <summary>
        /// サンプリングのチェック回数、密度、最大半径の倍率
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

        public UniTask<List<Vector3>> SamplingAsync(Vector3 maxPosition, Vector3 minPosition,
            CancellationToken cancellationToken)
        {
            float xLength = Mathf.Abs(maxPosition.x - minPosition.x);
            float yLength = Mathf.Abs(maxPosition.y - minPosition.y);
            float zLength = Mathf.Abs(maxPosition.z - minPosition.z);

            // 1個あたりの体積
            float singleVolume = 1f / _density;
            // 半径を計算
            _minRadius = Mathf.Pow((3f * singleVolume) / (4f * Mathf.PI), 1f / 3f);
            _maxRadius = _minRadius * _maxRadiusMagnitude;

            //グリッドのセルサイズ
            float cellSize = _minRadius / Mathf.Sqrt(3);

            //セルを定義
            int xCount = Mathf.CeilToInt(xLength / cellSize);
            int yCount = Mathf.CeilToInt(yLength / cellSize);
            int zCount = Mathf.CeilToInt(zLength / cellSize);
            _grid = new Vector3?[xCount, yCount, zCount];

            return SamplingVector3Async(maxPosition, minPosition, cellSize, cancellationToken);
        }

        private UniTask<List<Vector3>> SamplingVector3Async(Vector3 maxPosition, Vector3 minPosition, float cellSize,
            CancellationToken cancellation)
        {
            return UniTask.Run(() =>
            {
                Random random = new Random();
                if (_minRadius < 0 || _maxRadius < 0)
                    throw new Exception("minRadius and maxRadius are required");

                List<Vector3> generatedVerts = new();
                List<Vector3> activeVerts = new();

                Vector3 firstVert = new Vector3(
                    RandomFloat(random, minPosition.x, maxPosition.x),
                    RandomFloat(random, minPosition.y, maxPosition.y),
                    RandomFloat(random, minPosition.z, maxPosition.z));

                generatedVerts.Add(firstVert);
                activeVerts.Add(firstVert);

                var gridIndex = GetGridIndex(firstVert, minPosition, cellSize);
                _grid[gridIndex.x, gridIndex.y, gridIndex.z] = firstVert;

                while (activeVerts.Count > 0)
                {
                    if (cancellation.IsCancellationRequested)
                        return null;

                    int index = random.Next(0, activeVerts.Count);
                    Vector3 currentPos = activeVerts[index];
                    bool found = false;

                    for (int i = 0; i < _tryCheck; i++)
                    {
                        Vector3 newVert = GenerateRandomVert(random, currentPos, _minRadius, _maxRadius);

                        if (IsInBounds(newVert, minPosition, maxPosition) &&
                            !IsTooClose(newVert, cellSize, _minRadius))
                        {
                            generatedVerts.Add(newVert);
                            activeVerts.Add(newVert);
                            Vector3Int key = GetGridPos(newVert, cellSize);
                            _grid[key.x, key.y, key.z] = newVert;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        activeVerts.RemoveAt(index);
                }

                return generatedVerts;
            }, cancellationToken: cancellation);
        }

        private float RandomFloat(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
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
        /// グリッドのインデックスを取得する
        /// </summary>
        /// <param name="position"></param>
        /// <param name="minPosition"></param>
        /// <param name="cellSize"></param>
        private Vector3Int GetGridIndex(Vector3 position, Vector3 minPosition, float cellSize)
        {
            return new Vector3Int(
                Mathf.FloorToInt((position.x - minPosition.x) / cellSize),
                Mathf.FloorToInt((position.y - minPosition.y) / cellSize),
                Mathf.FloorToInt((position.z - minPosition.z) / cellSize)
            );
        }

        /// <summary>
        /// 指定座標を中心とした新たな座標を作成
        /// </summary>
        /// <param name="center">中心</param>
        /// <param name="minRadius">最小半径</param>
        /// <param name="maxRadius">最大半径</param>
        /// <returns></returns>
        private Vector3 GenerateRandomVert(Random random, Vector3 center, float minRadius, float maxRadius)
        {
            return center + RandomDirection() * RandomFloat(random, minRadius, maxRadius);
        }

        /// <summary>
        /// ランダムな方向を生成する
        /// </summary>
        /// <returns></returns>
        private Vector3 RandomDirection()
        {
            // System.Random.NextDouble() は [0.0, 1.0)
            float theta = (float)(_random.NextDouble() * 2.0 * Math.PI);
            float phi = (float)Math.Acos(2.0 * _random.NextDouble() - 1.0);

            float x = Mathf.Sin(phi) * Mathf.Cos(theta);
            float y = Mathf.Sin(phi) * Mathf.Sin(theta);
            float z = Mathf.Cos(phi);

            return new Vector3(x, y, z);
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
        /// <param name="cellSize"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private bool IsTooClose(Vector3 vertPos, float cellSize, float radius)
        {
            Vector3Int key = GetGridPos(vertPos, cellSize);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighborKey = new Vector3Int(key.x + x, key.y + y, key.z + z);

                        if (neighborKey.x < 0 || neighborKey.y < 0 || neighborKey.z < 0 ||
                            neighborKey.x >= _grid.GetLength(0) ||
                            neighborKey.y >= _grid.GetLength(1) ||
                            neighborKey.z >= _grid.GetLength(2))
                        {
                            continue;
                        }

                        var neighborValue = _grid[neighborKey.x, neighborKey.y, neighborKey.z];

                        if (neighborValue.HasValue)
                        {
                            Vector3 diff = vertPos - neighborValue.Value;
                            if (diff.sqrMagnitude < radius * radius)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}