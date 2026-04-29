using System.Collections;
using System.Collections.Generic;
using Code.Scripts.Utility;
using UnityEngine;

namespace MeshBreak.MeshCut
{
    public class ObjectShardPool : MonoBehaviour
    {
        [SerializeField, Tooltip("あらかじめ生成しておくインスタンス数")]
        private int _preCutObjectInstanceNum;

        [SerializeField] private GameObject _cutObjectPrefab;

        private RingBuffer<GameObject> _preCutPool;

        private readonly Dictionary<int, GameObject> _postCutPool = new();

        private void Awake()
        {
            _preCutPool = new RingBuffer<GameObject>(_preCutObjectInstanceNum);
        }

        private void Start()
        {
            StartCoroutine(PoolGenerator());
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
            if (_preCutPool.Count > 0)
            {
                var pooledObject = _preCutPool.Dequeue();

                var cuttable = pooledObject.GetComponent<BreakableObjectL>();

                cuttable.SetParentHash(baseObject.GetInstanceID());
                cuttable.transform.position = baseObject.transform.position;
                cuttable.transform.rotation = baseObject.transform.rotation;
                cuttable.MeshRenderer.materials = mats;

                bool result = cuttable.ColliderWeightReduction(
                    verts,
                    cutFaceCenterPos
                );

                _postCutPool[cuttable.GetInstanceID()] = cuttable.gameObject;

                return (cuttable.gameObject, result);
            }

            return default;
        }

        /// <summary>
        /// 使用済みオブジェクトを事前プールへ戻す
        /// </summary>
        public void ReturnToPool(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            int id = target.GetInstanceID();

            if (_postCutPool.Remove(id))
            {
                target.transform.SetParent(transform);
                target.SetActive(false);

                _preCutPool.Enqueue(target);
            }
        }

        public void ResetPostPool()
        {
            _postCutPool.Clear();
        }

        private IEnumerator PoolGenerator()
        {
            int createCount = _preCutObjectInstanceNum - _preCutPool.Count;

            if (createCount <= 0)
            {
                yield break;
            }

            var asyncOperation =
                InstantiateAsync(_cutObjectPrefab, createCount, transform);

            yield return asyncOperation;

            var result = asyncOperation.Result;

            foreach (var obj in result)
            {
                obj.SetActive(false);
                _preCutPool.Enqueue(obj);
            }
        }
    }
}