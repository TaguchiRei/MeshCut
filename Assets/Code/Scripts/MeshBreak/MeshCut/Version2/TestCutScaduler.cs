using System.Diagnostics;
using UnityEngine;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

public class TestCutScaduler : MonoBehaviour
{
    [SerializeField] private int _batchCount = 64;
    [SerializeField] private GameObject[] _cutObject;

    private NativeMeshData[] _nativeMeshData;
    private NativePlane _blade;

    private BurstMeshCutScheduler _scheduler;

    public void Start()
    {
        _scheduler = new BurstMeshCutScheduler();
        Stopwatch stopwatch = Stopwatch.StartNew();
        // L3設計: 事前にNativeArrayへキャッシュ済み。Allocator.Persistent。

        _nativeMeshData = new NativeMeshData[_cutObject.Length];

        for (int i = 0; i < _cutObject.Length; i++)
        {
            _nativeMeshData[i] =
                new NativeMeshData(_cutObject[i].GetComponent<MeshFilter>().mesh, _cutObject[i].transform);
        }

        _blade = new NativePlane(transform.position, transform.up);
        Debug.Log(stopwatch.ElapsedMilliseconds + "msでメッシュの変換が完了しました");
    }

    [MethodExecutor]
    public void TestMeshCutBurst()
    {
        _scheduler.Cut(_blade, _nativeMeshData, _batchCount);
    }

    private void OnDestroy()
    {
        foreach (var nativeMeshData in _nativeMeshData)
        {
            if (nativeMeshData.Vertices.IsCreated)
                nativeMeshData.Dispose();
        }
    }
}