using System;
using UnityEngine;

public class CuttableObject : MonoBehaviour
{
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;
    public MeshCollider MeshCollider;
    public Material CutFaceMaterial;
    
    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        Debug.Log($"オブジェクト{gameObject.name}  頂点数{mesh.vertexCount}");
    }
}
