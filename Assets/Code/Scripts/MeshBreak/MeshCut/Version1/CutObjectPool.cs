using System.Collections;
using System.Collections.Generic;
using MeshBreak;
using UnityEngine;

public class CutObjectPool : MonoBehaviour
{
    [SerializeField, Tooltip("あらかじめ生成しておくインスタンス数")]
    private int _preCutObjectInstanceNum;

    [SerializeField] private GameObject _cutObjectPrefab;

    private List<GameObject> _preCutPool = new();
    private Dictionary<int, GameObject> _postCutPool = new();

    private void Start()
    {
        StartCoroutine(PoolGenerator());
    }

    /// <summary>
    /// オブジェクトプールから切断後のオブジェクトを生成する
    /// </summary>
    /// <param name="baseObject"></param>
    /// <param name="verts"></param>
    /// <param name="mats"></param>
    /// <param name="cutFaceCenterPos"></param>
    /// <param name="oldCutFaces"></param>
    /// <returns></returns>
    public (GameObject, bool) GenerateCutObject(
        GameObject baseObject, List<Vector3> verts, Material[] mats,
        List<Vector3> cutFaceCenterPos,
        List<Vector3> oldCutFaces = null)
    {
        if (_preCutPool.Count > 0)
        {
            var cuttable = _preCutPool[0].GetComponent<BreakableObject>();
            _preCutPool.RemoveAt(0);
            cuttable.SetParentHash(baseObject.GetInstanceID());
            cuttable.transform.position = baseObject.transform.position;
            cuttable.transform.rotation = baseObject.transform.rotation;
            cuttable.MeshRenderer.materials = mats;
            var result = cuttable.ColliderWeightReduction(verts, cutFaceCenterPos);
            _postCutPool[cuttable.GetInstanceID()] = cuttable.gameObject;
            return (cuttable.gameObject, result);
        }

        return default;
    }

    public void ResetPostPool()
    {
        _postCutPool.Clear();
    }

    private IEnumerator PoolGenerator()
    {
        if (_preCutPool.Count < _preCutObjectInstanceNum)
        {
            var asyncOperation = InstantiateAsync(
                _cutObjectPrefab, _preCutObjectInstanceNum - _preCutPool.Count, gameObject.transform);
            yield return asyncOperation;
            var result = asyncOperation.Result;
            _preCutPool.AddRange(result);
        }
    }
}