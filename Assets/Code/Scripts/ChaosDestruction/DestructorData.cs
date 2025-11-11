using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// メッシュ破壊用の空間指定の仮メッシュのデータ
/// </summary>
public class DestructorData
{
    private readonly List<Vector3> _destructVertices = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vertices"></param>
    public DestructorData(List<Vector3> vertices)
    {
        ClearAll();
        
        //
    }

    private void InitDestructorVertices(List<Vector3> vertices)
    {
        float xMax = float.MinValue;
        float xMin = float.MaxValue;
        float yMax = float.MinValue;
        float yMin = float.MaxValue;
        float zMax = float.MinValue;
        float zMin = float.MaxValue;

        //最も広い頂点をサンプリング
        foreach (var pos in vertices)
        {
            if (pos.x > xMax) xMax = pos.x;
            if (pos.x < xMin) xMin = pos.x;
            if (pos.y > yMax) yMax = pos.y;
            if (pos.y < yMin) yMin = pos.y;
            if (pos.z > zMax) zMax = pos.z;
            if (pos.z < zMin) zMin = pos.z;
        }

        //範囲を少し広げて面上に元の頂点がないようにする
        //Xmax++;
        //Ymax++;
        //Zmax++;
        //Xmin--;
        //Ymin--;
        //Zmin--;
        
        _destructVertices.Add(new Vector3(xMin, yMax, zMax));
        _destructVertices.Add(new Vector3(xMax, yMax, zMax));
        _destructVertices.Add(new Vector3(xMin, yMax, zMin));
        _destructVertices.Add(new Vector3(xMax, yMax, zMin));
        _destructVertices.Add(new Vector3(xMin, yMin, zMax));
        _destructVertices.Add(new Vector3(xMax, yMin, zMax));
        _destructVertices.Add(new Vector3(xMin, yMin, zMin));
        _destructVertices.Add(new Vector3(xMax, yMin, zMin));
    }

    private void GenerateInsideVertex()
    {
        
    }

    private void ClearAll()
    {
        _destructVertices.Clear();
    }
}
