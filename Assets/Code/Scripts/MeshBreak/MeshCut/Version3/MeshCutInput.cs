using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public readonly struct MeshCutInput
    {
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector2[] UVs;
        public readonly int[][] SubMeshTriangles;
        public readonly Plane Blade;
        public readonly Matrix4x4 LocalToWorld;

        public MeshCutInput(
            Vector3[] vertices,
            Vector3[] normals,
            Vector2[] uvs,
            int[][] subMeshTriangles,
            Plane blade,
            Matrix4x4 localToWorld)
        {
            Vertices         = vertices;
            Normals          = normals;
            UVs              = uvs;
            SubMeshTriangles = subMeshTriangles;
            Blade            = blade;
            LocalToWorld     = localToWorld;
        }
    }
}