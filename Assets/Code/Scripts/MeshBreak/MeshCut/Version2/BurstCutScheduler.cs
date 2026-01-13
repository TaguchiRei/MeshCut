using Unity.Collections;
using UnityEngine;

public class BurstCutScheduler
{
    public MeshCutContext SchedulingCut(Mesh[] mesh, NativeTransform[] transforms)
    {
        int verticesCount = 0;
        int objectCount = mesh.Length;
        int triangleCount = 0;
        for (int i = 0; i < mesh.Length; i++)
        {
            verticesCount += mesh[i].vertexCount;
            objectCount += mesh[i].vertexCount;
        }

        MeshCutContext context = new
        (
            verticesCount,
            objectCount,
            triangleCount,
            Allocator.TempJob
        );
        
        
        
        

        return context;
    }
}