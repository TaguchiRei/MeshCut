using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TriangleGetSideJob : IJob
{
    //元メッシュデータを入力
    public NativeArray<int3> TrianglesStartLengthId;
    public NativeArray<SubmeshTriangle> Triangles;

    //ほかのJobで取得した値を代入
    public NativeArray<int> VerticesSide;

    /// <summary> 各三角形がどの配列に入るかを保存している。 </summary>
    public NativeArray<int> TrianglesArrayNumber;

    /// <summary> 各配列の長さと開始地点を保存している </summary>
    public NativeArray<int2> LengthAndStart;

    //長さはTrianglesと同じでOK
    public NativeArray<int3> ResultTrianglesStartLengthId;

    public void Execute()
    {
        NativeArray<int> length = new NativeArray<int>(8, Allocator.Temp);

        for (int i = 0; i < Triangles.Length; i++)
        {
            //三角形の各頂点のインデックスを取得するのに必要
            SubmeshTriangle triangle = Triangles[i];

            var baseIndex = LengthAndStart[TrianglesArrayNumber[i]];
            var index = baseIndex.x + length[baseIndex.y];

            ResultTrianglesStartLengthId[index] = 
            
            length[baseIndex.y]++;
        }
    }
}