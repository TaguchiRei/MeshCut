using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TrianglesCutJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> BaseVertices;
    [ReadOnly] public NativeArray<float3> BaseNormals;
    [ReadOnly] public NativeArray<float2> BaseUvs;
    [ReadOnly] public NativeArray<SubmeshTriangle> BaseTriangles;
    [ReadOnly] public NativeArray<NativePlane> Blades;

    /// <summary> 各三角形のインデックスに対するオフセット </summary>
    [ReadOnly] public NativeArray<int> TrianglesObjectStartIndex;

    /// <summary> 各頂点がどのオブジェクトに対応しているのかを保持する配列 </summary>
    [ReadOnly] public NativeArray<int> VertexObjectIndex;

    /// <summary> 三角形の状態（0-7） </summary>
    [ReadOnly] public NativeArray<int> TrianglesArrayNumber;

    /// <summary> 元の三角形一つにつき二つ分の領域を用意 </summary>
    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float3> NewVertices;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float3> NewNormals;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float2> NewUvs;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<int> ActiveResultTriangleIndex;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<int> ActiveResultVertexIndex;

    /// <summary> 元の三角形1つにつき3つ分の領域を確保 </summary>
    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<NewTriangleData> NewTriangles;

    public void Execute(int index)
    {
        int countIndex = TrianglesArrayNumber[index];
        if (countIndex == 0 || countIndex == 7) return;

        int indexStart = TrianglesObjectStartIndex[index];
        var triangle = BaseTriangles[index];

        // 孤立頂点のローカルインデックスを取得
        int localIsoIdx = countIndex switch
        {
            1 => 2, 2 => 1, 3 => 0,
            4 => 0, 5 => 1, 6 => 2,
            _ => -1
        };

        // 頂点インデックスの特定
        int soloIndex = GetIndexByLocal(triangle, localIsoIdx);
        int double1Index = GetIndexByLocal(triangle, (localIsoIdx + 1) % 3);
        int double2Index = GetIndexByLocal(triangle, (localIsoIdx + 2) % 3);

        // データの取得
        float3 soloV = BaseVertices[soloIndex + indexStart];
        float3 d1V = BaseVertices[double1Index + indexStart];
        float3 d2V = BaseVertices[double2Index + indexStart];

        float3 soloN = BaseNormals[soloIndex + indexStart];
        float3 d1N = BaseNormals[double1Index + indexStart];
        float3 d2N = BaseNormals[double2Index + indexStart];

        float2 soloU = BaseUvs[soloIndex + indexStart];
        float2 d1U = BaseUvs[double1Index + indexStart];
        float2 d2U = BaseUvs[double2Index + indexStart];

        int objectId = VertexObjectIndex[soloIndex + indexStart];
        NativePlane blade = Blades[objectId];

        // 交差計算 (t = 割合)
        float dot1 = math.dot(blade.Normal, d1V - soloV);
        float t1 = (-math.dot(blade.Normal, soloV) - blade.Distance) / dot1;

        float dot2 = math.dot(blade.Normal, d2V - soloV);
        float t2 = (-math.dot(blade.Normal, soloV) - blade.Distance) / dot2;

        // 新規頂点生成
        float3 nV1 = soloV + (d1V - soloV) * t1;
        float3 nN1 = math.normalize(soloN + (d1N - soloN) * t1);
        float2 nU1 = soloU + (d1U - soloU) * t1;

        float3 nV2 = soloV + (d2V - soloV) * t2;
        float3 nN2 = math.normalize(soloN + (d2N - soloN) * t2);
        float2 nU2 = soloU + (d2U - soloU) * t2;

        // 新規頂点の書き込み
        int vBase = index * 2;
        ActiveResultVertexIndex[vBase] = 1;
        ActiveResultTriangleIndex[vBase + 1] = 1;
        NewVertices[vBase] = nV1;
        NewVertices[vBase + 1] = nV2;
        NewNormals[vBase] = nN1;
        NewNormals[vBase + 1] = nN2;
        NewUvs[vBase] = nU1;
        NewUvs[vBase + 1] = nU2;

        // VertexRefの作成
        VertexRef vrSolo = VertexRef.Existing(soloIndex);
        VertexRef vrD1 = VertexRef.Existing(double1Index);
        VertexRef vrD2 = VertexRef.Existing(double2Index);
        VertexRef vrN1 = VertexRef.New(vBase);
        VertexRef vrN2 = VertexRef.New(vBase + 1);

        // 三角形データの生成 (3枚分)
        int triBase = index * 3;
        int submesh = triangle.SubmeshIndex;
        ActiveResultTriangleIndex[triBase] = 1;
        ActiveResultTriangleIndex[triBase + 1] = 1;
        ActiveResultTriangleIndex[triBase + 2] = 1;

        // 孤立頂点が「表(1)」なのか「裏(0)」なのかでサイドを判定
        // countIndex 1, 2, 4 は孤立頂点側が「表」
        int isFrontSide = (countIndex == 1 || countIndex == 2 || countIndex == 4) ? 1 : 0;
        int isFrontSideDouble = 1 - isFrontSide;

        // 1. 孤立頂点側の三角形 (solo, n1, n2)
        NewTriangles[triBase] = new NewTriangleData(vrSolo, vrN1, vrN2, submesh, objectId, isFrontSide);

        // 2. 残り側三角形1 (n1, d1, d2)
        NewTriangles[triBase + 1] = new NewTriangleData(vrN1, vrD1, vrD2, submesh, objectId, isFrontSideDouble);

        // 3. 残り側三角形2 (n1, d2, n2)
        NewTriangles[triBase + 2] = new NewTriangleData(vrN1, vrD2, vrN2, submesh, objectId, isFrontSideDouble);
    }

    private int GetIndexByLocal(SubmeshTriangle tri, int localIdx)
    {
        return localIdx == 0 ? tri.Index0 : (localIdx == 1 ? tri.Index1 : tri.Index2);
    }
}