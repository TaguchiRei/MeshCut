/*
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

public class BurstMeshCutL2 : MonoBehaviour
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
        NativeMeshDataL2 baseMesh = new NativeMeshDataL2(mesh.vertexCount, mesh.subMeshCount);
        NativeMeshDataUtility.ReadMeshDataSafely(
            mesh, baseMesh.Vertices, baseMesh.Normals, baseMesh.Uvs, baseMesh.SubMesh);
        float3 pos = new(gameObject.transform.position.x, gameObject.transform.position.y,
            gameObject.transform.position.z);
        float3 normal = new(gameObject.transform.up.x, gameObject.transform.up.y, gameObject.transform.up.z);

        #endregion

        #region 調べる処理

        Stopwatch individualStopwatch = new Stopwatch();
        individualStopwatch.Start();
        NativeArray<int> vertSide = new(baseMesh.Vertices.Length, Allocator.TempJob);
        BurstGetSide.CalculateDirect(baseMesh.Vertices, blade,  vertSide);

        Debug.Log($"頂点左右分け所要時間{individualStopwatch.ElapsedMilliseconds}ms {vertSide[0]}");

        individualStopwatch.Restart();

        int triangleCount = baseMesh.SubMesh.Length;
        NativeMeshDataParallel frontSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.TempJob);
        NativeMeshDataParallel backSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.TempJob);
        NativeList<NativeTriangleDetailData> overlapFront =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.TempJob);
        NativeList<NativeTriangleDetailData> overlapBack =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.TempJob);


        BurstGetFaceDirectionL2.CalculateFaceDirectionDirect(
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
        NativeMeshDataL2 baseMesh = new NativeMeshDataL2(mesh.vertexCount, mesh.subMeshCount);
        NativeMeshDataUtility.ReadMeshDataSafely(
            mesh, baseMesh.Vertices, baseMesh.Normals, baseMesh.Uvs, baseMesh.SubMesh);
        float3 pos = new(gameObject.transform.position.x, gameObject.transform.position.y,
            gameObject.transform.position.z);
        float3 normal = new(gameObject.transform.up.x, gameObject.transform.up.y, gameObject.transform.up.z);

        #endregion

        #region 調べる処理

        Stopwatch individualStopwatch = new Stopwatch();
        individualStopwatch.Start();

        NativeArray<int> result = new(baseMesh.Vertices.Length, Allocator.TempJob);
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
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.TempJob);
        NativeMeshDataParallel backSideMesh =
            new NativeMeshDataParallel(triangleCount * 3, baseMesh.SubMeshCount, Allocator.TempJob);
        NativeList<NativeTriangleDetailData> overlapFront =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.TempJob);
        NativeList<NativeTriangleDetailData> overlapBack =
            new NativeList<NativeTriangleDetailData>(triangleCount, Allocator.TempJob);

        var getSideFaceJob = new BurstGetFaceDirectionL2
        {
            BaseMesh = baseMesh,
            VerticesSide = getSideJob.VertsSide,
            Quantize = _quantizationPrecision,
            FrontSideMesh = frontSideMesh,
            BackSideMesh = backSideMesh,
            OverlapFrontDominant = overlapFront,
            OverlapBackDominant = overlapBack,
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
        VertsSide[index] = math.dot(Vertices[index] - Blade.Position, Blade.Normal) > 0.0f ? 1 : 0;
    }

    [BurstCompile]
    public static void CalculateDirect(
        NativeArray<float3> verts,
        NativePlane blade,
        NativeArray<int> result)
    {
        for (int i = 0; i < verts.Length; i++)
        {
            result[i] = math.dot(verts[i] - blade.Position, blade.Normal) > 0.0f ? 1 : 0;
        }
    }
}
*/