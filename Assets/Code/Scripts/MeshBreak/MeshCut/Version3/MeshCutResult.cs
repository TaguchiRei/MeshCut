using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshCutResult
    {
        public readonly CutMeshData LeftMeshData;
        public readonly CutMeshData RightMeshData;
        public readonly List<Vector3> Centers;

        public MeshCutResult(
            CutMeshData left,
            CutMeshData right,
            List<Vector3> centers)
        {
            LeftMeshData = left;
            RightMeshData = right;
            Centers = centers;
        }
    }
}