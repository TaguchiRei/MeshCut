using System.Collections.Generic;
using ScriptedTalk;
using UnityEngine;

public class MeshCutObjectPool : MonoBehaviour
{
    [ShowOnly] public bool IsGenerated { get; private set; }

    [SerializeField] private int _generateCapacity;
    [SerializeField] private GameObject _prefab;

    private List<(GameObject gameObject, CuttableObject cuttable)> _unusedObjects;
    private List<(GameObject gameObject, CuttableObject cuttable)> _usedObjects;

    private async void Start()
    {
        IsGenerated = false;
        _unusedObjects = new();
        _usedObjects = new();
        var objects = await InstantiateAsync(_prefab, _generateCapacity, transform);
        foreach (var obj in objects)
        {
            obj.SetActive(false);
            _unusedObjects.Add((obj, obj.GetComponent<CuttableObject>()));
        }

        IsGenerated = true;
    }

    public List<(GameObject, CuttableObject)> GetObjects(int objectCount)
    {
        if (objectCount > _generateCapacity)
        {
            Debug.LogWarning("オブジェクトの要求量が生成数を超えています");
            objectCount = _generateCapacity;
        }

        // 不足分を使用中のオブジェクトから回収
        if (_unusedObjects.Count < objectCount)
        {
            int shortCount = objectCount - _unusedObjects.Count;

            // 必要な数だけ先頭(古いもの)から抜き出す
            var reusable = _usedObjects.GetRange(0, shortCount);
            _usedObjects.RemoveRange(0, shortCount);

            // 回収アクションの実行
            foreach (var item in reusable)
            {
                item.cuttable.ReuseAction?.Invoke();
                item.gameObject.SetActive(false);
            }

            _unusedObjects.AddRange(reusable);
        }

        // 取得
        var results = _unusedObjects.GetRange(0, objectCount);
        _unusedObjects.RemoveRange(0, objectCount);
        _usedObjects.AddRange(results);

        return results;
    }

    public void ReleaseObject((GameObject, CuttableObject) releaseObject)
    {
        if (_usedObjects.Contains(releaseObject))
        {
            releaseObject.Item2.ReuseAction?.Invoke();
            releaseObject.Item1.gameObject.SetActive(false);
            _usedObjects.Remove(releaseObject);
            _unusedObjects.Add(releaseObject);
        }
        else
        {
            Debug.LogWarning("このオブジェクトはすでに返還済みか、オブジェクトプールに登録されていません");
        }
    }
}