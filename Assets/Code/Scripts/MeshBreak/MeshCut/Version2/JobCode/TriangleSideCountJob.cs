using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TriangleSideCountJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<SubmeshTriangle> Triangles;
    [ReadOnly] public NativeArray<int> VertexSide;


    /// <summary> 三角形毎にどの配列に入るかを示す </summary>
    [WriteOnly] public NativeArray<int> TrianglesArrayNumber;

    public void Execute(int index)
    {
        var triangle = Triangles[index];
        int countIndex = (VertexSide[triangle.Index0] << 2) |
                         (VertexSide[triangle.Index1] << 1) |
                         (VertexSide[triangle.Index2] << 0);
        /*
         * 0,0,0 → 0 切断面の裏側配列に入れる
         * 0,0,1 → 1 孤立がv2表面配列に入れる
         * 0,1,0 → 2 孤立がv1表面配列に入れる
         * 0,1,1 → 3 孤立がv0裏面配列に入れる
         * 1,0,0 → 4 孤立がv0表面配列に入れる
         * 1,0,1 → 5 孤立がv1裏面配列に入れる
         * 1,1,0 → 6 孤立がv2裏面配列に入れる
         * 1,1,1 → 7 切断面の表側配列に入れる
         */
        TrianglesArrayNumber[index] = countIndex;
    }
}