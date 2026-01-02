using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

/// <summary>
/// 破壊したメッシュのデータを保持するための構造体
/// </summary>
public struct NativeBreakMeshData : IDisposable
{
    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;
    public NativeNestedIntList SubIndices;

    private readonly NativeArray<float3> _baseVertices;
    private readonly NativeArray<float3> _baseNormals;
    private readonly NativeArray<float2> _baseUvs;

    private NativeArray<int> _addVerticesArray;

    public NativeBreakMeshData(BaseMeshData baseMeshData, Allocator allocator)
    {
        Vertices = new NativeList<float3>(allocator);
        Normals = new NativeList<float3>(allocator);
        Uvs = new NativeList<float2>(allocator);
        SubIndices = new NativeNestedIntList(baseMeshData.SubMeshCount * 2, allocator);

        _baseVertices = baseMeshData.Vertices;
        _baseNormals = baseMeshData.Normals;
        _baseUvs = baseMeshData.Uvs;

        _addVerticesArray = new NativeArray<int>(baseMeshData.Vertices.Length, allocator);
        for (int i = 0; i < _addVerticesArray.Length; i++)
        {
            _addVerticesArray[i] = -1;
        }
    }

    public void AddTriangle(int p1, int p2, int p3, int submesh)
    {
        int3 triangle = new int3(GetOrAddVertex(p1), GetOrAddVertex(p2), GetOrAddVertex(p3));
        SubIndices.AddElement(submesh, triangle);
    }

    public void AddTriangle(NativeTriangleData triangleData, float3 faceNormal, int submesh)
    {
        float3 normal = math.cross(
            triangleData.Vertex1 - triangleData.Vertex0,
            triangleData.Vertex2 - triangleData.Vertex0);

        int baseIndex = Vertices.Length;

        int3 triangle = new int3(baseIndex, baseIndex + 1, baseIndex + 2);
        SubIndices.AddElement(submesh, triangle);

        if (math.dot(normal, faceNormal) < 0)
        {
            Vertices.Add(triangleData.Vertex2);
            Vertices.Add(triangleData.Vertex1);
            Vertices.Add(triangleData.Vertex0);

            Normals.Add(triangleData.Normal2);
            Normals.Add(triangleData.Normal1);
            Normals.Add(triangleData.Normal0);

            Uvs.Add(triangleData.Uv2);
            Uvs.Add(triangleData.Uv1);
            Uvs.Add(triangleData.Uv0);
        }
        else
        {
            Vertices.Add(triangleData.Vertex0);
            Vertices.Add(triangleData.Vertex1);
            Vertices.Add(triangleData.Vertex2);

            Normals.Add(triangleData.Normal0);
            Normals.Add(triangleData.Normal1);
            Normals.Add(triangleData.Normal2);

            Uvs.Add(triangleData.Uv0);
            Uvs.Add(triangleData.Uv1);
            Uvs.Add(triangleData.Uv2);
        }
    }

    private int GetOrAddVertex(int index)
    {
        if (_addVerticesArray[index] != -1)
        {
            return _addVerticesArray[index];
        }

        int newIndex = Vertices.Length;
        _addVerticesArray[index] = newIndex;
        Vertices.Add(_baseVertices[index]);
        Normals.Add(_baseNormals[index]);
        Uvs.Add(_baseUvs[index]);
        return newIndex;
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Normals.Dispose();
        Uvs.Dispose();
        SubIndices.Dispose();
    }
}

public struct BaseMeshData
{
    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeArray<float2> Uvs;
    public NativeParallelMultiHashMap<int, int3> SubIndices;
    public int SubMeshCount;
}

public struct NativeTriangleData
{
    public float3 Vertex0;
    public float3 Vertex1;
    public float3 Vertex2;

    public float3 Normal0;
    public float3 Normal1;
    public float3 Normal2;

    public float2 Uv0;
    public float2 Uv1;
    public float2 Uv2;

    public void SetVertices(float3 vertex0, float3 vertex1, float3 vertex2)
    {
        Vertex0 = vertex0;
        Vertex1 = vertex1;
        Vertex2 = vertex2;
    }

    public void SetNormals(float3 normal0, float3 normal1, float3 normal2)
    {
        Normal0 = normal0;
        Normal1 = normal1;
        Normal2 = normal2;
    }

    public void SetUvs(float2 uv0, float2 uv1, float2 uv2)
    {
        Uv0 = uv0;
        Uv1 = uv1;
        Uv2 = uv2;
    }
}

public struct NativeNestedIntList : IDisposable
{
    /// <summary>
    /// 各リストを識別するIDをキー、中身を値とする。
    /// DictionaryのBurst対応、一つのキーに複数Valueを付与できるバージョン
    /// </summary>
    private NativeParallelMultiHashMap<int, int3> dataMap;

    private int maxCount;

    public NativeNestedIntList(int initialCapacity, Allocator allocator)
    {
        dataMap = new NativeParallelMultiHashMap<int, int3>(initialCapacity, allocator);
        maxCount = 0;
    }

    /// <summary>
    /// 特定のリスト（listIndex）に値を追加します
    /// </summary>
    public void AddElement(int listIndex, int3 value)
    {
        dataMap.Add(listIndex, value);
        if (listIndex > maxCount) maxCount = listIndex + 0;
    }

    public int Count()
    {
        return maxCount;
    }

    public void Dispose()
    {
        if (dataMap.IsCreated)
        {
            dataMap.Dispose();
        }
    }
}