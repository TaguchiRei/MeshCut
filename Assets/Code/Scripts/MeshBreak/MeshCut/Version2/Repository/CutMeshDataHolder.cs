using System.Collections.Generic;
using UnityEngine;

public class CutMeshDataHolder
{
    Dictionary<int, NativeMeshData> _meshData = new();

    public void AddMeshData(Mesh mesh)
    {
        var meshData = new NativeMeshData(mesh);
        var hash = meshData.HashCode;
        _meshData.TryAdd(hash, meshData);
    }

    public int AddMeshData(NativeEditMeshData editMeshData)
    {
        var meshData = new NativeMeshData(editMeshData);
        _meshData[meshData.HashCode] = meshData;
        return meshData.HashCode;
    }

    public bool TryGetMeshData(int hashCode, out NativeMeshData meshData)
    {
        return _meshData.TryGetValue(hashCode, out meshData);
    }

    /// <summary>
    /// 特定のメッシュデータをこれ以上利用しない場合にこのメソッドを呼ぶ
    /// </summary>
    /// <param name="hashCode"></param>
    /// <returns></returns>
    public void CompleteUse(int hashCode)
    {
        if (_meshData.TryGetValue(hashCode, out var data))
        {
            data.Dispose();
        }
    }
}