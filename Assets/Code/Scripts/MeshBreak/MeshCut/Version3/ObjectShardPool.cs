using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut
{
    public class ObjectShardPool : MonoBehaviour
    {
        [SerializeField, Tooltip("あらかじめ生成しておくインスタンス数")]
        private int _preCutObjectInstanceNum;

        [SerializeField] private GameObject _cutObjectPrefab;

        private RecycleBuffer<BreakableObject> _recycleBuffer;

        private void Start()
        {
            StartCoroutine(PoolGenerator());
        }

        /// <summary>
        /// オブジェクトプールから切断後オブジェクトを取得する
        /// </summary>
        public (GameObject, bool) GenerateCutObject(
            GameObject baseObject,
            Vector3[] verts,
            Material[] mats,
            List<Vector3> cutFaceCenterPos,
            List<Vector3> oldCutFaces = null)
        {
            if (_recycleBuffer == null)
            {
                Debug.LogError("[ObjectShardPool] RecycleBuffer が初期化されていません。");
                return default;
            }

            BreakableObject cuttable = _recycleBuffer.Get();

            if (cuttable == null)
            {
                Debug.LogError("[ObjectShardPool] RecycleBuffer から取得したオブジェクトが null です。");
                return default;
            }

            SetupBreakableObject(
                cuttable,
                baseObject,
                mats
            );

            bool result = cuttable.ColliderWeightReduction(
                verts,
                cutFaceCenterPos
            );

            return (cuttable.gameObject, result);
        }

        /// <summary>
        /// オブジェクトプールから切断後オブジェクトを取得する
        /// </summary>
        public (GameObject, bool) GenerateCutObject(
            GameObject baseObject,
            List<Vector3> verts,
            Material[] mats,
            List<Vector3> cutFaceCenterPos,
            List<Vector3> oldCutFaces = null)
        {
            if (_recycleBuffer == null)
            {
                Debug.LogError("[ObjectShardPool] RecycleBuffer が初期化されていません。");
                return default;
            }

            BreakableObject cuttable = _recycleBuffer.Get();

            if (cuttable == null)
            {
                Debug.LogError("[ObjectShardPool] RecycleBuffer から取得したオブジェクトが null です。");
                return default;
            }

            SetupBreakableObject(
                cuttable,
                baseObject,
                mats
            );

            bool result = cuttable.ColliderWeightReduction(
                verts,
                cutFaceCenterPos
            );

            return (cuttable.gameObject, result);
        }

        /// <summary>
        /// 使用済みオブジェクトを返却する
        /// </summary>
        public void ReturnToPool(BreakableObject target)
        {
            if (target == null)
            {
                return;
            }

            target.transform.SetParent(transform);
            target.gameObject.SetActive(false);

            _recycleBuffer.Release(target);
        }

        /// <summary>
        /// すべてのオブジェクトをリサイクルする
        /// </summary>
        public void ResetPool()
        {
            _recycleBuffer?.RecycleAll();
        }

        private void SetupBreakableObject(
            BreakableObject breakable,
            GameObject baseObject,
            Material[] mats)
        {
            breakable.gameObject.SetActive(true);

            Transform cuttableTransform = breakable.transform;

            cuttableTransform.position = baseObject.transform.position;
            cuttableTransform.rotation = baseObject.transform.rotation;

            if (breakable.MeshRenderer != null)
            {
                breakable.MeshRenderer.materials = mats;
            }
            else
            {
                Debug.LogWarning(
                    $"[ObjectShardPool] {breakable.name} の MeshRenderer が null です。",
                    breakable
                );
            }
        }

        private IEnumerator PoolGenerator()
        {
            var asyncOperation =
                InstantiateAsync(_cutObjectPrefab, _preCutObjectInstanceNum, transform);

            yield return asyncOperation;

            var result = asyncOperation.Result;

            BreakableObject[] buffer =
                new BreakableObject[_preCutObjectInstanceNum];

            for (int i = 0; i < result.Length; i++)
            {
                GameObject obj = result[i];

                obj.SetActive(false);

                if (!obj.TryGetComponent(out BreakableObject breakableObject))
                {
                    Debug.LogError(
                        $"[ObjectShardPool] {obj.name} に BreakableObject が存在しません。",
                        obj
                    );

                    continue;
                }

                buffer[i] = breakableObject;
            }

            _recycleBuffer = new RecycleBuffer<BreakableObject>(buffer);
        }
    }
}