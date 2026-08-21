using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MeshBreak.MeshCut.Version4
{
    /// <summary>
    /// V2のMeshDataCacheと同じ役割(シーン内のCuttableObjectが参照するメッシュをユニーク登録し、
    /// 各CuttableObjectにMeshIdを割り振る)を担うが、実データはNativeMeshDataStoreとして
    /// NativeArrayにフラット化して保持する。
    /// </summary>
    public class MeshDataCache : MonoBehaviour
    {
        public static MeshDataCache Instance { get; private set; }

        public NativeMeshDataStore Store { get; private set; }

        private void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
        }

        public void Initialize()
        {
            Store?.Dispose();
            Store = new NativeMeshDataStore();

            var objects = GetComponentsInChildren<CuttableObject>();
            List<Mesh> registeredMeshes = new();

            foreach (var cuttable in objects)
            {
                var mesh = cuttable.Mesh.sharedMesh;
                if (mesh == null) continue;

                int index = registeredMeshes.IndexOf(mesh);

                if (index == -1)
                {
                    registeredMeshes.Add(mesh);
                    Store.Add(mesh);
                    cuttable.MeshId = registeredMeshes.Count - 1;
                }
                else
                {
                    cuttable.MeshId = index;
                }
            }

            Debug.Log($"[MeshDataCache V4] Cache Completed. Cache Count: {Store.MeshCount}");
        }

        /// <summary> 指定メッシュIDの頂点範囲・三角形範囲・元サブメッシュ数を取得する </summary>
        public bool TryGet(int meshId, out int2 vertexRange, out int2 triangleRange, out int submeshCount)
        {
            if (Store == null || meshId < 0 || meshId >= Store.MeshCount)
            {
                Debug.LogError($"[MeshDataCache V4] IDの値が不正です {meshId}");
                vertexRange = default;
                triangleRange = default;
                submeshCount = 0;
                return false;
            }

            vertexRange = Store.MeshVertexRange[meshId];
            triangleRange = Store.MeshTriangleRange[meshId];
            submeshCount = Store.MeshSubmeshCount[meshId];
            return true;
        }

        public void Unload()
        {
            Store?.Dispose();
            Store = null;
            Debug.Log("[MeshDataCache V4] キャッシュを解放しました。");
        }

        private void OnDestroy()
        {
            Store?.Dispose();
        }
    }
}
