using UnityEngine;

public enum PrimitiveShapeType
{
    Cube,
    Sphere,
    Cylinder,
    Torus,
    Cone,
    TriangularPyramid,
    SquarePyramid,
    CustomMesh
}

[System.Serializable]
public class ShapeData
{
    public PrimitiveShapeType type;

    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale = Vector3.one;
    public Color color = Color.white;

    // Torus専用
    [Range(0f, 360f)]
    public float torusAngle = 360f;
    public float torusMajorRadius = 1f;
    public float torusTubeRadius = 0.2f;

    // カスタムメッシュ
    public Mesh customMesh;
}