using System;
using System.Collections.Generic;
using UnityEngine;

public class MeshDataHolder : IDisposable
{
    private readonly Dictionary<int, MeshCounterData> _meshData = new();

    public int AddMeshData(Mesh mesh, Transform transform)
    {
        var meshData = new NativeMeshData(mesh, transform);
        var hash = meshData.HashCode;
        if (_meshData.TryGetValue(hash, out var data))
        {
            data.Counter++;
        }
        else
        {
            _meshData[hash] = new(meshData);
        }

        return _meshData[hash].MeshData.HashCode;
    }

    public bool TryGetMeshData(int hashCode, out NativeMeshData meshData)
    {
        if (_meshData.TryGetValue(hashCode, out var data) && data.Counter > data.UseCounter)
        {
            meshData = data.MeshData;
            data.UseCounter++;

            return true;
        }
        else
        {
            meshData = default;
            return false;
        }
    }

    /// <summary>
    /// 特定のメッシュデータをこれ以上利用しない場合にこのメソッドを呼ぶ
    /// </summary>
    /// <param name="hashCode"></param>
    /// <returns></returns>
    public bool CompleteUse(int hashCode)
    {
        if (_meshData.TryGetValue(hashCode, out var data))
        {
            data.UseCounter--;
            data.Counter--;

            if (data.Counter < 1 && data.UseCounter < 1)
            {
                _meshData.Remove(hashCode);
                data.MeshData.Dispose();
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        foreach (var meshCounterData in _meshData)
        {
            meshCounterData.Value.MeshData.Dispose();
        }
    }
}