using System;
using System.Collections.Generic;
using System.Linq;
using ScriptedTalk;
using UnityEditor;
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

        List<(GameObject, CuttableObject)> returnObjects = new();
        if (_unusedObjects.Count < objectCount)
        {
            for (int i = 0; i < objectCount - _unusedObjects.Count; i++)
            {
                ReleaseObject(_usedObjects[0]);
            }
        }

        var moveObjects = _unusedObjects.GetRange(0, objectCount);

        returnObjects.AddRange(moveObjects);
        _usedObjects.AddRange(moveObjects);
        _unusedObjects.RemoveRange(0, objectCount);

        return returnObjects;
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