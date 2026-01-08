using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FillVertexObjectIdJob : IJobParallelFor
{
    // x:開始インデックス, y:長さ, z:オブジェクトID
    [ReadOnly] public NativeArray<int3> VertexGroups;

    // 結果を書き込む配列（長さは全頂点数）
    [NativeDisableParallelForRestriction] // 異なるインデックス範囲に書き込むため制限を解除
    public NativeArray<int> VertexToObjectId;

    public void Execute(int index)
    {
        int3 group = VertexGroups[index];
        int start = group.x;
        int length = group.y;
        int objectId = group.z;

        // 指定された範囲をObjectIdで埋める
        for (int i = 0; i < length; i++)
        {
            VertexToObjectId[start + i] = objectId;
        }
    }
}