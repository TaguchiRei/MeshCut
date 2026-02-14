using System;
using System.Collections.Generic;
using UnityEngine;

public class MeshDataHolderL3 : IDisposable
{
    private readonly Dictionary<int, MeshCounterDataL3> _meshData = new();

    public int AddMeshData(Mesh mesh, Transform transform)
    {
        var meshData = new NativeMeshDataL3(mesh, transform);
        var hash = meshData.HashCode;
        if (_meshData.TryGetValue(hash, out var data))
        {
            data.Counter++;
        }
        else
        {
            _meshData[hash] = new(meshData);
        }

        return _meshData[hash].MeshDataL3.HashCode;
    }

    public bool TryGetMeshData(int hashCode, out NativeMeshDataL3 meshDataL3)
    {
        if (_meshData.TryGetValue(hashCode, out var data) && data.Counter > data.UseCounter)
        {
            meshDataL3 = data.MeshDataL3;
            data.UseCounter++;

            return true;
        }
        else
        {
            meshDataL3 = default;
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
                data.MeshDataL3.Dispose();
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        foreach (var meshCounterData in _meshData)
        {
            meshCounterData.Value.MeshDataL3.Dispose();
        }
    }
}