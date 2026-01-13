using System;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// メッシュ切断の過程で生まれる一時配列をすべて保持するクラス。
/// 処理完了後に一括でDisposeできるようにする
/// </summary>
public class MeshCutContext : IDisposable
{
    public JobHandle CutJobHandle;

    /// <summary> 各頂点とオブジェクトの対応表 </summary>
    public NativeArray<int> VerticesObjectIdList;

    /// <summary> 乱雑に並んだ全頂点 </summary>
    public NativeArray<float3> Vertices;

    /// <summary> 同一インデックス番号の頂点の法線 </summary>
    public NativeArray<float3> Normals;

    /// <summary> 同一インデックス番号の頂点のUV座標 </summary>
    public NativeArray<float2> Uvs;

    /// <summary> 全三角形を保持 </summary>
    public NativeArray<NativeTriangle> Triangles;

    /// <summary> Triangles配列のオブジェクトごとのスタート位置と終了位置をオブジェクト順で保持 </summary>
    public NativeArray<StartAndLength> TrianglesStartAndLength;

    /// <summary> 各オブジェクトごとの切断面をオブジェクト順に保持 </summary>
    public NativeArray<NativePlane> Planes;

    /// <summary> 各オブジェクトのTransform情報をオブジェクト順に保持 </summary>
    public NativeArray<NativeTransform> Transforms;

    /// <summary> 各頂点が面に対してどの方向にあるのかを保持する </summary>
    public NativeArray<int> VertSide;

    public async UniTask Complete()
    {
        await UniTask.WaitUntil(() => CutJobHandle.IsCompleted);
    }

    public void Dispose()
    {
        CutJobHandle.Complete();
        
        VerticesObjectIdList.Dispose();
        Vertices.Dispose();
        Normals.Dispose();
        Uvs.Dispose();
        Triangles.Dispose();
        TrianglesStartAndLength.Dispose();
        Planes.Dispose();
        Transforms.Dispose();
        VertSide.Dispose();
    }
}

public struct StartAndLength
{
    public int Start;
    public int Length;
}