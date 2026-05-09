using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshDataCache : MonoBehaviour
    {
        public static MeshDataCache Instance { get; private set; }

        List<CachedMeshData> _cache = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// ステージ開始時にMeshRegistryを渡して登録する
        /// </summary>
        public void Initialize()
        {
            var objects = GetComponentsInChildren<BreakableObject>();
            List<Mesh> meshes = new();

            foreach (var breakable in objects)
            {
                var mesh = breakable.MeshFilter.sharedMesh;
                if (!meshes.Contains(mesh))
                {
                    meshes.Add(mesh);
                    _cache.Add(new CachedMeshData(mesh));
                    breakable.MeshId = _cache.Count;
                }
                else
                {
                    breakable.MeshId = meshes.FindIndex(x => x == mesh);
                }
            }
        }

        public void Get(int meshId, out CachedMeshData data)
        {
            if (_cache.Count <= meshId || meshId < 0)
            {
                Debug.LogError("[MeshDataCache]IDの値が不正です");
                data = null;
            }
            else
            {
                data = _cache[meshId];
            }
        }

        public void Unload()
        {
            _cache.Clear();
            Debug.Log("[MeshDataCache] キャッシュを解放しました。");
        }
    }
}