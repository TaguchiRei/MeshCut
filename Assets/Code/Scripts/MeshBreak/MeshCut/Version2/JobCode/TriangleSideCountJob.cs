using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TriangleSideCountJob : IJob
{
    [ReadOnly] public NativeArray<SubmeshTriangle> Triangles;
    [ReadOnly] public NativeArray<int> VertexSide;

    public NativeArray<int2> LengthAndStart;
    [WriteOnly] public NativeArray<int> TrianglesArrayNumber;

    public void Execute()
    {
        for (int i = 0; i < Triangles.Length; i++)
        {
            var triangle = Triangles[i];
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
            //各配列の長さを保存
            LengthAndStart[countIndex] = new int2(LengthAndStart[countIndex].x + 1, 0);
            TrianglesArrayNumber[i] = countIndex;
        }
        
        //各配列の開始位置を設定
        int start = 0;
        for (int i = 0; i < 8; i++)
        {
            LengthAndStart[i] = new int2(LengthAndStart[i].x, start);
            start += LengthAndStart[i].x;
        }
    }
}