using UnityEngine;
using UnityEditor;

public static class Texture3DExporter
{
    public static Texture3D GenerateTexture3D(ShapeData shape, int size, Color emptyColor)
    {
        Texture3D texture = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size * size];

        // 空白部分初期化
        for (int i = 0; i < colors.Length; i++)
            colors[i] = emptyColor;

        Vector3 pos = shape.position;
        Vector3 scale = shape.scale;

        // Cubeの例
        if (shape.type == PrimitiveShapeType.Cube)
        {
            int minX = Mathf.FloorToInt((pos.x - scale.x / 2 + size / 2f));
            int maxX = Mathf.CeilToInt((pos.x + scale.x / 2 + size / 2f));
            int minY = Mathf.FloorToInt((pos.y - scale.y / 2 + size / 2f));
            int maxY = Mathf.CeilToInt((pos.y + scale.y / 2 + size / 2f));
            int minZ = Mathf.FloorToInt((pos.z - scale.z / 2 + size / 2f));
            int maxZ = Mathf.CeilToInt((pos.z + scale.z / 2 + size / 2f));

            for (int x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= size) continue;
                for (int y = minY; y <= maxY; y++)
                {
                    if (y < 0 || y >= size) continue;
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (z < 0 || z >= size) continue;
                        int idx = x + y * size + z * size * size;
                        colors[idx] = shape.color;
                    }
                }
            }
        }

        // TODO: Sphereや外部モデルもボクセル化可能

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    public static void ExportAllShapes(ShapeCollection collection, int size, Color emptyColor)
    {
        if (collection == null)
        {
            Debug.LogError("ShapeCollectionが指定されていません");
            return;
        }

        string folder = EditorUtility.SaveFolderPanel("Save Texture3Ds", "Assets", "");
        if (string.IsNullOrEmpty(folder)) return;

        for (int i = 0; i < collection.shapes.Count; i++)
        {
            ShapeData shape = collection.shapes[i];
            if (shape == null) continue;

            Texture3D tex = GenerateTexture3D(shape, size, emptyColor);

            string localPath = "Assets" + folder.Substring(Application.dataPath.Length) + $"/Texture3D_{i}.asset";
            AssetDatabase.CreateAsset(tex, localPath);
        }

        AssetDatabase.SaveAssets();
    }

    public static Texture3D GenerateCombinedTexture3D(ShapeCollection collection)
    {
        int size = collection.textureSize;
        Color emptyColor = collection.emptyColor;

        Texture3D tex = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size * size];
        for (int i = 0; i < colors.Length; i++) colors[i] = emptyColor;

        Vector3 half = Vector3.one * size / 2f;

        // 後ろ（インデックスが大きい）から順に塗り込む
        for (int s = collection.shapes.Count - 1; s >= 0; s--)
        {
            ShapeData shape = collection.shapes[s];
            if (shape == null) continue;

            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            for (int z = 0; z < size; z++)
            {
                Vector3 p = new Vector3(x, y, z) - half - shape.position;
                p = Quaternion.Inverse(Quaternion.Euler(shape.rotation)) *
                    Vector3.Scale(p, new Vector3(
                        1f / shape.scale.x,
                        1f / shape.scale.y,
                        1f / shape.scale.z
                    ));

                bool inside = false;

                switch (shape.type)
                {
                    case PrimitiveShapeType.Cube:
                        inside = Mathf.Abs(p.x) <= 0.5f && Mathf.Abs(p.y) <= 0.5f && Mathf.Abs(p.z) <= 0.5f;
                        break;

                    case PrimitiveShapeType.Sphere:
                        inside = p.sqrMagnitude <= 0.25f;
                        break;

                    case PrimitiveShapeType.Cylinder:
                        inside = (p.x * p.x + p.z * p.z <= 0.25f) && Mathf.Abs(p.y) <= 0.5f;
                        break;

                    case PrimitiveShapeType.Cone:
                        if (p.y >= -0.5f && p.y <= 0.5f)
                        {
                            float r = 0.5f * (1f - (p.y + 0.5f));
                            inside = (p.x * p.x + p.z * p.z) <= r * r;
                        }

                        break;

                    case PrimitiveShapeType.Torus:
                    {
                        float R = shape.torusMajorRadius * 0.5f;
                        float r = shape.torusTubeRadius * 0.5f;
                        float q = Mathf.Sqrt(p.x * p.x + p.z * p.z) - R;
                        inside = q * q + p.y * p.y <= r * r;
                    }
                        break;
                }

                if (inside)
                {
                    int idx = x + y * size + z * size * size;
                    colors[idx] = shape.color;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }
}