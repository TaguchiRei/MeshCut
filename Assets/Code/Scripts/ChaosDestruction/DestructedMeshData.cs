using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// メッシュ破壊後のデータを保持
/// </summary>
public class DestructedMeshData
{
    public List<Vector3> _vertices = new();
    public List<Vector3> _normals = new();
    public List<Vector2> _uvs = new();
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="vertices"></param>
    public DestructedMeshData(List<Vector3> vertices)
    {
        
    }
}
