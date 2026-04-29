using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshCutResult
    {
        public readonly BreakMeshData LeftMeshData;
        public readonly BreakMeshData RightMeshData;
        public readonly System.Collections.Generic.List<Vector3> Centers;

        public MeshCutResult(
            BreakMeshData left,
            BreakMeshData right,
            System.Collections.Generic.List<Vector3> centers)
        {
            LeftMeshData = left;
            RightMeshData = right;
            Centers = centers;
        }
    }
}