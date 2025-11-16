using System;
using System.Collections.Generic;
using ChaosDestruction.PoissonDiskSampling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MeshBreak.ChaosDestruction
{
    /// <summary>
    /// メッシュ破壊のためのデータを保持
    /// </summary>
    public class DestructAreaData
    {
        private PoissonDiskSampling _poissonDiskSampling;

        private readonly float _checkCellSize;

        private readonly Vector3Int[] _checkCellPositions;

        private class VoronoiVertexData
        {
            /// <summary> 頂点候補の座標群 </summary>
            public readonly List<Vector3> Positions = new();

            /// <summary> ボロノイの中心のインデックス </summary>
            public readonly List<int> ConfigureCenters = new();

            /// <summary> グリッドの端に位置しているか </summary>
            public bool IsEdge = false;
        }


        /// <param name="checkCellSize">頂点取得のための</param>
        public DestructAreaData(float checkCellSize, Vector3Int[] checkCellPositions)
        {
            _checkCellSize = checkCellSize;
            if (checkCellPositions != null)
            {
                _checkCellPositions = checkCellPositions;
            }
            else
            {
                _checkCellPositions = new Vector3Int[27];
                int index = 0;
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            _checkCellPositions[index++] = new Vector3Int(i, j, k);
                        }
                    }
                }
            }
        }

        private Mesh[] GetAllDestructMeshes(List<Vector3> centers, Vector3 maxPosition, Vector3 minPosition)
        {
            Mesh[] resultMeshes = new Mesh[centers.Count];

            int gridX = (int)Math.Ceiling((maxPosition.x - minPosition.x) / _checkCellSize);
            int gridY = (int)Math.Ceiling((maxPosition.y - minPosition.y) / _checkCellSize);
            int gridZ = (int)Math.Ceiling((maxPosition.z - minPosition.z) / _checkCellSize);

            #region まずグリッド毎のボロノイ図を調べる

            int[,,] voronoiGrid = new int[gridX, gridY, gridZ];

            //MEMO : マルチスレッド化した方が良いかも
            //追記　：バーストを使ってみてもいいかもしれない
            for (int x = 0; x < gridX; x++)
            {
                for (int y = 0; y < gridY; y++)
                {
                    for (int z = 0; z < gridZ; z++)
                    {
                        float minDistance = float.MaxValue;
                        for (int i = 0; i < centers.Count; i++)
                        {
                            Vector3 disVector = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * _checkCellSize;
                            float distance = (disVector - centers[i]).sqrMagnitude;

                            if (distance <= minDistance)
                            {
                                voronoiGrid[x, y, z] = i;
                                minDistance = distance;
                            }
                        }
                    }
                }
            }

            #endregion

            #region 自身の周囲のマスを調べ、３つ以上のエリアが含まれれば頂点としてマーク

            //MEMO : マルチスレッド化した方が良いかも
            VoronoiVertexData[,,] voronoiVertex = new VoronoiVertexData [gridX, gridY, gridZ];


            for (int x = 0; x < gridX; x++)
            {
                for (int y = 0; y < gridY; y++)
                {
                    for (int z = 0; z < gridZ; z++)
                    {
                        var voronoiV = voronoiVertex[x, y, z] = new VoronoiVertexData();

                        if (x == 0 || y == 0 || z == 0 ||
                            x == gridX - 1 || y == gridY - 1 || z == gridZ - 1)
                        {
                            voronoiV.IsEdge = true;
                        }

                        //周囲のマスを調べて未追加の隣接エリアがあった場合リストに追加する
                        foreach (var checkCellPosition in _checkCellPositions)
                        {
                            var checkPos = checkCellPosition + new Vector3Int(x, y, z);

                            //範囲外チェック
                            if (checkPos.x < 0 || checkPos.y < 0 || checkPos.z < 0 ||
                                checkPos.x >= gridX || checkPos.y >= gridY || checkPos.z >= gridZ)
                            {
                                continue;
                            }

                            //未追加かどうか調べる
                            if (!voronoiV.ConfigureCenters.Contains(voronoiGrid[checkPos.x, checkPos.y, checkPos.z]))
                            {
                                voronoiV.ConfigureCenters.Add(voronoiGrid[checkPos.x, checkPos.y, checkPos.z]);
                                if (!voronoiV.Positions.Contains(new(x, y, z)))
                                {
                                    voronoiV.Positions.Add(new(x, y, z));
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            return resultMeshes;
        }
    }
}