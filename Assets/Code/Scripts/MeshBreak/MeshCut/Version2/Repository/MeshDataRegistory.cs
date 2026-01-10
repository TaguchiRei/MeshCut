using System;
using UnityEngine;

/// <summary>
/// MeshDataHolderおよびCutMeshDataHolderを保持し、それらのデータへのアクセスを受け付ける
/// </summary>
public class MeshDataRegistory : MonoBehaviour
{
    /// <summary> 最初から置かれた切断オブジェクトを取得する </summary>
    private MeshDataHolder _meshDataHolder;

    /// <summary> 切断後のデータを 保持する</summary>
    private CutMeshDataHolder _cutMeshDataHolder;

    private void Start()
    {
        _meshDataHolder = new();
        _cutMeshDataHolder = new();

        var breakableObjects = GetComponentsInChildren<BreakableObject>();

        for (int i = 0; i < breakableObjects.Length; i++)
        {
            breakableObjects[i].HashCode =
                _meshDataHolder.AddMeshData(breakableObjects[i].BreakableMesh, breakableObjects[i].transform);
        }
    }

    /// <summary>
    /// メッシュを取得する
    /// </summary>
    /// <param name="hash">メッシュデータのハッシュコード</param>
    /// <param name="cutMesh">切断済みメッシュならtrueにする</param>
    /// <param name="meshData">取得したメッシュデータ</param>
    /// <returns></returns>
    public bool GetMesh(int hash, bool cutMesh, out NativeMeshData meshData)
    {
        if (!cutMesh)
        {
            return _meshDataHolder.TryGetMeshData(hash, out meshData);
        }
        else
        {
            return _cutMeshDataHolder.TryGetMeshData(hash, out meshData);
        }
    }

    /// <summary>
    /// 切断後のメッシュデータを追加するのに利用する
    /// </summary>
    /// <param name="meshData"></param>
    /// <returns>メッシュデータのhash</returns>
    public int AddCutMesh(NativeMeshData meshData)
    {
        return _cutMeshDataHolder.AddMeshData(meshData);
    }

    private void OnDestroy()
    {
        _meshDataHolder.Dispose();
        _cutMeshDataHolder.Dispose();
    }
}