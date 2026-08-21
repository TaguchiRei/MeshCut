using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// このプロジェクトには MultiCutBlade / CuttableObject / MeshCutObjectPool と同名の型が
// グローバル名前空間(Version2)にも存在する。C# はグローバル名前空間の型を using でインポートした型より
// 優先するため、`using UsefulMeshCut;` と書いてもエラーも警告も出ないまま Version2 の型が使われてしまう。
// 型エイリアスも CS0576 で衝突するため、名前空間エイリアスで完全に区別する。
using Pkg = UsefulMeshCut;
using V4 = MeshBreak.MeshCut.Version4;

namespace MeshCutTest.Editor
{
    /// <summary>
    /// MeshCut Version4 の動作確認用シーンを、マテリアル・破片プレハブごと一括生成するツール。
    /// 開発ディレクトリの Version4 と、そこから切り出した UsefulMeshCut パッケージのどちらも対象にできる。
    /// </summary>
    public class UsefulMeshCutTestSceneBuilder : EditorWindow
    {
        /// <summary> どちらの実装のシーンを作るか </summary>
        private enum CutTarget
        {
            /// <summary> 開発ディレクトリの MeshBreak.MeshCut.Version4 </summary>
            DevVersion4,

            /// <summary> Version4 を切り出した UsefulMeshCut パッケージ </summary>
            Package
        }

        private const string RootFolder = "Assets/UsefulMeshCutTest";
        private const string BodyMaterialPath = RootFolder + "/TestBody.mat";
        private const string CapMaterialPath = RootFolder + "/TestCap.mat";
        private const string PhysicsMaterialPath = RootFolder + "/TestFragment.physicMaterial";

        private CutTarget _target = CutTarget.DevVersion4;

        // 切断対象の配置
        private PrimitiveType _primitiveType = PrimitiveType.Cube;
        private int _gridX = 3;
        private int _gridZ = 3;
        private float _spacing = 1.5f;

        /// <summary> 切断対象を並べる高さ。Bladeもこの高さに置くので、刃が対象の中心を通る。 </summary>
        private float _objectHeight = 2f;

        // 各種設定
        private int _colliderNum = 10;
        private bool _canMultiCut;
        private bool _enableProfileLog = true;

        private Vector2 _scrollPos;

        private string ScenePath =>
            _target == CutTarget.Package
                ? RootFolder + "/MeshCutTestScene_Package.unity"
                : RootFolder + "/MeshCutTestScene_V4.unity";

        private string PrefabPath =>
            _target == CutTarget.Package
                ? RootFolder + "/MeshCutFragment_Package.prefab"
                : RootFolder + "/MeshCutFragment_V4.prefab";

        [MenuItem("UsefulTools/UsefulMesh/MeshCut/テストシーンを生成")]
        public static void ShowWindow()
        {
            var window = GetWindow<UsefulMeshCutTestSceneBuilder>("MeshCut Test Scene");
            window.minSize = new Vector2(380, 480);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.HelpBox(
                "MeshCut Version4 の動作確認用シーンを生成します。\n" +
                "マテリアル・物理マテリアル・破片プレハブも同時に作成し、" + RootFolder + " に保存します。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("対象の実装", EditorStyles.boldLabel);
            _target = (CutTarget)EditorGUILayout.EnumPopup(
                new GUIContent("実装", "どちらの Version4 実装でシーンを組むか"),
                _target);

            EditorGUILayout.LabelField(
                _target == CutTarget.Package
                    ? "UsefulMeshCut パッケージ (Packages/com.rei.usefulmeshcut)"
                    : "開発ディレクトリの Version4 (MeshBreak.MeshCut.Version4)",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("切断対象", EditorStyles.boldLabel);
            _primitiveType = (PrimitiveType)EditorGUILayout.EnumPopup(
                new GUIContent("形状", "Cubeは12三角形、Sphereは768三角形。負荷を変えたいときに切り替える"),
                _primitiveType);
            _gridX = Mathf.Max(1, EditorGUILayout.IntField("X方向の数", _gridX));
            _gridZ = Mathf.Max(1, EditorGUILayout.IntField("Z方向の数", _gridZ));
            _spacing = EditorGUILayout.FloatField("間隔", _spacing);
            _objectHeight = EditorGUILayout.FloatField(
                new GUIContent("配置する高さ", "Bladeも同じ高さに置かれ、対象の中心を水平に切ります"),
                _objectHeight);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);
            _colliderNum = Mathf.Max(7, EditorGUILayout.IntField(
                new GUIContent("球コライダー数", "破片の形を近似する球コライダーの数(7以上)"),
                _colliderNum));

            using (new EditorGUI.DisabledScope(_target != CutTarget.Package))
            {
                _canMultiCut = EditorGUILayout.Toggle(
                    new GUIContent("複数回切断を許可", "パッケージ版のみ。オフだと1回だけ切断でき、破片は切れません"),
                    _canMultiCut);

                _enableProfileLog = EditorGUILayout.Toggle(
                    new GUIContent("処理時間ログ", "パッケージ版のみ。開発版Version4は常時出力します"),
                    _enableProfileLog);
            }

            int objectCount = _gridX * _gridZ;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("切断対象数", $"{objectCount} 個");
            EditorGUILayout.LabelField("プール生成数", $"{objectCount * 2} 個 (対象数 × 2)");

            EditorGUILayout.Space(15);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            if (GUILayout.Button("テストシーンを生成", btnStyle, GUILayout.Height(40)))
            {
                // シーンの新規作成やダイアログ表示をOnGUI内で行うとGUIレイアウトが壊れるため、
                // 描画が終わってから実行する
                EditorApplication.delayCall += Build;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "生成後の使い方\n" +
                "1. プレイモードに入る\n" +
                "2. Hierarchy の CutBlade を選択\n" +
                (_target == CutTarget.Package
                    ? "3. インスペクタで MultiCutBlade を右クリック →「切断」"
                    : "3. インスペクタの MultiCutBlade にある「切断」ボタンを押す"),
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ────────────────────────────── 生成本体 ──────────────────────────────

        private void Build()
        {
            EditorApplication.delayCall -= Build;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                EnsureFolder();

                Material bodyMaterial = CreateMaterial(BodyMaterialPath, new Color(0.55f, 0.7f, 0.9f));
                Material capMaterial = CreateMaterial(CapMaterialPath, new Color(0.9f, 0.35f, 0.3f));
                PhysicsMaterial physicsMaterial = CreatePhysicsMaterial(PhysicsMaterialPath);

                GameObject fragmentPrefab = CreateFragmentPrefab(bodyMaterial, capMaterial, physicsMaterial);

                // 新規シーンを作る(Camera と Directional Light 付き)
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                SetupCamera();
                CreateGround();

                var systemRoot = new GameObject("UsefulMeshCut System");

                Transform cache = CreateCache(systemRoot);

                // 切断対象を先に作る。以降で失敗しても、シーンの主要な中身は残る
                CreateTargets(cache, bodyMaterial, capMaterial, physicsMaterial);

                Component pool = CreatePool(systemRoot, fragmentPrefab);
                CreateBlade(pool);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[MeshCutTest] テストシーンを生成しました ({DescribeTarget()}): {ScenePath}\n" +
                    $"切断対象 {_gridX * _gridZ} 個。プレイモードに入り、CutBlade から切断を実行してください。");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MeshCutTest] テストシーンの生成に失敗しました: {e}");
            }
        }

        private string DescribeTarget()
        {
            return _target == CutTarget.Package ? "UsefulMeshCut パッケージ" : "開発版 Version4";
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(RootFolder));
            }
        }

        // ────────────────────────────── アセット生成 ──────────────────────────────

        private static Material CreateMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = existing != null ? existing : new Material(shader);
            material.shader = shader;

            // URP Lit は _BaseColor、Built-in Standard は _Color
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static PhysicsMaterial CreatePhysicsMaterial(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new PhysicsMaterial("TestFragment")
            {
                dynamicFriction = 0.6f,
                staticFriction = 0.6f,
                bounciness = 0.05f
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private GameObject CreateFragmentPrefab(Material bodyMaterial, Material capMaterial,
            PhysicsMaterial physicsMaterial)
        {
            // 破片プレハブにはコライダーを付けない(CuttableObject が実行時に球コライダーを生成するため)
            var temp = new GameObject("MeshCutFragment");

            var meshFilter = temp.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetPrimitiveMesh(_primitiveType);

            var meshRenderer = temp.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = bodyMaterial;

            var rigidbody = temp.AddComponent<Rigidbody>();

            Component cuttable = _target == CutTarget.Package
                ? temp.AddComponent<Pkg.CuttableObject>()
                : temp.AddComponent<V4.CuttableObject>();

            ApplyCuttableSettings(cuttable, meshFilter, meshRenderer, rigidbody, capMaterial, physicsMaterial);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            DestroyImmediate(temp);

            return prefab;
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);

            return mesh;
        }

        // ────────────────────────────── シーン構築 ──────────────────────────────

        private void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            float distance = Mathf.Max(_gridX, _gridZ) * _spacing + 6f;

            camera.transform.position = new Vector3(0f, _objectHeight + 2f, -distance);
            camera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
        }

        private Transform CreateCache(GameObject systemRoot)
        {
            var cacheObj = new GameObject("MeshDataCache");
            cacheObj.transform.SetParent(systemRoot.transform);

            if (_target == CutTarget.Package)
            {
                cacheObj.AddComponent<Pkg.MeshDataCache>();
            }
            else
            {
                cacheObj.AddComponent<V4.MeshDataCache>();
            }

            return cacheObj.transform;
        }

        private Component CreatePool(GameObject systemRoot, GameObject fragmentPrefab)
        {
            var poolObj = new GameObject("FragmentPool");
            poolObj.transform.SetParent(systemRoot.transform);

            Component pool = _target == CutTarget.Package
                ? poolObj.AddComponent<Pkg.MeshCutObjectPool>()
                : poolObj.AddComponent<V4.MeshCutObjectPool>();

            ConfigureComponent(pool, "MeshCutObjectPool", so =>
            {
                TrySetProperty(so, "_generateCapacity", p => p.intValue = _gridX * _gridZ * 2);
                TrySetProperty(so, "_prefab", p => p.objectReferenceValue = fragmentPrefab);
            });

            return pool;
        }

        private void CreateBlade(Component pool)
        {
            var bladeObj = new GameObject("CutBlade");
            bladeObj.transform.position = new Vector3(0f, _objectHeight, 0f);

            // 切断範囲となる OverlapBox。切断対象を全て覆うだけの広さを持たせる
            var box = bladeObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(_gridX * _spacing + 2f, 0.1f, _gridZ * _spacing + 2f);

            Component blade = _target == CutTarget.Package
                ? bladeObj.AddComponent<Pkg.MultiCutBlade>()
                : bladeObj.AddComponent<V4.MultiCutBlade>();

            bool isPackage = _target == CutTarget.Package;

            ConfigureComponent(blade, "MultiCutBlade", so =>
            {
                TrySetProperty(so, "_pool", p => p.objectReferenceValue = pool);

                // 開発版 Version4 には処理時間ログの切り替えが無い
                if (isPackage)
                {
                    TrySetProperty(so, "_enableProfileLog", p => p.boolValue = _enableProfileLog);
                }
            });
        }

        private void CreateTargets(Transform cache, Material bodyMaterial, Material capMaterial,
            PhysicsMaterial physicsMaterial)
        {
            float offsetX = (_gridX - 1) * _spacing * 0.5f;
            float offsetZ = (_gridZ - 1) * _spacing * 0.5f;

            for (int x = 0; x < _gridX; x++)
            {
                for (int z = 0; z < _gridZ; z++)
                {
                    GameObject obj = GameObject.CreatePrimitive(_primitiveType);
                    obj.name = $"Cuttable_{x}_{z}";

                    obj.transform.SetParent(cache);
                    obj.transform.position = new Vector3(
                        x * _spacing - offsetX,
                        _objectHeight,
                        z * _spacing - offsetZ);

                    var meshRenderer = obj.GetComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = bodyMaterial;

                    // 切断対象は落下させたくないので Rigidbody は付けない。
                    // 破片側にだけ Rigidbody があり、切断された瞬間から落ち始める。
                    Component cuttable = _target == CutTarget.Package
                        ? obj.AddComponent<Pkg.CuttableObject>()
                        : obj.AddComponent<V4.CuttableObject>();

                    ApplyCuttableSettings(cuttable, obj.GetComponent<MeshFilter>(), meshRenderer, null,
                        capMaterial, physicsMaterial);
                }
            }
        }

        private void ApplyCuttableSettings(Component cuttable, MeshFilter meshFilter, Renderer renderer,
            Rigidbody rigidbody, Material capMaterial, PhysicsMaterial physicsMaterial)
        {
            ConfigureComponent(cuttable, "CuttableObject", so =>
            {
                TrySetProperty(so, "Mesh", p => p.objectReferenceValue = meshFilter);
                TrySetProperty(so, "Renderer", p => p.objectReferenceValue = renderer);
                TrySetProperty(so, "Rig", p => p.objectReferenceValue = rigidbody);
                TrySetProperty(so, "CapMaterial", p => p.objectReferenceValue = capMaterial);
                TrySetProperty(so, "_physicsMaterial", p => p.objectReferenceValue = physicsMaterial);
                TrySetProperty(so, "_colliderNum", p => p.intValue = _colliderNum);

                // 開発版 Version4 には複数回切断の設定が無いのでスキップする
                if (_target == CutTarget.Package)
                {
                    TrySetProperty(so, "_canMultiCut", p => p.boolValue = _canMultiCut);
                }
            });
        }

        // ────────────────────────────── 設定の適用 ──────────────────────────────

        /// <summary>
        /// コンポーネントが正しく追加されているかを確認し、SerializedObject を作って設定を適用します。
        /// AddComponent はスクリプトが解決できないと null を返すため、そのまま SerializedObject に渡すと
        /// 原因の分からない NullReferenceException になります。
        /// </summary>
        private static void ConfigureComponent(Component component, string label,
            System.Action<SerializedObject> configure)
        {
            if (component == null)
            {
                Debug.LogError(
                    $"[MeshCutTest] {label} の追加に失敗しました。コンパイルが完了してから再実行してください。");
                return;
            }

            var so = new SerializedObject(component);
            so.Update();
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// SerializedProperty を名前で引いて設定します。
        /// 名前が見つからないときは NullReferenceException ではなく、どのフィールドが原因か分かるログを出します。
        /// </summary>
        private static bool TrySetProperty(SerializedObject so, string propertyName,
            System.Action<SerializedProperty> setter)
        {
            SerializedProperty property = so.FindProperty(propertyName);

            if (property == null)
            {
                string typeName = so.targetObject != null ? so.targetObject.GetType().FullName : "(破棄済みオブジェクト)";

                Debug.LogError(
                    $"[MeshCutTest] {typeName} に SerializeField \"{propertyName}\" が見つかりません。" +
                    "フィールド名が変わっていないか確認してください。");
                return false;
            }

            setter(property);
            return true;
        }
    }
}
