using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace MeshBreak.MeshBooleanOperator
{
    /// <summary>
    /// メッシュを別のメッシュの内側にある部分とそれ以外で分割する
    /// </summary>
    public class MeshBooleanOperator
    {
        public void GetMeshDataForNative()
        {
            Mesh testMesh = new Mesh();
            using (var data = Mesh.AcquireReadOnlyMeshData(testMesh))
            {
                var meshData = data[0];
                var start = float3.zero;
                MeshBooleanOperatorUtility.CheckContactPoint(ref meshData, ref start, ref start);

                NativeArray<Vector3> vertices = new();
                NativeArray<Vector3> normals = new();
                NativeArray<Vector2> uvs = new();
 
                meshData.GetVertices(vertices);
                meshData.GetNormals(normals);
                meshData.GetUVs(0, uvs);
                meshData.subMeshCount = 1;
            }
        }
    }

    [BurstCompile]
    public static class MeshBooleanOperatorUtility
    {
        [BurstCompile]
        public static void CheckContactPoint(ref Mesh.MeshData meshData, ref float3 insideVert,
            ref float3 outsideVert)
        {
            //meshData.GetSubMesh();
            return;
        }
    }


    public struct BooleanMesh
    {
        public Mesh InSideMesh;
        public Mesh OutSideMesh;

        public BooleanMesh(Mesh inSide, Mesh outSide)
        {
            InSideMesh = inSide;
            OutSideMesh = outSide;
        }
    }

    public struct NativeMeshData
    {
        NativeArray<float3> vertices;
        NativeArray<float3> normals;
        NativeArray<float2> uvs;
    }
}