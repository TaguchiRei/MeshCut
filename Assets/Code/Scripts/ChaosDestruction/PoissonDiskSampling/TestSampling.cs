using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Attribute;
using ChaosDestruction.PoissonDiskSampling;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestSampling : MonoBehaviour
{
    [SerializeField] private int _tryCheck = 20;
    [SerializeField] private float _density = 1;
    [SerializeField] private Vector3 _maxPosition;
    [SerializeField] private Vector3 _minPosition;
    [SerializeField] private GameObject _testObject;

    private PoissonDiskSampling _poissonDiskSampling;
    private CancellationTokenSource  _cancellationTokenSource;

    private List<GameObject> _generatedObjects;

    void Start()
    {
        _generatedObjects = new List<GameObject>();
        _poissonDiskSampling = new PoissonDiskSampling(_tryCheck, _density, 1.2f);
    }

    [MethodExecutor("生成", false)]
    private void GenerateSample()
    {
        foreach (var generatedObject in _generatedObjects)
        {
            Destroy(generatedObject);
        }

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Dispose();
        }
        _cancellationTokenSource = new CancellationTokenSource();
        
        GenerateAsync().Forget();
    }

    private async UniTask GenerateAsync()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var samples = await _poissonDiskSampling.SamplingAsync(_maxPosition, _minPosition, _cancellationTokenSource.Token);
        Debug.Log($"{stopwatch.ElapsedMilliseconds}ms");

        foreach (var position in samples)
        {
            _generatedObjects.Add(Instantiate(_testObject, position, Quaternion.identity));
        }
    }
}