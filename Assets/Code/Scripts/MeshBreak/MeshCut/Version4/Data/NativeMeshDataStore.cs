using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MeshBreak.MeshCut.Version4
{
    /// <summary>
    /// シーン内に登録された全ユニークメッシュを、Burst Jobから直接読めるフラットなNativeArrayとして
    /// 一度だけ構築・保持するストア。ジャグ配列(int[][])を毎カット時に走査する必要が無いようにする。
    /// </summary>
    public class NativeMeshDataStore : IDisposable
    {
        public NativeList<float3> Vertices;
        public NativeList<float3> Normals;
        public NativeList<float2> Uvs;

        /// <summary> メッシュローカルな頂点インデックスを持つ、サブメッシュをまたいでフラット化済みの三角形リスト </summary>
        public NativeList<int3> Triangles;

        /// <summary> Trianglesと1対1対応するサブメッシュ番号 </summary>
        public NativeList<int> TriangleSubmesh;

        /// <summary> メッシュIDごとの (Vertices/Normals/Uvs内でのstart, count) </summary>
        public NativeList<int2> MeshVertexRange;

        /// <summary> メッシュIDごとの (Triangles/TriangleSubmesh内でのstart, count) </summary>
        public NativeList<int2> MeshTriangleRange;

        /// <summary> メッシュIDごとの元のサブメッシュ数 </summary>
        public NativeList<int> MeshSubmeshCount;

        public int MeshCount => MeshVertexRange.Length;

        public NativeMeshDataStore()
        {
            Vertices = new NativeList<float3>(Allocator.Persistent);
            Normals = new NativeList<float3>(Allocator.Persistent);
            Uvs = new NativeList<float2>(Allocator.Persistent);
            Triangles = new NativeList<int3>(Allocator.Persistent);
            TriangleSubmesh = new NativeList<int>(Allocator.Persistent);
            MeshVertexRange = new NativeList<int2>(Allocator.Persistent);
            MeshTriangleRange = new NativeList<int2>(Allocator.Persistent);
            MeshSubmeshCount = new NativeList<int>(Allocator.Persistent);
        }

        /// <summary> メッシュを登録し、割り当てられたメッシュIDを返す </summary>
        public int Add(Mesh mesh)
        {
            int vStart = Vertices.Length;
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int vertexCount = mesh.vertexCount;

            Vertices.Resize(vStart + vertexCount, NativeArrayOptions.UninitializedMemory);
            Normals.Resize(vStart + vertexCount, NativeArrayOptions.UninitializedMemory);
            Uvs.Resize(vStart + vertexCount, NativeArrayOptions.UninitializedMemory);

            Vertices.AsArray().GetSubArray(vStart, vertexCount).Reinterpret<Vector3>().CopyFrom(verts);
            Normals.AsArray().GetSubArray(vStart, vertexCount).Reinterpret<Vector3>().CopyFrom(normals);
            Uvs.AsArray().GetSubArray(vStart, vertexCount).Reinterpret<Vector2>().CopyFrom(uvs);

            int subCount = mesh.subMeshCount;
            int tStart = Triangles.Length;

            for (int s = 0; s < subCount; s++)
            {
                int[] tris = mesh.GetTriangles(s);
                int triCount = tris.Length / 3;

                for (int i = 0; i < triCount; i++)
                {
                    Triangles.Add(new int3(tris[i * 3 + 0], tris[i * 3 + 1], tris[i * 3 + 2]));
                    TriangleSubmesh.Add(s);
                }
            }

            MeshVertexRange.Add(new int2(vStart, vertexCount));
            MeshTriangleRange.Add(new int2(tStart, Triangles.Length - tStart));
            MeshSubmeshCount.Add(subCount);

            return MeshVertexRange.Length - 1;
        }

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            Uvs.Clear();
            Triangles.Clear();
            TriangleSubmesh.Clear();
            MeshVertexRange.Clear();
            MeshTriangleRange.Clear();
            MeshSubmeshCount.Clear();
        }

        public void Dispose()
        {
            if (Vertices.IsCreated) Vertices.Dispose();
            if (Normals.IsCreated) Normals.Dispose();
            if (Uvs.IsCreated) Uvs.Dispose();
            if (Triangles.IsCreated) Triangles.Dispose();
            if (TriangleSubmesh.IsCreated) TriangleSubmesh.Dispose();
            if (MeshVertexRange.IsCreated) MeshVertexRange.Dispose();
            if (MeshTriangleRange.IsCreated) MeshTriangleRange.Dispose();
            if (MeshSubmeshCount.IsCreated) MeshSubmeshCount.Dispose();
        }
    }
}
