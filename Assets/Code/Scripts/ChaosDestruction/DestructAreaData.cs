using System.Collections.Generic;
using ChaosDestruction.PoissonDiskSampling;
using UnityEngine;

namespace MeshBreak.ChaosDestruction
{
    /// <summary>
    /// メッシュ破壊のためのデータを保持
    /// </summary>
    public class DestructAreaData
    {
        private PoissonDiskSampling _poissonDiskSampling;

        private float _checkDensity;


        /// <param name="checkDensity">頂点取得のための</param>
        public DestructAreaData(float checkDensity)
        {
        }

        private Mesh[] GetAllDestructMeshes(List<Vector3> centers, Vector3 maxPosition, Vector3 minPosition)
        {
            Mesh[] resultMeshes = new Mesh[centers.Count];

            float checkSize = (maxPosition - minPosition).magnitude / (1 / _checkDensity);

            for (float x = minPosition.x; x < maxPosition.x; x += _checkDensity)
            {
            }
            
            return resultMeshes;
        }
    }
}