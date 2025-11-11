using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Version2
{
    public class CutObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _cutObjectPrefab;
        [SerializeField, Tooltip("あらかじめ生成しておくインスタンス数")]
        
        private int _preCutObjectInstanceNum;
        private List<CuttableObject> _preCutPool = new();
        private List<CuttableObject> _postCutPool = new();
        
        
        
        //初めにオブジェクトプール作成
        private IEnumerator GeneratePool()
        {
            if (_preCutPool.Count < _preCutObjectInstanceNum)
            {
                var asyncOperation = InstantiateAsync(
                    _cutObjectPrefab, _preCutObjectInstanceNum - _preCutPool.Count, gameObject.transform);
                yield return asyncOperation;
                var resultObjects = asyncOperation.Result;
                foreach (var resultObject in resultObjects)
                {
                    _preCutPool.Add(resultObject.GetComponent<CuttableObject>());
                }
            }
        }
    }
}
