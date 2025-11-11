using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShapeCollection", menuName = "3DTexture/Shape Collection")]
public class ShapeCollection : ScriptableObject
{
    [Tooltip("この配列の中でShapeDataを直接編集可能です")]
    public List<ShapeData> shapes = new List<ShapeData>();

    [Header("3Dテクスチャ全体の設定")]
    public int textureSize = 10;
    public Color emptyColor = Color.black;
}