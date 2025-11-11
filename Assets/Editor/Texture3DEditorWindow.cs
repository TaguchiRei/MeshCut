using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Texture3DEditorWindow : EditorWindow
{
    private ShapeCollection shapeCollection;
    private bool showPreview = true;
    private Color _baseColor = new(0, 0, 0, 0);
    private Vector2 _scrollPos;

    [MenuItem("Window/3D Texture Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<Texture3DEditorWindow>("3D Texture Editor");
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("3D Texture Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        shapeCollection = (ShapeCollection)EditorGUILayout.ObjectField(
            "Shape Collection",
            shapeCollection,
            typeof(ShapeCollection),
            false
        );

        EditorGUILayout.Space();
        showPreview = EditorGUILayout.Toggle("プレビュー表示", showPreview);

        if (shapeCollection == null)
        {
            EditorGUILayout.HelpBox("Shape Collection を指定してください。", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shapes", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(shapeCollection);
        SerializedProperty listProp = so.FindProperty("shapes");

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        EditorGUILayout.PropertyField(listProp, new GUIContent("Shape List"), true);
        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();

        _baseColor = EditorGUILayout.ColorField(_baseColor);

        if (GUILayout.Button("プレビュー更新"))
        {
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("出力"))
        {
            SaveTexture3D(Texture3DExporter.GenerateCombinedTexture3D(shapeCollection)); 
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview)
            return;

        // SceneViewでHandlesを描画
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        // 外枠（青）
        Handles.color = Color.blue;
        Handles.DrawWireCube(Vector3.zero, new Vector3(10.2f, 10.2f, 10.2f));

        // 内側（赤）
        Handles.color = _baseColor != new Color(0, 0, 0, 0) ? _baseColor : new Color(1, 0, 0, 0.5f);

        Handles.DrawWireCube(Vector3.zero, new Vector3(10f, 10f, 10f));

        // ShapeCollectionがあれば中身も描画予定
        if (shapeCollection != null)
        {
            foreach (var shape in shapeCollection.shapes)
            {
                if (shape == null) continue;

                Handles.color = shape.color;
                DrawShape(shape);
            }
        }

        // SceneView更新
        sceneView.Repaint();
    }

    private void DrawShape(ShapeData shape)
    {
        // 各形状タイプに応じたワイヤーフレーム描画
        Matrix4x4 matrix = Matrix4x4.TRS(
            shape.position,
            Quaternion.Euler(shape.rotation),
            shape.scale
        );

        using (new Handles.DrawingScope(matrix))
        {
            switch (shape.type)
            {
                case PrimitiveShapeType.Cube:
                    Handles.DrawWireCube(Vector3.zero, Vector3.one);
                    break;
                case PrimitiveShapeType.Sphere:
                    Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
                    break;
                case PrimitiveShapeType.Cylinder:
                    Handles.CylinderHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
                    break;
                case PrimitiveShapeType.Cone:
                    Handles.ConeHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
                    break;
                case PrimitiveShapeType.Torus:
                    DrawTorusWirePreview(shape);
                    break;
                // 他の形状は今後追加
            }
        }
    }

    private void DrawTorusWirePreview(ShapeData shape, int segments = 48)
    {
        float angleStep = shape.torusAngle / segments;
        Vector3 origin = shape.position;
        Quaternion rot = Quaternion.Euler(shape.rotation);

        Handles.color = shape.color;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (angleStep * i);
            Vector3 center = origin + rot * new Vector3(Mathf.Cos(angle) * shape.torusMajorRadius, 0,
                Mathf.Sin(angle) * shape.torusMajorRadius);
            DrawWireSphere(center, shape.torusTubeRadius);
        }
    }

    // 球をワイヤーフレームで描く（3軸に円を重ねる）
    private void DrawWireSphere(Vector3 center, float radius)
    {
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
    }
    
    /// <summary>
    /// 指定された Texture3D をアセットとして保存する
    /// </summary>
    /// <param name="texture">保存したい Texture3D</param>
    private void SaveTexture3D(Texture3D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Texture3D が null です。保存できません。");
            return;
        }

        // デフォルトファイル名
        string defaultName = texture.name;
        if (string.IsNullOrEmpty(defaultName))
            defaultName = "NewTexture3D";

        // 保存ダイアログを開く（Assetsフォルダ内のみ）
        string path = EditorUtility.SaveFilePanelInProject(
            "Texture3D を保存",
            defaultName,
            "asset",
            "保存先を指定してください"
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("保存がキャンセルされました。");
            return;
        }

        // 既存ファイルがある場合は削除（上書き対応）
        var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if (existing != null)
        {
            Object.DestroyImmediate(existing, true);
        }

        // Texture3D をアセットとして保存
        AssetDatabase.CreateAsset(Object.Instantiate(texture), path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Texture3D を保存しました: {path}");
    }
}