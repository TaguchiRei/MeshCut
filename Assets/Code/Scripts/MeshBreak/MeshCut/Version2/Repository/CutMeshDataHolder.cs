using System.Collections.Generic;

public class CutMeshDataHolder
{
    Dictionary<int, NativeMeshData> _meshData = new();

    public void AddMeshData(NativeMeshData meshData)
    {
        var hash = meshData.HashCode;
        _meshData.TryAdd(hash, meshData);
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