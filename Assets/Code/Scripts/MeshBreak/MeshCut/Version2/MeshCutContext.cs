using System;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// メッシュ切断の過程で生まれる一時配列をすべて保持するクラス。
/// 処理完了後に一括でDisposeできるようにする
/// </summary>
public class MeshCutContext : INativeDisposable
{
    public JobHandle CutJobHandle;

    /// <summary> ベースの各頂点とオブジェクトの対応配列。長さはベースオブジェクトの頂点群の長さ </summary>
    public NativeArray<int> VerticesObjectIdList;

    /// <summary> オブジェクト順に並んだ頂点群 </summary>
    public NativeMultiArrayView<float3> BaseVertices;

    /// <summary> 同一インデックス番号の頂点の法線 </summary>
    public NativeMultiArrayView<float3> BaseNormals;

    /// <summary> 同一インデックス番号の頂点のUV座標 </summary>
    public NativeMultiArrayView<float2> BaseUvs;

    /// <summary> 全三角形を保持 </summary>
    public NativeMultiArrayView<NativeTriangle> BaseTriangles;

    /// <summary> 各オブジェクトごとの切断面をオブジェクト順に保持 </summary>
    public NativeArray<NativePlane> Blades;

    /// <summary> 各オブジェクトのTransform情報をオブジェクト順に保持 </summary>
    public NativeArray<NativeTransform> Transforms;

    /// <summary> 各頂点が面に対してどの方向にあるのかを保持する </summary>
    public NativeArray<int> VertSide;

    private NativeArray<float3>[] baseVerticesArray;
    private NativeArray<float3>[] baseNormalsArray;
    private NativeArray<float2>[] baseUvsArray;
    private NativeArray<NativeTriangle>[] baseTrianglesArray;

    public async UniTask Complete()
    {
        if (CutJobHandle.IsCompleted) return;
        await UniTask.WaitUntil(() => CutJobHandle.IsCompleted);
    }

    public MeshCutContext(
        NativeArray<float3>[] baseVertices,
        NativeArray<float3>[] baseNormals,
        NativeArray<float2>[] baseUvs,
        NativeArray<NativeTriangle>[] baseTriangles,
        NativeArray<NativePlane> blades,
        NativeArray<int> vertSide)
    {
    }
}