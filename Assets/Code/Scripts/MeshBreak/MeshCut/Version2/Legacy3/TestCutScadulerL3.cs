using System.Diagnostics;
using UnityEngine;
using UsefulAttribute;
using Debug = UnityEngine.Debug;

public class TestCutScadulerL3 : MonoBehaviour
{
    [SerializeField] private int _batchCount = 64;
    [SerializeField] private GameObject[] _cutObject;

    private NativeMeshDataL3[] _nativeMeshData;
    private NativePlane _blade;

    private BurstMeshCutSchedulerL3 _schedulerL3;

    public void Start()
    {
        _schedulerL3 = new BurstMeshCutSchedulerL3();
        Stopwatch stopwatch = Stopwatch.StartNew();
        // L3設計: 事前にNativeArrayへキャッシュ済み。Allocator.Persistent。

        _nativeMeshData = new NativeMeshDataL3[_cutObject.Length];

        for (int i = 0; i < _cutObject.Length; i++)
        {
            _nativeMeshData[i] =
                new NativeMeshDataL3(_cutObject[i].GetComponent<MeshFilter>().mesh, _cutObject[i].transform);
        }

        _blade = new NativePlane(transform.position, transform.up);
        Debug.Log(stopwatch.ElapsedMilliseconds + "msでメッシュの変換が完了しました");
    }

    [MethodExecutor]
    public void TestMeshCutBurst()
    {
        _schedulerL3.Cut(_blade, _nativeMeshData, _batchCount);
    }

    private void OnDestroy()
    {
        if (!enabled) return;
        foreach (var nativeMeshData in _nativeMeshData)
        {
            if (nativeMeshData.Vertices.IsCreated)
                nativeMeshData.Dispose();
        }
    }
}