using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class CuttableObject : MonoBehaviour
{
    private NativeTransform _nativeTransform;
    public Mesh mesh;
    public NativeArray<NativeTriangle> Triangles;

    public bool Cuttable => mesh != null && Triangles.Length > 0;

    private async void Start()
    {
        _nativeTransform = new NativeTransform(transform);
        mesh = GetComponent<MeshFilter>().sharedMesh;
        Stopwatch stopwatch = Stopwatch.StartNew();
        await GenerateNativeTriangleArray();
        Debug.Log($"{gameObject.name} {stopwatch.ElapsedMilliseconds} ms {mesh.triangles.Length / 3} ");
    }

    public NativeTransform GetNativeTransform()
    {
        _nativeTransform.Position = transform.position;
        _nativeTransform.Rotation = transform.rotation;
        _nativeTransform.Scale = transform.localScale;

        return _nativeTransform;
    }

    private void OnDestroy()
    {
        if (Triangles.IsCreated)
            Triangles.Dispose();
    }

    private async UniTask GenerateNativeTriangleArray()
    {
        // --- メインスレッドで Mesh 情報を取得 ---
        int subMeshCount = mesh.subMeshCount;
        var triangleLists = new List<int[]>(subMeshCount);

        for (int subMeshId = 0; subMeshId < subMeshCount; subMeshId++)
        {
            triangleLists.Add(mesh.GetTriangles(subMeshId));
        }

        // 総三角形数を算出
        int triangleCount = 0;
        for (int i = 0; i < triangleLists.Count; i++)
        {
            triangleCount += triangleLists[i].Length / 3;
        }

        // NativeArray を確保
        var nativeTriangles = new NativeArray<NativeTriangle>(triangleCount, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory);

        // バックグラウンドで独自形式に変換
        await UniTask.RunOnThreadPool(() =>
        {
            int index = 0;

            for (int subMeshId = 0; subMeshId < triangleLists.Count; subMeshId++)
            {
                var triangles = triangleLists[subMeshId];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    nativeTriangles[index++] = new NativeTriangle(
                        triangles[i],
                        triangles[i + 1],
                        triangles[i + 2],
                        subMeshId
                    );
                }
            }
        });

        Triangles = nativeTriangles;
    }
    private enum MeshSize
    {
        
    }
}