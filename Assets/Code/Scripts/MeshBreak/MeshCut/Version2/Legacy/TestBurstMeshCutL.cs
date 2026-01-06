/*
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MeshBreak;
using Unity.Mathematics;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

public class TestBurstMeshCutL : MonoBehaviour
{
    [SerializeField] private int _meshCutNumber;
    [SerializeField] private Collider _myCollider;
    [SerializeField] private Material _capMaterial;

    [MethodExecutor("TestMethod", false)]
    private void Test()
    {
        Stopwatch st = Stopwatch.StartNew();
        List<GameObject> newObjects = new();
        var cutObjects = CheckOverlapObjects().ToHashSet();

        Stopwatch stopwatch = new();
        stopwatch.Start();
        foreach (var obj in cutObjects)
        {
        }

        Debug.Log($"メッシュ切断完了。総オブジェクト数:{cutObjects.Count} 全体処理時間:{stopwatch.ElapsedMilliseconds}ms");
    }

    private GameObject[] CheckOverlapObjects()
    {
        // コライダーの範囲内にあるオブジェクトを取得
        List<GameObject> objects = new();
        Collider[] hits = Physics.OverlapBox(
            _myCollider.bounds.center,
            _myCollider.bounds.extents,
            Quaternion.identity
        );

        foreach (Collider hit in hits)
        {
            if (!hit.gameObject.TryGetComponent<BreakableObject>(out BreakableObject cuttable)) continue;
            objects.Add(hit.gameObject);
            var mesh = hit.gameObject.GetComponent<MeshFilter>().mesh;

            ExecuteDirectCut(mesh, destroyCancellationToken).Forget();

            //Instantiate()
        }

        return objects.ToArray();
    }

    private async UniTask<List<Mesh>> ExecuteDirectCut(Mesh mesh, CancellationToken cancellationToken)
    {
        NativeArray<float3> vertices = new NativeArray<float3>(mesh.vertexCount, Allocator.TempJob);
        NativeArray<float3> normals = new NativeArray<float3>(mesh.vertexCount, Allocator.TempJob);
        NativeArray<float2> uvs = new NativeArray<float2>(mesh.vertexCount, Allocator.TempJob);
        NativeParallelMultiHashMap<int, int3> submesh =
            new NativeParallelMultiHashMap<int, int3>(mesh.subMeshCount, Allocator.TempJob);
        MeshDataSupport.ReadMeshDataSafely(mesh, vertices, normals, uvs, submesh);

        var leftResult = new NativeBreakMeshDataL();
        var rightResult = new NativeBreakMeshDataL();

        var job = new BurstMeshCutL
        {
            BaseMeshData = new BaseMeshData(vertices, normals, uvs, submesh, mesh.subMeshCount),
            BladePosition = transform.position,
            BladeNormal = transform.up,
            ConnectionCapacity = 512,
        };

        JobHandle handle = job.Schedule();

        while (!handle.IsCompleted)
        {
            await UniTask.Yield();
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        List<Mesh> results = new();

        results.Add(MeshDataSupport.ToMesh(leftResult));
        results.Add(MeshDataSupport.ToMesh(rightResult));

        return results;
    }
}

[BurstCompile]
public static class BurstMeshCutUtil
{
    [BurstCompile]
    public static void Cut()
    {
    }
}
*/