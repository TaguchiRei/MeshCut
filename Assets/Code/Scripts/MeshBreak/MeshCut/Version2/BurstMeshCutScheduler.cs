using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// メッシュカットのスケジュールを担当する
/// </summary>
public class BurstMeshCutScheduler
{
    private INativeMeshRepository meshRepository;

    private UniTask cutTask;

    public void Cut(NativePlane blade, NativeMeshData[] meshData)
    {
        cutTask = CutTaskAsync(blade, meshData);
    }

    private void GenerateBlade(NativePlane blade, NativeMeshData[] meshData, NativeList<NativePlane> blades)
    {
        int start = 0;
        for (int i = 0; i < meshData.Length; i++)
        {
            var length = meshData[i].Vertices.Length;
            NativeArray<float3>.
        }
    }

    private async UniTask CutTaskAsync(NativePlane blade, NativeMeshData[] meshData)
    {
        GenerateBlade(blade, meshData);
    }
}

public interface INativeMeshRepository
{
    bool GetMesh(int hash, bool cutMesh, out NativeMeshData meshData);
}