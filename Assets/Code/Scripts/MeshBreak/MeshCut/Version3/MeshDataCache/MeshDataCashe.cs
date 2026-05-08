using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshDataCache : MonoBehaviour
    {
        public static MeshDataCache Instance { get; private set; }

        /// <summary> Mesh参照 → CachedMeshData </summary>
        private readonly Dictionary<Mesh, CachedMeshData> _cache = new();

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
        public void RegisterStage(MeshRegistry registry)
        {
            _cache.Clear();

            foreach (var mesh in registry.Meshes)
            {
                if (mesh == null) continue;

                if (_cache.ContainsKey(mesh))
                {
                    Debug.LogWarning($"[MeshDataCache] {mesh.name} は既に登録済みです。スキップします。");
                    continue;
                }

                if (mesh.vertexCount > MeshCutBatchRunner.MaxVertexCount)
                {
                    Debug.LogWarning($"[MeshDataCache] {mesh.name} の頂点数({mesh.vertexCount})が上限を超えています。登録をスキップします。");
                    continue;
                }

                _cache[mesh] = new CachedMeshData(mesh);
                Debug.Log($"[MeshDataCache] {mesh.name} を登録しました。頂点数:{mesh.vertexCount}");
            }
        }

        public bool TryGet(Mesh mesh, out CachedMeshData data)
            => _cache.TryGetValue(mesh, out data);

        public void Unload()
        {
            _cache.Clear();
            Debug.Log("[MeshDataCache] キャッシュを解放しました。");
        }
    }
}