using System;
using System.Collections.Generic;
using MeshBreak;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class CutMeshData
{
    private BreakMeshData _leftMeshData;
    private BreakMeshData _rightMeshData;

    /// <summary> 切断面の頂点同士のつながりを保存する </summary>
    private readonly Dictionary<Vector3, List<Vector3>> _capConnections = new();

    private Plane _blade;
    private BaseMeshData _baseMeshData;

    /// <summary> 全頂点の方向を保存する </summary>
    private bool[] _baseVerticesSide;

    private static TriangleData _triangleData = new();

    public BreakMeshData[] Cut(BaseMeshData baseMesh, Plane blade)
    {
        _blade = blade;
        _baseMeshData = baseMesh;
        _baseVerticesSide = new bool[_baseMeshData.Vertices.Length];

        //エラーを出さないための狩りの戻り値。完成時はBreakMeshData[]を返す
        return default;
    }
}

[BurstCompile]
public static class CutMeshUtility
{
    [BurstCompile]
    public static void SortOutVertices(
        float3 planePoint,
        float3 planeNormal,
        [ReadOnly] NativeArray<float3> baseVertices,
        ref NativeList<float3> frontVertices,
        ref NativeList<float3> backVertices)
    {
        foreach (var baseVertex in baseVertices)
        {
            if (math.dot(baseVertex - planePoint, planeNormal) > 0f)
            {
                frontVertices.Add(baseVertex);
            }
            else
            {
                backVertices.Add(baseVertex);
            }
        }
    }
}