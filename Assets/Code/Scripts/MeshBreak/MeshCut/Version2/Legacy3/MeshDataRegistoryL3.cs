using System;
using UnityEngine;

/// <summary>
/// MeshDataHolderおよびCutMeshDataHolderを保持し、それらのデータへのアクセスを受け付ける
/// </summary>
public class MeshDataRegistoryL3 : MonoBehaviour
{
    /// <summary> 最初から置かれた切断オブジェクトを取得する </summary>
    private MeshDataHolderL3 _meshDataHolderL3;

    /// <summary> 切断後のデータを 保持する</summary>
    private CutMeshDataHolderL3 _cutMeshDataHolderL3;

    private void Start()
    {
        _meshDataHolderL3 = new();
        _cutMeshDataHolderL3 = new();

        var breakableObjects = GetComponentsInChildren<BreakableObjectL3>();

        for (int i = 0; i < breakableObjects.Length; i++)
        {
            breakableObjects[i].HashCode =
                _meshDataHolderL3.AddMeshData(breakableObjects[i].BreakableMesh, breakableObjects[i].transform);
        }
    }

    /// <summary>
    /// メッシュを取得する
    /// </summary>
    /// <param name="hash">メッシュデータのハッシュコード</param>
    /// <param name="cutMesh">切断済みメッシュならtrueにする</param>
    /// <param name="meshDataL3">取得したメッシュデータ</param>
    /// <returns></returns>
    public bool GetMesh(int hash, bool cutMesh, out NativeMeshDataL3 meshDataL3)
    {
        if (!cutMesh)
        {
            return _meshDataHolderL3.TryGetMeshData(hash, out meshDataL3);
        }
        else
        {
            return _cutMeshDataHolderL3.TryGetMeshData(hash, out meshDataL3);
        }
    }

    /// <summary>
    /// 切断後のメッシュデータを追加するのに利用する
    /// </summary>
    /// <param name="meshDataL3"></param>
    /// <returns>メッシュデータのhash</returns>
    public int AddCutMesh(NativeMeshDataL3 meshDataL3)
    {
        return _cutMeshDataHolderL3.AddMeshData(meshDataL3);
    }

    private void OnDestroy()
    {
        _meshDataHolderL3.Dispose();
        _cutMeshDataHolderL3.Dispose();
    }
}