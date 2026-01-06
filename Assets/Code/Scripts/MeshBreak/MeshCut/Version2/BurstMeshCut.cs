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
    [SerializeField] private int _quantizationPrecision = 10000;
    [SerializeField] private int _lnnerLoopBatchCount = 64;

    [MethodExecutor("DirectCallTest", false)]
    public void DirectCallTest()
    {
        Stopwatch allTime = new Stopwatch();
        allTime.Start();

        #region 必要配列やリストを初期化する

        var mesh = _cutObj.GetComponent<MeshFilter>().mesh;
        var blade = new NativePlane(transform.position, transform.up);
        NativeMeshData baseMesh = new NativeMeshData(mesh.vertexCount, mesh.subMeshCount);
        NativeMeshDataUtility.ReadMeshDataSafely(
            mesh, baseMesh.Vertices, baseMesh.Normals, baseMesh.Uvs, baseMesh.SubMesh);
        float3 pos = new(gameObject.transform.position.x, gameObject.transform.position.y,
            gameObject.transform.position.z);
        float3 normal = new(gameObject.transform.up.x, gameObject.transform.up.y, gameObject.transform.up.z);

        #endregion

        #region 調べる処理

        Stopwatch individualStopwatch = new Stopwatch();
        individualStopwatch.Start();
        NativeArray<int> vertSide = new(baseMesh.Vertices.Length, Allocator.Persistent);
        BurstGetSide.CalculateDirect(baseMesh.Vertices, blade, ref vertSide);

        Debug.Log($"頂点左右分け所要時間{individualStopwatch.ElapsedMilliseconds}ms {vertSide[0]}");

        individualStopwatch.Restart();

        int triangleCount = baseMesh.SubMesh.Length;
        NativeMeshDataParallel frontSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.Persistent);
        NativeMeshDataParallel backSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.Persistent);
        NativeList<NativeTriangleDetailData> overlapFront =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.Persistent);
        NativeList<NativeTriangleDetailData> overlapBack =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.Persistent);


        BurstGetFaceDirection.CalculateFaceDirectionDirect(
            baseMesh, vertSide, _quantizationPrecision,
            frontSideMesh.GetParallelWriter(), backSideMesh.GetParallelWriter(),
            overlapFront.AsParallelWriter(), overlapBack.AsParallelWriter());

        Debug.Log($"面左右分け所要時間{individualStopwatch.ElapsedMilliseconds}ms \n overlapFront{overlapFront.Length}");

        #endregion

        #region 必要配列やリストを破棄する

        frontSideMesh.Dispose();
        backSideMesh.Dispose();
        overlapFront.Dispose();
        overlapBack.Dispose();
        baseMesh.Dispose();
        vertSide.Dispose();

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
        NativeMeshData baseMesh = new NativeMeshData(mesh.vertexCount, mesh.subMeshCount);
        NativeMeshDataUtility.ReadMeshDataSafely(
            mesh, baseMesh.Vertices, baseMesh.Normals, baseMesh.Uvs, baseMesh.SubMesh);
        float3 pos = new(gameObject.transform.position.x, gameObject.transform.position.y,
            gameObject.transform.position.z);
        float3 normal = new(gameObject.transform.up.x, gameObject.transform.up.y, gameObject.transform.up.z);

        #endregion

        #region 調べる処理

        Stopwatch individualStopwatch = new Stopwatch();
        individualStopwatch.Start();

        NativeArray<int> result = new(baseMesh.Vertices.Length, Allocator.Persistent);
        var getSideJob = new BurstGetSide
        {
            Vertices = baseMesh.Vertices,
            Blade = blade,
            VertsSide = result,
        };

        var jobHandle = getSideJob.Schedule(baseMesh.Vertices.Length, _lnnerLoopBatchCount);

        await UniTask.WaitUntil(() => jobHandle.IsCompleted);
        jobHandle.Complete();

        Debug.Log($"頂点左右分け所要時間 {individualStopwatch.ElapsedMilliseconds}ms FirstResult:{result[0]}");

        individualStopwatch.Restart();

        int triangleCount = baseMesh.SubMesh.Length;
        NativeMeshDataParallel frontSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.Persistent);
        NativeMeshDataParallel backSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.Persistent);
        NativeList<NativeTriangleDetailData> overlapFront =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.Persistent);
        NativeList<NativeTriangleDetailData> overlapBack =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.Persistent);

        var getSideFaceJob = new BurstGetFaceDirection
        {
            BaseMesh = baseMesh,
            VerticesSide = getSideJob.VertsSide,
            Quantize = _quantizationPrecision,
            FrontSideMesh = frontSideMesh.GetParallelWriter(),
            BackSideMesh = backSideMesh.GetParallelWriter(),
            OverlapFrontDominant = overlapFront.AsParallelWriter(),
            OverlapBackDominant = overlapBack.AsParallelWriter(),
        };

        jobHandle = getSideFaceJob.Schedule(baseMesh.SubMesh.Length, _lnnerLoopBatchCount);
        await UniTask.WaitUntil(() => jobHandle.IsCompleted);
        jobHandle.Complete();

        Debug.Log($"面左右分け所要時間{individualStopwatch.ElapsedMilliseconds}ms {overlapBack.Length}");

        #endregion

        #region 必要配列やリストを破棄する

        baseMesh.Dispose();
        result.Dispose();
        frontSideMesh.Dispose();
        backSideMesh.Dispose();
        overlapFront.Dispose();
        overlapBack.Dispose();

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

    [BurstCompile]
    public static void CalculateDirect(
        [ReadOnly] in NativeArray<float3> verts,
        in NativePlane blade,
        ref NativeArray<int> result)
    {
        for (int i = 0; i < verts.Length; i++)
        {
            result[i] = blade.GetSide(verts[i]);
        }
    }
}