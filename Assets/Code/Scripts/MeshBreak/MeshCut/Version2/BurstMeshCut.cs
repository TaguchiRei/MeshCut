using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

public class BurstMeshCut : MonoBehaviour
{
    [SerializeField] private GameObject _cutObj;

    [MethodExecutor]
    public void Cut()
    {
        Stopwatch allTime = new Stopwatch();
        allTime.Start();

        #region 必要配列やリストを初期化する

        var mesh = _cutObj.GetComponent<MeshFilter>().mesh;
        NativeArray<float3> verts = new(mesh.vertices.Length, Allocator.TempJob);
        NativeArray<float3> normals = new(mesh.normals.Length, Allocator.TempJob);
        NativeArray<float2> uvs = new(mesh.uv.Length, Allocator.TempJob);
        NativeParallelMultiHashMap<int, int3> subIndices = new(mesh.vertices.Length * mesh.subMeshCount,
            Allocator.TempJob);
        MeshDataSupport.ReadMeshDataSafely(mesh, verts, normals, uvs, subIndices);
        float3 pos = new(gameObject.transform.position.x, gameObject.transform.position.y,
            gameObject.transform.position.z);
        float3 normal = new(gameObject.transform.up.x, gameObject.transform.up.y, gameObject.transform.up.z);

        #endregion

        #region 処理タイマー初期化

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        #endregion

        #region 調べる処理

        NativeArray<bool> result = new NativeArray<bool>(verts.Length, Allocator.TempJob);

        stopwatch.Stop();

        #endregion

        #region 結果出力

        Debug.Log($"完了 処理時間{stopwatch.ElapsedMilliseconds}ms");

        #endregion

        #region 必要配列やリストを破棄する

        verts.Dispose();
        normals.Dispose();
        uvs.Dispose();
        subIndices.Dispose();
        result.Dispose();

        #endregion

        allTime.Stop();
        Debug.Log($"全処理所要時間{allTime.ElapsedMilliseconds}ms");
    }

    [MethodExecutor("JobTest", false)]
    public void Test()
    {
        TestAsync().Forget();
    }

    public async UniTask TestAsync()
    {
        Stopwatch allTime = new Stopwatch();
        allTime.Start();

        #region 必要配列やリストを初期化する

        var mesh = _cutObj.GetComponent<MeshFilter>().mesh;
        var blade = new NativePlane(transform.position, transform.up);
        NativeArray<float3> verts = new(mesh.vertices.Length, Allocator.TempJob);
        NativeArray<float3> normals = new(mesh.normals.Length, Allocator.TempJob);
        NativeArray<float2> uvs = new(mesh.uv.Length, Allocator.TempJob);
        NativeParallelMultiHashMap<int, int3> subIndices = new(mesh.vertices.Length * mesh.subMeshCount,
            Allocator.TempJob);
        MeshDataSupport.ReadMeshDataSafely(mesh, verts, normals, uvs, subIndices);
        float3 pos = new(gameObject.transform.position.x, gameObject.transform.position.y,
            gameObject.transform.position.z);
        float3 normal = new(gameObject.transform.up.x, gameObject.transform.up.y, gameObject.transform.up.z);

        #endregion

        #region 処理タイマー初期化

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        #endregion

        #region 調べる処理

        NativeArray<int> result = new(verts.Length, Allocator.TempJob);
        var job = new BurstGetSide
        {
            Vertices = verts,
            Blade = blade,
            VertsSide = result,
        };

        var jobHandle = job.Schedule(verts.Length, 64);

        await UniTask.WaitUntil(() => jobHandle.IsCompleted);
        jobHandle.Complete();

        Debug.Log($"TaskComplete  FirstResult:{result[0]}");

        stopwatch.Stop();

        #endregion

        #region 結果出力

        Debug.Log($"完了 処理時間{stopwatch.ElapsedMilliseconds}ms");

        #endregion

        #region 必要配列やリストを破棄する

        verts.Dispose();
        normals.Dispose();
        uvs.Dispose();
        subIndices.Dispose();
        result.Dispose();

        #endregion

        allTime.Stop();
        Debug.Log($"全処理所要時間{allTime.ElapsedMilliseconds}ms");
    }
}

[BurstCompile]
public struct BurstGetSide : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Vertices;
    [ReadOnly] public NativePlane Blade;

    public NativeArray<int> VertsSide;

    public void Execute(int index)
    {
        VertsSide[index] = Blade.GetSide(Vertices[index]);
    }
}