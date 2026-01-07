using System;
using System.Collections.Generic;

public class CutMeshDataHolder : IDisposable
{
    Dictionary<int, NativeMeshUsingCheck> _meshData = new();

    private class NativeMeshUsingCheck
    {
        public NativeMeshData NativeMeshData;
        public bool IsUsing;

        public NativeMeshUsingCheck(NativeMeshData nativeMeshData)
        {
            NativeMeshData = nativeMeshData;
            IsUsing = false;
        }
    }

    public int AddMeshData(NativeMeshData editMeshData)
    {
        var meshData = new NativeMeshData(editMeshData);
        _meshData[meshData.HashCode] = new(meshData);
        return meshData.HashCode;
    }

    public bool TryGetMeshData(int hashCode, out NativeMeshData meshData)
    {
        if (_meshData.TryGetValue(hashCode, out var data) && !data.IsUsing)
        {
            meshData = data.NativeMeshData;
            data.IsUsing = true;
            return true;
        }

        meshData = default;
        return false;
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
            data.NativeMeshData.Dispose();
            _meshData.Remove(hashCode);
        }
    }

    public void Dispose()
    {
        foreach (var valueTuple in _meshData)
        {
            valueTuple.Value.NativeMeshData.Dispose();
        }
    }
}