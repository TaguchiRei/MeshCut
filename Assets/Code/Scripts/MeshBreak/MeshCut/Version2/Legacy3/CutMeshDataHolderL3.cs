using System;
using System.Collections.Generic;

public class CutMeshDataHolderL3 : IDisposable
{
    Dictionary<int, NativeMeshUsingCheck> _meshData = new();

    private class NativeMeshUsingCheck
    {
        public NativeMeshDataL3 NativeMeshDataL3;
        public bool IsUsing;

        public NativeMeshUsingCheck(NativeMeshDataL3 nativeMeshDataL3)
        {
            NativeMeshDataL3 = nativeMeshDataL3;
            IsUsing = false;
        }
    }

    public int AddMeshData(NativeMeshDataL3 editMeshDataL3)
    {
        var meshData = new NativeMeshDataL3(editMeshDataL3);
        _meshData[meshData.HashCode] = new(meshData);
        return meshData.HashCode;
    }

    public bool TryGetMeshData(int hashCode, out NativeMeshDataL3 meshDataL3)
    {
        if (_meshData.TryGetValue(hashCode, out var data) && !data.IsUsing)
        {
            meshDataL3 = data.NativeMeshDataL3;
            data.IsUsing = true;
            return true;
        }

        meshDataL3 = default;
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
            data.NativeMeshDataL3.Dispose();
            _meshData.Remove(hashCode);
        }
    }

    public void Dispose()
    {
        foreach (var valueTuple in _meshData)
        {
            valueTuple.Value.NativeMeshDataL3.Dispose();
        }
    }
}