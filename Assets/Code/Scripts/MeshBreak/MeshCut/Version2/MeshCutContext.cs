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
        NativeTransform[] transforms,
        Allocator allocator)
    {
        //結合配列初期化
        BaseVertices = new(baseVertices, allocator);
        BaseNormals = new(baseNormals, allocator);
        BaseUvs = new(baseUvs, allocator);
        BaseTriangles = new(baseTriangles, allocator);
        
        //処理用配列初期化
        Transforms = new(transforms, allocator);
        Blades = new NativeArray<NativePlane>(baseVertices.Length, allocator, NativeArrayOptions.UninitializedMemory);
        VerticesObjectIdList =
            new NativeArray<int>(BaseVertices.Length, allocator, NativeArrayOptions.UninitializedMemory);
        int globalIdx = 0;
        for (int i = 0; i < baseVertices.Length; i++)
        {
            for (int j = 0; j < baseVertices[i].Length; j++)
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
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return default;
    }
}