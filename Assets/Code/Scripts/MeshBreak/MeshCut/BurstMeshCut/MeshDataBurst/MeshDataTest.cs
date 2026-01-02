using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MeshDataTest : MonoBehaviour
{
    [SerializeField] private Mesh _mesh;

    void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(_mesh);
        using NativeArray<float3> vertices = new NativeArray<float3>(_mesh.vertexCount, Allocator.Persistent);
        Mesh.MeshData meshData = meshDataArray[0];

        meshData.GetVertices(vertices.Reinterpret<Vector3>());

        stopwatch.Stop();
        Debug.Log($"{stopwatch.ElapsedMilliseconds}ms");

        var arrayMeshData = _mesh.vertices;


        for (int i = 0; i < arrayMeshData.Length; i++)
        {
            if (arrayMeshData[i].x != vertices[i].x ||
                arrayMeshData[i].y != vertices[i].y ||
                arrayMeshData[i].z != vertices[i].z)
            {
                Debug.Log($"Native:{vertices[i]}  Array:{arrayMeshData[i]}  i = {i}");
            }
        }

        Debug.Log("Success");
    }
}