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

    /// <summary> ベースの各頂点とオブジェクトの対応配列。長さはベースオブジェクト群の長さ </summary>
    public NativeArray<int> VerticesObjectIdList;

    /// <summary> 乱雑に並んだ全頂点 </summary>
    public NativeArray<float3> Vertices;

    /// <summary> 同一インデックス番号の頂点の法線 </summary>
    public NativeArray<float3> Normals;

    /// <summary> 同一インデックス番号の頂点のUV座標 </summary>
    public NativeArray<float2> Uvs;

    /// <summary> 全三角形を保持 </summary>
    public NativeList<NativeTriangle> Triangles;

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

    public MeshCutContext(int verticesArrayCount, int objectCount, int triangleCount,
        NativeTransform[] transforms,
        Allocator allocator)
    {
        VerticesObjectIdList = new(verticesArrayCount, allocator, NativeArrayOptions.UninitializedMemory);
        Vertices = new(verticesArrayCount + triangleCount * 2, allocator, NativeArrayOptions.UninitializedMemory);
        Normals = new(verticesArrayCount + triangleCount * 2, allocator, NativeArrayOptions.UninitializedMemory);
        Uvs = new(verticesArrayCount + triangleCount * 2, allocator, NativeArrayOptions.UninitializedMemory);
        Triangles = new(triangleCount * 3, allocator);
        Blades = new(objectCount, allocator, NativeArrayOptions.UninitializedMemory);
        Transforms = new(transforms.Length, allocator, NativeArrayOptions.UninitializedMemory);
        Transforms.CopyFrom(transforms);
        VertSide = new(verticesArrayCount, allocator, NativeArrayOptions.UninitializedMemory);
    }

    public void Dispose()
    {
        CutJobHandle.Complete();

        if (VerticesObjectIdList.IsCreated) VerticesObjectIdList.Dispose();
        if (Vertices.IsCreated) Vertices.Dispose();
        if (Normals.IsCreated) Normals.Dispose();
        if (Uvs.IsCreated) Uvs.Dispose();
        if (Triangles.IsCreated) Triangles.Dispose();
        if (Blades.IsCreated) Blades.Dispose();
        if (Transforms.IsCreated) Transforms.Dispose();
        if (VertSide.IsCreated) VertSide.Dispose();
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        CutJobHandle.Complete();

        VerticesObjectIdList.Dispose(inputDeps);
        Vertices.Dispose(inputDeps);
        Normals.Dispose(inputDeps);
        Uvs.Dispose(inputDeps);
        Triangles.Dispose(inputDeps);
        Blades.Dispose(inputDeps);
        Transforms.Dispose(inputDeps);
        VertSide.Dispose(inputDeps);
        return CutJobHandle;
    }
}

public struct StartAndLength
{
    public int Start;
    public int Length;
}