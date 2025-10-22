using System;
using UnityEngine;

public class CuttableObject : MonoBehaviour
{
    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        Debug.Log($"オブジェクト{gameObject.name}  頂点数{mesh.vertexCount}");
    }
}
