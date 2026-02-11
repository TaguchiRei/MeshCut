using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TriangleCutJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int3> CutFaces;
    [ReadOnly] public NativeArray<int> CutStatus; 
    [ReadOnly] public NativeArray<int> SubmeshIndices;
    [ReadOnly] public NativeArray<NativePlane> Blades;
    [ReadOnly] public NativeArray<int> ObjectIndices;

    [ReadOnly] public NativeArray<float3> BaseVertices;
    [ReadOnly] public NativeArray<float3> BaseNormals;
    [ReadOnly] public NativeArray<float2> BaseUvs;
    
    [WriteOnly] public NativeArray<float3> OutNewVertices;
    [WriteOnly] public NativeArray<float3> OutNewNormals;
    [WriteOnly] public NativeArray<float2> OutNewUvs;
    
    [WriteOnly] public NativeArray<NewTriangle> OutNewTriangles;
    
    [WriteOnly] public NativeArray<CutEdge> OutCapEdges;

    public void Execute(int index)
    {
        int3 face = CutFaces[index];
        int status = CutStatus[index];
        NativePlane blade = Blades[ObjectIndices[index]];
        int submesh = SubmeshIndices[index];
        
        int3 order = GetFaceOrder(status);
        bool isAFront = GetIsFront(status);

        int indexA = face[order.x];
        int indexB = face[order.y];
        int indexC = face[order.z];
        
        
        float tAB = Intersect(BaseVertices[indexA], BaseVertices[indexB], blade);
        float tAC = Intersect(BaseVertices[indexA], BaseVertices[indexC], blade);
        
        int vIdxStart = index * 2;
        OutNewVertices[vIdxStart + 0] = math.lerp(BaseVertices[indexA], BaseVertices[indexB], tAB);
        OutNewVertices[vIdxStart + 1] = math.lerp(BaseVertices[indexA], BaseVertices[indexC], tAC);
        
        OutNewNormals[vIdxStart + 0] = math.lerp(BaseNormals[indexA], BaseNormals[indexB], tAB);
        OutNewNormals[vIdxStart + 1] = math.lerp(BaseNormals[indexA], BaseNormals[indexC], tAC);
        
        OutNewUvs[vIdxStart + 0] = math.lerp(BaseUvs[indexA], BaseUvs[indexB], tAB);
        OutNewUvs[vIdxStart + 1] = math.lerp(BaseUvs[indexA], BaseUvs[indexC], tAC);
        
        
        int oldA = -(indexA + 1);
        int oldB = -(indexB + 1);
        int oldC = -(indexC + 1);
        int newV1 = vIdxStart;
        int newV2 = vIdxStart + 1;

        int triIdxStart = index * 3;
        int sideA = isAFront ? 1 : 0;
        int sideBC = isAFront ? 0 : 1;
        
        
        OutNewTriangles[triIdxStart + 0] = new NewTriangle 
        { 
            Vertex1 = oldA, Vertex2 = newV1, Vertex3 = newV2, 
            Submesh = submesh, Side = sideA 
        };
        
        
        OutNewTriangles[triIdxStart + 1] = new NewTriangle 
        { 
            Vertex1 = newV1, Vertex2 = oldB, Vertex3 = oldC, 
            Submesh = submesh, Side = sideBC 
        };
        OutNewTriangles[triIdxStart + 2] = new NewTriangle 
        { 
            Vertex1 = newV1, Vertex2 = oldC, Vertex3 = newV2, 
            Submesh = submesh, Side = sideBC 
        };
        
        OutCapEdges[index] = new CutEdge 
        { 
            Vertex1 = isAFront ? newV1 : newV2, 
            Vertex2 = isAFront ? newV2 : newV1 
        };
    }

    private static float Intersect(float3 p0, float3 p1, NativePlane plane)
    {
        float3 edge = p1 - p0;
        return (-math.dot(plane.Normal, p0) - plane.Distance) / math.dot(plane.Normal, edge);
    }

    private static int3 GetFaceOrder(int status)
    {
        return status switch
        {
            1 or 6 => new int3(0, 1, 2), // p1(x) 孤立
            2 or 5 => new int3(1, 2, 0), // p2(y) 孤立
            4 or 3 => new int3(2, 0, 1), // p3(z) 孤立
            _ => new int3(0, 0, 0)
        };
    }

    private static bool GetIsFront(int status)
    {
        return status == 1 || status == 2 || status == 4;
    }
}