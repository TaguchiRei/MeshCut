using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshDataCache : MonoBehaviour
    {
        public static MeshDataCache Instance { get; private set; }

        List<CachedMeshData> _cache = new();

        private void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
            Debug.Log($"[MeshDataCache] cache Completed.  Object Count{_cache.Count}]");
        }

        /// <summary>
        /// ステージ開始時にMeshRegistryを渡して登録する
        /// </summary>
        public void Initialize()
        {
            // 初期化時に一度キャッシュをクリアしておくと安全
            _cache.Clear();
    
            var objects = GetComponentsInChildren<BreakableObject>();
            List<Mesh> registeredMeshes = new();

            foreach (var breakable in objects)
            {
                var mesh = breakable.MeshFilter.sharedMesh;
                if (mesh == null) continue;

                int index = registeredMeshes.IndexOf(mesh);

                if (index == -1) 
                {
                    // 未登録なら新しく追加
                    registeredMeshes.Add(mesh);
                    _cache.Add(new CachedMeshData(mesh));
            
                    // 追加した直後の「末尾のインデックス」をIDにする
                    // Countが1ならIDは0、Countが2ならIDは1になる
                    breakable.MeshId = registeredMeshes.Count - 1;
                }
                else 
                {
                    // 登録済みならそのインデックスをそのままIDにする
                    breakable.MeshId = index;
                }
            }
    
            Debug.Log($"[MeshDataCache] cache Completed. Cache Count: {_cache.Count}");
        }

        public void Get(int meshId, out CachedMeshData data)
        {
            if (_cache.Count <= meshId || meshId < 0)
            {
                Debug.LogError($"[MeshDataCache]IDの値が不正です {meshId}");
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