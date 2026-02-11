using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class MultiCutContext : IDisposable
{
    public Mesh.MeshDataArray BaseMeshDataArray;
    public NativeArray<float3> BaseVertices;
    public NativeArray<float3> BaseNormals;
    public NativeArray<float2> BaseUvs;

    public NativeList<float3> NewVertices;
    public NativeList<float3> NewNormals;
    public NativeList<float2> NewUvs;

    /// <summary> オブジェクトごとの切断処理に使う </summary>
    public NativeArray<NativePlane> Blades;

    /// <summary> 各頂点が表裏のどちらにあるのかを保持する </summary>
    public NativeArray<int> BaseVertexSide;

    /// <summary> 各頂点のオブジェクト番号 </summary>
    public NativeArray<int> ObjectIndex;

    /// <summary> 切断が必要な面を保存する </summary>
    public NativeList<float3> CutFaces;

    /// <summary> どちらに孤立頂点があるかなどの情報が分かる </summary>
    public NativeList<int> CutStatus;

    /// <summary> 頂点配列のオブジェクト毎の開始位置を保持する </summary>
    public List<int> StartIndex;

    /// <summary> 各頂点配列のオブジェクトごとの長さを保持する </summary>
    public List<int> Length;

    public MultiCutContext(int objectCount)
    {
        StartIndex = new List<int>(objectCount);
        Length = new List<int>(objectCount);
    }

    public void Dispose()
    {
        BaseMeshDataArray.Dispose();
        BaseVertices.Dispose();
        BaseNormals.Dispose();
        BaseUvs.Dispose();
        Blades.Dispose();
        BaseVertexSide.Dispose();
        ObjectIndex.Dispose();
        CutFaces.Dispose();
    }
}

public class MeshBoundary
{
    public int StartIndex;
    public int Length;
}