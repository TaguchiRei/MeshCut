using System;
using System.Collections.Generic;
using UnityEngine;

public class PoissonDiskSampling : MonoBehaviour
{
    [SerializeField] private float _radius;
    private List<Vector3> _rangeVerts;
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private float scale = 1.0f;
    [SerializeField] private int _tryCheck = 18;
    [SerializeField] private int _maxGenerate = 1000;

    private void Start()
    {
        _rangeVerts = new();
        float xMax = 20f;
        float xMin = 0f;
        float yMax = 20f;
        float yMin = 0f;
        float zMax = 20f;
        float zMin = 0f;
        _rangeVerts.Add(new Vector3(xMin, yMax, zMax));
        _rangeVerts.Add(new Vector3(xMax, yMax, zMax));
        _rangeVerts.Add(new Vector3(xMin, yMax, zMin));
        _rangeVerts.Add(new Vector3(xMax, yMax, zMin));
        _rangeVerts.Add(new Vector3(xMin, yMin, zMax));
        _rangeVerts.Add(new Vector3(xMax, yMin, zMax));
        _rangeVerts.Add(new Vector3(xMin, yMin, zMin));
        _rangeVerts.Add(new Vector3(xMax, yMin, zMin));

        SamplingVector3(_radius);
    }

    private void SamplingVector3(float radius)
    {
        List<Vector3> generatedVerts = new List<Vector3>();
    }
}