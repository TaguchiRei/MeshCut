using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
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
        BurstCutUtility.GetSide(verts, pos, normal, ref result);

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
public static class BurstCutUtility
{
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatPrecision = FloatPrecision.Low,
        FloatMode = FloatMode.Fast)]
    public static void GetSide(
        [ReadOnly] in NativeArray<float3> vertices,
        in float3 bladePos, in float3 bladeNormal,
        ref NativeArray<bool> results)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            results[i] = math.dot(vertices[i] - bladePos, bladeNormal) > 0f;
        }
    }
}

public class Hoge
{
    private void Test(int a, int b, int c, int id, ref List<List<int>> list)
    {
        //abcはそれぞれ結果を1がtrue、0がfalseになるよう数学的に求めた結果が入っている
        
        int result = a + b + c;
        list[result].Add(id);
    }
}