using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct BurstBreakMeshData : IDisposable
{
    public NativeList<float3> Vertices;
    public NativeList<float3> Normals;
    public NativeList<float2> Uvs;
    public NativeFloatList2D SubIndices;


    public void Dispose()
    {
        Vertices.Dispose();
        Normals.Dispose();
        Uvs.Dispose();
        SubIndices.Dispose();
    }
}

public struct NativeFloatList2D : IDisposable
{
    private NativeList<int> _indexCount;
    private NativeList<int> _valueList;

    private NativeList<int> _rowOffsets;

    public NativeFloatList2D(NativeList<int> indexCount, NativeList<int> valueList, NativeList<int> rowOffsets)
    {
        _indexCount = indexCount;
        _valueList = valueList;
        _rowOffsets = rowOffsets;
        _indexCount.Clear();
        _valueList.Clear();
        _rowOffsets.Clear();
    }

    public int this[int row, int col]
    {
        get => _valueList[_rowOffsets[row] + col];
        set => _valueList[_rowOffsets[row] + col] = value;
    }

    public void AddList()
    {
        _indexCount.Add(0);
        RecalculationRowOffset();
    }

    public void Add(int row, int value)
    {
        int pos = _rowOffsets[row] + _indexCount[row];
        _valueList.InsertRange(pos, 1);
        _valueList[pos] = value;
        _indexCount[row]++;
        RecalculationRowOffset();
    }

    public void Remove(int index)
    {
        _valueList.RemoveAt(_rowOffsets[index] + (_indexCount[index] - 1));
        _indexCount[index]--;
        RecalculationRowOffset();
    }

    private void RecalculationRowOffset()
    {
        _rowOffsets.Clear();
        _rowOffsets.Add(0);
        for (int i = 1; i < _indexCount.Length; i++)
        {
            _rowOffsets.Add(_rowOffsets[i - 1] + _indexCount[i - 1]);
        }
    }

    public void Dispose()
    {
        _indexCount.Dispose();
        _valueList.Dispose();
        _rowOffsets.Dispose();
    }
}