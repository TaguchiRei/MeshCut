using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CutPlaneCollider : MonoBehaviour
{
    [SerializeField] private RectTransform lineRect;
    private readonly List<GameObject> _targetObject = new();
    private BoxCollider _boxCollider;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        transform.localRotation = lineRect.rotation;
    }

    public CuttableObject[] GetObjects()
    {
        Vector3 worldCenter = transform.TransformPoint(_boxCollider.center);

        Vector3 worldHalfExtents = Vector3.Scale(_boxCollider.size * 0.5f, transform.lossyScale);
        var objects = Physics.OverlapBox(worldCenter, worldHalfExtents, transform.rotation);
        List<CuttableObject> cuttables = new();

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].TryGetComponent<CuttableObject>(out var cuttable))
            {
                cuttables.Add(cuttable);
            }
        }

        return cuttables.ToArray();
    }
}