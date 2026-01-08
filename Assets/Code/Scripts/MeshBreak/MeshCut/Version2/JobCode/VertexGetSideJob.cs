using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VertexGetSideJob : IJobParallelFor
{
    public NativeArray<float3> Vertices;
    public NativePlane Blades;

    public NativeArray<int> VerticesSide;

    public void Execute(int index)
    {
        VerticesSide[index] = math.dot(Vertices[index] - Blades.Position, Blades.Normal) > 0f ? 1 : 0;
    }

    /*
     大量面を同時に処理するなら以下のコードに書き換えてbitをそのまま面IDとして利用する
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VertexGetSideJob : IJobParallelFor
{
    public NativeArray<float3> Vertices;
    public NativeArray<NativePlane> Blades;

    public NativeArray<int> VerticesSide;

    public void Execute(int index)
    {
        var bitData = 0;
        for (int i = 0; i < Blades.Length; i++)
        {
            if (math.dot(Vertices[index] - Blades[i].Position, Blades[i].Normal) > 0f)
            {
                bitData |= 1 << i;
            }
            bitData <<= 1;
            bitData +=  > 0.0f ? 1 : 0;
        }

        VerticesSide[index] = bitData;
    }
}*/
}