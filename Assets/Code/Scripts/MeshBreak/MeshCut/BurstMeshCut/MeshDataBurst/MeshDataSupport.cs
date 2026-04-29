using MeshBreak;
using UnityEngine;

public static class MeshDataSupport
{
    public static Mesh ToMesh(BreakMeshData breakMeshData, string meshName = "mesh")
    {
        Mesh mesh = new()
        {
            name = meshName
        };

        if (breakMeshData.Vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(breakMeshData.Vertices);
        mesh.SetNormals(breakMeshData.Normals);
        mesh.SetUVs(0, breakMeshData.Uvs);
        mesh.subMeshCount = breakMeshData.SubIndices.Count;
        for (int i = 0; i < breakMeshData.SubIndices.Count; i++)
        {
            mesh.SetIndices(breakMeshData.SubIndices[i], MeshTopology.Triangles, i);
        }

        return mesh;
    }
}