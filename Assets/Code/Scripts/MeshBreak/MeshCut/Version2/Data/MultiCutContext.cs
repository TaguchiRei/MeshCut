using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class MultiCutContext : IDisposable
{
    public Mesh.MeshDataArray BaseMeshDataArray;
    public NativeArray<float3> BaseVertices;

    /// <summary> オブジェクトごとの切断処理に使う </summary>
    public NativeArray<NativePlane> Blades;

    /// <summary> 各頂点が表裏のどちらにあるのかを保持する </summary>
    public NativeArray<int> BaseVertexSide;

    /// <summary> 各頂点のオブジェクト番号 </summary>
    public NativeArray<int> ObjectIndex;


    public void Dispose()
    {
        if (BaseVertices.IsCreated) BaseVertices.Dispose();
        if (Blades.IsCreated) Blades.Dispose();
        if (BaseVertexSide.IsCreated) BaseVertexSide.Dispose();
        if (ObjectIndex.IsCreated) ObjectIndex.Dispose();
        BaseMeshDataArray.Dispose();
    }
}

public class MeshBoundary
{
    public int StartIndex;
    public int Length;
}