using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct MoldingTriangleData : IJob
{
    [ReadOnly]public NativeArray<NewTriangleDataL3> NewTriangles;
    [ReadOnly]public NativeArray<SubmeshTriangleL3> SubmeshTriangles;
    [ReadOnly]public NativeArray<int> TrianglesGroupNumber;
    /// <summary> もともと存在した全三角形の頂点インデックスに対するオフセット </summary>
    [ReadOnly] public NativeArray<int> TrianglesObjectStartIndex;
    
    public void Execute()
    {
        
    }
}