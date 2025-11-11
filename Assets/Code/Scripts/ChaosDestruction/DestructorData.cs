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
    /// <param name="scale">分割後の大きさの割合 0~1 </param>
    /// <param name="roughness">分割点をどの程度ずらすか</param>
    /// <param name="vertices"></param>
    public DestructorData(List<Vector3> vertices)
    {
        
    }
}
