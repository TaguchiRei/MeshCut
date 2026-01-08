using Unity.Collections;
using Unity.Mathematics;
using System.Diagnostics;
using UnityEngine;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

public class TestCutScaduler : MonoBehaviour
{
    [SerializeField] private GameObject[] _cutObject;

    private NativeMeshData[] _nativeMeshData;
    private NativePlane _blade;

    public void Start()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        // L3設計: 事前にNativeArrayへキャッシュ済み。Allocator.Persistent。

        _nativeMeshData = new NativeMeshData[_cutObject.Length];

        for (int i = 0; i < _cutObject.Length; i++)
        {
            _nativeMeshData[i] = new NativeMeshData(_cutObject[i].GetComponent<MeshFilter>().mesh);
        }

        _blade = new NativePlane(transform.position, transform.up);
        Debug.Log(stopwatch.ElapsedMilliseconds + "msでメッシュの変換が完了しました");
    }

    [MethodExecutor]
    public void TestMeshCutBurst()
    {
        #region 各種配列統合

        int arrayLength = 0;

        int trianglesLength = 0;
        for (int i = 0; i < _nativeMeshData.Length; i++)
        {
            trianglesLength += _nativeMeshData[i].Triangles.Length;
        }

        NativeEditMeshData editMeshData = new NativeEditMeshData();
        editMeshData.Vertices = new NativeList<float3>(arrayLength, Allocator.Temp);
        editMeshData.Normals = new NativeList<float3>(arrayLength, Allocator.Temp);
        editMeshData.Uvs = new NativeList<float2>(arrayLength, Allocator.Temp);
        editMeshData.Triangles = new NativeList<SubmeshTriangle>(trianglesLength, Allocator.Temp);
        editMeshData.TrianglesStartLengthID = new NativeList<int3>(_nativeMeshData.Length, Allocator.Temp);

        for (int i = 0; i < _nativeMeshData.Length; i++)
        {
            editMeshData.Vertices.AddRange(_nativeMeshData[i].Vertices);
            editMeshData.Normals.AddRange(_nativeMeshData[i].Normals);
            editMeshData.Uvs.AddRange(_nativeMeshData[i].Uvs);
        }

        int start = 0;
        for (int i = 0; i < _nativeMeshData.Length; i++)
        {
            editMeshData.Triangles.AddRange(_nativeMeshData[i].Triangles);
            editMeshData.TrianglesStartLengthID.Add(new(start, _nativeMeshData[i].Triangles.Length, i));
            start += _nativeMeshData[i].Triangles.Length;
        }

        #endregion

        #region MeshDataOffsetJob

        #endregion
    }

    private void OnDestroy()
    {
        foreach (var nativeMeshData in _nativeMeshData)
        {
            if (nativeMeshData.Vertices.IsCreated)
                nativeMeshData.Vertices.Dispose();
        }
    }
}