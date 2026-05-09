using UnityEditor;
using UnityEngine;
using MeshBreak.MeshCut.Version2;

namespace MeshBreak.MeshCut.Editor
{
    public class MeshCutV2SetupEditor : EditorWindow
    {
        private GameObject _cuttablePrefab;
        private int _poolCapacity = 20;

        [MenuItem("Window/MeshCut/Version2 Setup")]
        public static void ShowWindow()
        {
            GetWindow<MeshCutV2SetupEditor>("MeshCut V2 Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("MeshCut Version 2 Setup Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _cuttablePrefab = (GameObject)EditorGUILayout.ObjectField("Cuttable Prefab", _cuttablePrefab, typeof(GameObject), false);
            _poolCapacity = EditorGUILayout.IntField("Pool Capacity", _poolCapacity);

            EditorGUILayout.Space();

            if (GUILayout.Button("Setup Scene Essentials"))
            {
                SetupScene();
            }

            if (GUILayout.Button("Add CuttableObject to Selected"))
            {
                AddCuttableToSelected();
            }
        }

        private void SetupScene()
        {
            // 1. ルート管理オブジェクトの作成
            GameObject systemRoot = GameObject.Find("MeshCutSystem_V2");
            if (systemRoot == null)
            {
                systemRoot = new GameObject("MeshCutSystem_V2");
            }

            // 2. Cache オブジェクトの作成 (これから切るオブジェクトをこの下に置く)
            GameObject cacheObj = GameObject.Find("MeshCutCache_V2");
            if (cacheObj == null)
            {
                cacheObj = new GameObject("MeshCutCache_V2");
                cacheObj.transform.SetParent(systemRoot.transform);
            }

            var cache = cacheObj.GetComponent<MeshDataCache>();
            if (cache == null)
            {
                cache = cacheObj.AddComponent<MeshDataCache>();
            }

            // 3. Pool オブジェクトの作成 (生成された破片がこの下に溜まる)
            GameObject poolObj = GameObject.Find("MeshCutPool_V2");
            if (poolObj == null)
            {
                poolObj = new GameObject("MeshCutPool_V2");
                poolObj.transform.SetParent(systemRoot.transform);
            }

            var pool = poolObj.GetComponent<MeshCutObjectPool>();
            if (pool == null)
            {
                pool = poolObj.AddComponent<MeshCutObjectPool>();
            }

            // Pool の設定
            var poolSerialized = new SerializedObject(pool);
            poolSerialized.FindProperty("_generateCapacity").intValue = _poolCapacity;
            if (_cuttablePrefab != null)
            {
                poolSerialized.FindProperty("_prefab").objectReferenceValue = _cuttablePrefab;
            }
            poolSerialized.ApplyModifiedProperties();

            // 4. Blade の作成
            GameObject blade = GameObject.Find("MeshCutBlade_V2");
            if (blade == null)
            {
                blade = new GameObject("MeshCutBlade_V2");
                blade.transform.position = new Vector3(0, 2, 0);
            }

            var box = blade.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = blade.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(5, 0.1f, 5);
            }

            var multiBlade = blade.GetComponent<MultiCutBlade>();
            if (multiBlade == null)
            {
                multiBlade = blade.AddComponent<MultiCutBlade>();
            }

            // Blade に Pool を紐付け
            var bladeSerialized = new SerializedObject(multiBlade);
            bladeSerialized.FindProperty("_pool").objectReferenceValue = pool;
            bladeSerialized.ApplyModifiedProperties();

            Selection.activeGameObject = systemRoot;
            Debug.Log("[MeshCut V2] Scene setup completed.");
        }

        private void AddCuttableToSelected()
        {
            int count = 0;
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.GetComponent<CuttableObject>() == null)
                {
                    obj.AddComponent<CuttableObject>();
                    count++;
                }
            }
            Debug.Log($"[MeshCut V2] Added CuttableObject to {count} objects.");
        }
    }
}