using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MeshBreak.MeshCut.Version3;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public class CutManager : MonoBehaviour
    {
        // ── インスペクター設定 ─────────────────────────────────────
        [SerializeField] private MeshCut  _meshCut;
        [SerializeField] private Material _capMaterial;

        [SerializeField, Tooltip("1スレッドあたりが担当する切断数")]
        private int _chunkSize = 20;

        // ── 内部状態 ───────────────────────────────────────────────
        private readonly Queue<CutRequest> _pendingQueue  = new();
        private readonly HashSet<int>      _processingIds = new();
        private bool _isBatchRunning = false;

        // ── 公開 API ───────────────────────────────────────────────
        /// <summary>
        /// 切断リクエストを登録する。
        /// メインスレッドから呼ぶこと（Unity API を内部で使用）。
        /// </summary>
        public void RequestCut(GameObject target, Plane blade)
        {
            int id = target.GetInstanceID();
            if (_processingIds.Contains(id))
                return;

            MeshCutInput input;
            Material[] mats;
            try
            {
                input = MeshCut.CollectInputOnMainThread(target, blade);
                mats  = target.GetComponent<MeshRenderer>().materials;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CutManager] データ収集失敗 ({target.name}): {e.Message}");
                return;
            }

            _processingIds.Add(id);
            _pendingQueue.Enqueue(new CutRequest(target, blade, mats, input));
        }

        /// <summary>
        /// キューにたまっている全リクエストを一気に処理する。
        /// 処理中に呼んでも多重起動しない。
        /// </summary>
        public void Flush()
        {
            if (_pendingQueue.Count == 0 || _isBatchRunning)
                return;

            // UniTask では Forget() で fire-and-forget
            // GetCancellationTokenOnDestroy() で MonoBehaviour 破棄時に自動キャンセル
            ProcessAllAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        // ── 全件並列処理 ──────────────────────────────────────────
        private async UniTaskVoid ProcessAllAsync(System.Threading.CancellationToken ct)
        {
            _isBatchRunning = true;

            // ① キューを全件スナップショット（メインスレッド）
            var allRequests = new List<CutRequest>(_pendingQueue.Count);
            while (_pendingQueue.Count > 0)
                allRequests.Add(_pendingQueue.Dequeue());

            int total     = allRequests.Count;
            int chunkSize = Mathf.Max(1, _chunkSize);
            int chunkCount = (total + chunkSize - 1) / chunkSize;

            // ② チャンクごとに UniTask を生成（ワーカースレッドで計算）
            var tasks = new UniTask<List<(CutRequest req, MeshCutResult result)>>[chunkCount];

            for (int c = 0; c < chunkCount; c++)
            {
                int start = c * chunkSize;
                int end   = Mathf.Min(start + chunkSize, total);
                var chunk = allRequests.GetRange(start, end - start);

                tasks[c] = UniTask.RunOnThreadPool(() =>
                {
                    var chunkResults = new List<(CutRequest, MeshCutResult)>(chunk.Count);
                    foreach (var req in chunk)
                    {
                        ct.ThrowIfCancellationRequested();
                        var result = MeshCut.Calculate(req.Input);
                        chunkResults.Add((req, result));
                    }
                    return chunkResults;
                }, cancellationToken: ct);
            }

            // ③ 全スレッドの完了を待つ
            List<(CutRequest req, MeshCutResult result)>[] chunkResultArrays;
            try
            {
                chunkResultArrays = await UniTask.WhenAll(tasks);
            }
            catch (System.OperationCanceledException)
            {
                // MonoBehaviour 破棄による正常キャンセル
                _isBatchRunning = false;
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CutManager] 並列計算中にエラー: {e.Message}");
                foreach (var req in allRequests)
                    _processingIds.Remove(req.Target.GetInstanceID());
                _isBatchRunning = false;
                return;
            }

            // ④ await 後は自動的にメインスレッドへ戻る（UniTask のデフォルト動作）
            //    全結果を一括適用
            foreach (var chunkResults in chunkResultArrays)
            {
                foreach (var (req, result) in chunkResults)
                {
                    if (req.Target == null)
                    {
                        _processingIds.Remove(req.Target.GetInstanceID());
                        continue;
                    }

                    try
                    {
                        _meshCut.ApplyResultOnMainThread(
                            req.Target, result, req.OriginalMaterials, _capMaterial);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[CutManager] 結果適用失敗 ({req.Target.name}): {e.Message}");
                    }
                    finally
                    {
                        _processingIds.Remove(req.Target.GetInstanceID());
                    }
                }
            }

            _isBatchRunning = false;
        }
    }
}