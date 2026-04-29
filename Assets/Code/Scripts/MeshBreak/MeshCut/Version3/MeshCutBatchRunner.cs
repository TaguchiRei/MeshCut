using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    /// <summary>
    /// 複数GameObjectを一定個数ごとに分割し、ThreadPoolで並列メッシュ切断する管理クラス
    /// </summary>
    public class MeshCutBatchRunner : MonoBehaviour
    {
        [SerializeField] private MeshCut _meshCut;
        [SerializeField] private Material _capMaterial;

        [Header("1スレッドあたりの処理数")]
        [SerializeField] private int _batchSize = 4;

        [Header("最大同時実行数")]
        [SerializeField] private int _maxParallel = 4;

        /// <summary>
        /// 複数オブジェクトをまとめて切断
        /// </summary>
        public async UniTask<List<GameObject[]>> CutObjectsAsync(
            List<GameObject> targets,
            Plane blade)
        {
            if (targets == null || targets.Count == 0)
                return new List<GameObject[]>();

            List<List<GameObject>> batches = SplitBatch(targets, _batchSize);

            List<UniTask<List<GameObject[]>>> runningTasks = new();

            int index = 0;

            while (index < batches.Count)
            {
                runningTasks.Clear();

                for (int i = 0; i < _maxParallel && index < batches.Count; i++, index++)
                {
                    var batch = batches[index];
                    runningTasks.Add(ProcessBatch(batch, blade));
                }

                await UniTask.WhenAll(runningTasks);
            }

            List<GameObject[]> results = new();

            foreach (var batch in batches)
            {
                foreach (var obj in batch)
                {
                    if (obj == null) continue;
                }
            }

            return results;
        }

        /// <summary>
        /// バッチ単位処理
        /// </summary>
        private async UniTask<List<GameObject[]>> ProcessBatch(
            List<GameObject> batch,
            Plane blade)
        {
            // ThreadPoolで純計算部分
            var meshResults = await UniTask.RunOnThreadPool(() =>
            {
                List<CutRequest> requests = new();

                foreach (var target in batch)
                {
                    if (target == null) continue;

                    var mf = target.GetComponent<MeshFilter>();
                    if (mf == null) continue;

                    requests.Add(new CutRequest
                    {
                        Target = target,
                        Blade = blade
                    });
                }

                return requests;
            });

            // Unity APIはメインスレッド
            await UniTask.SwitchToMainThread();

            List<GameObject[]> results = new();

            foreach (var request in meshResults)
            {
                if (request.Target == null) continue;

                var cut = _meshCut.Cut(
                    request.Target,
                    request.Blade,
                    _capMaterial);

                results.Add(cut);
            }

            return results;
        }

        /// <summary>
        /// リストを一定個数で分割
        /// </summary>
        private List<List<GameObject>> SplitBatch(
            List<GameObject> source,
            int batchSize)
        {
            List<List<GameObject>> result = new();

            for (int i = 0; i < source.Count; i += batchSize)
            {
                result.Add(source.Skip(i).Take(batchSize).ToList());
            }

            return result;
        }

        private struct CutRequest
        {
            public GameObject Target;
            public Plane Blade;
        }
    }
}