using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MeshDataHolderおよびCutMeshDataHolderを保持し、それらのデータへのアクセスを受け付ける
/// </summary>
public class MeshDataRegistory : MonoBehaviour
{
    private void Start()
    {
        GetComponentsInChildren<BreakableObject>();
    }
}
