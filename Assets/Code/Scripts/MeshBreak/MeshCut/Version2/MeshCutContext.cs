using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

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

    public NativeArray<float3>[] BaseVerticesArray;
    public NativeArray<float3>[] BaseNormalsArray;
    public NativeArray<float2>[] BaseUvsArray;

    public async UniTask Complete()
    {
        if (CutJobHandle.IsCompleted) return;
        await UniTask.WaitUntil(() => CutJobHandle.IsCompleted);
    }

    public MeshCutContext(int objectCount)
    {
        BaseVerticesArray = new NativeArray<float3>[objectCount];
        BaseNormalsArray = new NativeArray<float3>[objectCount];
        BaseUvsArray = new NativeArray<float2>[objectCount];
    }

    /// <summary>
    /// baseVertices等を設定したのちに使用
    /// </summary>
    /// <param name="baseTriangles"></param>
    /// <param name="transforms"></param>
    /// <param name="allocator"></param>
    public void InitializeContext(
        NativeArray<NativeTriangle>[] baseTriangles, NativeTransform[] transforms,
        Allocator allocator)
    {
        BaseVertices = new(BaseVerticesArray, allocator);
        BaseNormals = new(BaseNormalsArray, allocator);
        BaseUvs = new(BaseUvsArray, allocator);
        BaseTriangles = new(baseTriangles, allocator);

        Transforms = new(transforms, allocator);
        Blades = new NativeArray<NativePlane>(BaseVerticesArray.Length, allocator,
            NativeArrayOptions.UninitializedMemory);
        VerticesObjectIdList =
            new NativeArray<int>(BaseVertices.Length, allocator, NativeArrayOptions.UninitializedMemory);
        int globalIdx = 0;
        for (int i = 0; i < BaseVerticesArray.Length; i++)
        {
            for (int j = 0; j < BaseVerticesArray[i].Length; j++)
            {
                VerticesObjectIdList[globalIdx++] = i;
            }
        }

        VertSide = new(BaseVertices.Length, allocator);
    }

    public void Dispose()
    {
        VerticesObjectIdList.Dispose();
        BaseVertices.Dispose();
        BaseNormals.Dispose();
        BaseUvs.Dispose();
        BaseTriangles.Dispose();
        Blades.Dispose();
        Transforms.Dispose();
        VertSide.Dispose();
        for (int i = 0; i < BaseVerticesArray.Length; i++)
        {
            BaseVerticesArray[i].Dispose();
            BaseNormalsArray[i].Dispose();
            BaseUvsArray[i].Dispose();
        }
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return default;
    }
}