using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct InitializeArrayJobL3 : IJob
{
    [ReadOnly] public NativeArray<int2> ArrayPath;
    [ReadOnly] public NativeArray<int3> BaseTriangleArrayData;

    [WriteOnly] public NativeArray<int> VertexObjectIndex;
    [WriteOnly] public NativeArray<int> TrianglesObjectStartIndex;

    public void Execute()
    {
        int index = 0;
        for (int i = 0; i < ArrayPath.Length; i++)
        {
            for (int j = 0; j < ArrayPath[i].x; j++)
            {
                VertexObjectIndex[index++] = i;
            }
        }

        int tIndex = 0;
        for (int i = 0; i < BaseTriangleArrayData.Length; i++)
        {
            int triangleCount = BaseTriangleArrayData[i].y;
            int vertexOffset  = ArrayPath[i].y;

            for (int j = 0; j < triangleCount; j++)
            {
                TrianglesObjectStartIndex[tIndex++] = vertexOffset;
            }
        }
    }
}