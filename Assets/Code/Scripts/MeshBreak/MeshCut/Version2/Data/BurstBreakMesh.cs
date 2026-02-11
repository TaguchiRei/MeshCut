using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

public class BurstBreakMesh
{
    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;
    
    public List<List<int>> Triangles;

    public void AddTriangle(int p1, int p2, int p3, int submesh)
    {
        
    }
}
