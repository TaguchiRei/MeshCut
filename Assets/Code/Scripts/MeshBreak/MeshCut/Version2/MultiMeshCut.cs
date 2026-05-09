using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class MultiMeshCut
{
    public bool Complete { private set; get; }
    public Mesh[] CutMesh { private set; get; }
    public List<List<Vector3>> SamplingPoints { private set; get; }

    private UniTask _cutTask;
    private int _batchCount = 32;
    private int _sampling = 150;

    public UniTask Cut(CuttableObject[] breakables, NativePlane blade)
    {
        Complete = false;
        _cutTask = CutAsync(breakables, blade, _batchCount, _sampling);

        return _cutTask;
    }

    /// <summary>
    /// バッチ数を登録します
    /// </summary>
    /// <param name="batchCount"></param>
    public void SetBatch(int batchCount)
    {
        if (batchCount <= 0)
        {
            Debug.LogWarning("Batch count must be > 0");
        }

        _batchCount = batchCount;
    }

    /// <summary>
    /// 軽量化メッシュ用サンプリング数を設定します
    /// </summary>
    /// <param name="sampling"></param>
    public void SetSamplingCount(int sampling)
    {
        if (sampling < 10)
        {
            Debug.LogWarning("サンプリング数が少なすぎます");
            return;
        }

        _sampling = sampling;
    }

    private async UniTask CutAsync(CuttableObject[] breakables, NativePlane blade, int batchCount, int sampling)
    {
        Stopwatch totalStopwatch = new Stopwatch();
        totalStopwatch.Start();

        MultiCutContext context = new MultiCutContext(breakables.Length);
        try
        {
            Stopwatch stopwatch = new Stopwatch(); // 各セクション計測用

            Mesh[] mesh = new Mesh[breakables.Length];

            //MeshDataArrayを取得
            for (int i = 0; i < breakables.Length; i++)
            {
                mesh[i] = breakables[i].Mesh.sharedMesh;
            }

            context.BaseMeshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

            //合計頂点数等取得
            int totalVerticesCount = 0;
            int maxVerticesCount = 0;
            for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
            {
                var vertexCount = context.BaseMeshDataArray[i].vertexCount;
                totalVerticesCount += vertexCount;
                if (vertexCount > maxVerticesCount)
                {
                    maxVerticesCount = vertexCount;
                }
            }

            //ベースのデータを保持する配列を初期化
            context.BaseVertices =
                new(totalVerticesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.BaseNormals = new(totalVerticesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.BaseUvs = new(totalVerticesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.BaseVertexSide =
                new(totalVerticesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            context.Blades =
                new(context.BaseMeshDataArray.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.VertexObjectIndex =
                new(totalVerticesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            int startIndex = 0;

            //オブジェクト毎にループする初期化を行う
            for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
            {
                var data = context.BaseMeshDataArray[i];

                #region Base頂点配列初期化

                var baseV = context.BaseVertices.GetSubArray(startIndex, data.vertexCount);
                var baseN = context.BaseNormals.GetSubArray(startIndex, data.vertexCount);
                var baseU = context.BaseUvs.GetSubArray(startIndex, data.vertexCount);

                data.GetVertices(baseV.Reinterpret<Vector3>());
                data.GetNormals(baseN.Reinterpret<Vector3>());
                data.GetUVs(0, baseU.Reinterpret<Vector2>());

                #endregion

                #region Blades初期化

                //Bladeと頂点の対応をとるための配列
                for (int j = 0; j < data.vertexCount; j++)
                {
                    context.VertexObjectIndex[startIndex + j] = i;
                }

                //オブジェクトごとにBladeの位置と回転、スケールにオフセットを掛ける
                var cutObject = breakables[i].gameObject;
                quaternion invRot = math.inverse(cutObject.transform.rotation);
                float3 reciprocal = math.rcp(cutObject.transform.localScale);
                float3 position = blade.Position - (float3)cutObject.transform.position;
                position = math.mul(invRot, position);
                position *= reciprocal;
                float3 normal = math.mul(invRot, blade.Normal);
                normal *= reciprocal;
                context.Blades[i] = new NativePlane(position, normal);

                #endregion

                context.StartIndex.Add(startIndex);

                //次ループの結合頂点配列の開始インデックスとして扱える
                startIndex += data.vertexCount;
            }

            Debug.Log("頂点群データ取得完了");
            stopwatch.Stop();
            Debug.Log($"計測: 初期化処理 - {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Reset();

            #region 頂点のサイドを取得

            var vertexGetSideJob = new VertexGetSideJob
            {
                Vertices = context.BaseVertices,
                BladeIndex = context.VertexObjectIndex,
                Blades = context.Blades,
                VertexSides = context.BaseVertexSide
            };

            JobHandle vertexGetSideHandle = vertexGetSideJob.Schedule(context.BaseVertices.Length, batchCount);

            await vertexGetSideHandle.ToUniTask(PlayerLoopTiming.Update);

            Debug.Log("頂点群仕分け完了");

            #endregion

            #region 左右分け

            context.breakMeshes = new();
            List<int> triangleObjectTable = new();

            var vertices = context.BaseVertices;
            var normals = context.BaseNormals;
            var uvs = context.BaseUvs;

            //オブジェクト数分ループ
            for (int objIndex = 0; objIndex < context.BaseMeshDataArray.Length; objIndex++)
            {
                var objectStartIndex = context.StartIndex[objIndex];
                var meshData = context.BaseMeshDataArray[objIndex];
                var triangles = meshData.GetIndexData<ushort>();
                BurstBreakMesh frontSide = new BurstBreakMesh(meshData.vertexCount);
                BurstBreakMesh backSide = new BurstBreakMesh(meshData.vertexCount);

                //サブメッシュ数分ループ
                for (int submesh = 0; submesh < meshData.subMeshCount; submesh++)
                {
                    frontSide.AddSubmesh();
                    backSide.AddSubmesh();
                    SubMeshDescriptor subMeshDesc = meshData.GetSubMesh(submesh);
                    int start = subMeshDesc.indexStart;
                    int count = subMeshDesc.indexCount;

                    var indexData = triangles.GetSubArray(start, count);

                    //三角形ごとにループ
                    for (int i = 0; i < indexData.Length; i += 3)
                    {
                        // 全体のインデックス
                        var globalIndex1 = indexData[i + 0] + objectStartIndex;
                        var globalIndex2 = indexData[i + 1] + objectStartIndex;
                        var globalIndex3 = indexData[i + 2] + objectStartIndex;

                        // オブジェクトごとのインデックス
                        var localIndex1 = indexData[i + 0];
                        var localIndex2 = indexData[i + 1];
                        var localIndex3 = indexData[i + 2];

                        int result =
                            (context.BaseVertexSide[globalIndex1] << 2) |
                            (context.BaseVertexSide[globalIndex2] << 1) |
                            (context.BaseVertexSide[globalIndex3] << 0);

                        switch (result)
                        {
                            case 0: //0なら裏側
                                backSide.AddTriangleLegacyIndex(
                                    localIndex1, localIndex2, localIndex3,
                                    vertices[globalIndex1], vertices[globalIndex2], vertices[globalIndex3],
                                    normals[globalIndex1], normals[globalIndex2], normals[globalIndex3],
                                    uvs[globalIndex1], uvs[globalIndex2], uvs[globalIndex3],
                                    submesh);
                                break;
                            case 7:
                                frontSide.AddTriangleLegacyIndex(
                                    localIndex1, localIndex2, localIndex3,
                                    vertices[globalIndex1], vertices[globalIndex2], vertices[globalIndex3],
                                    normals[globalIndex1], normals[globalIndex2], normals[globalIndex3],
                                    uvs[globalIndex1], uvs[globalIndex2], uvs[globalIndex3],
                                    submesh);
                                break;
                            default:
                                triangleObjectTable.Add(objIndex);
                                context.CutFaces.Add(new(globalIndex1, globalIndex2, globalIndex3));
                                context.CutStatus.Add(result);
                                context.CutFaceSubmeshId.Add(submesh);
                                context.TriangleObjectIndex.Add(objIndex);
                                break;
                        }
                    }
                }

                context.breakMeshes.Add(frontSide);
                context.breakMeshes.Add(backSide);
            }

            Debug.Log("面仕分け完了");

            #endregion

            #region 三角形の分割

            int triangleCount = triangleObjectTable.Count;
            context.NewVertices = new(triangleCount * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.NewNormals = new(triangleCount * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.NewUvs = new(triangleCount * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.NewTriangles = new(triangleCount * 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            context.CutEdges = new(triangleCount * 2, Allocator.Persistent);

            var triangleCutJob = new TriangleCutJob
            {
                CutFaces = context.CutFaces.AsArray(),
                CutStatus = context.CutStatus.AsArray(),
                CutFaceSubmeshId = context.CutFaceSubmeshId.AsArray(),
                Blades = context.Blades,
                TriangleObjectIndex = context.TriangleObjectIndex.AsArray(),
                BaseVertices = context.BaseVertices,
                BaseNormals = context.BaseNormals,
                BaseUvs = context.BaseUvs,

                NewVertices = context.NewVertices,
                NewNormals = context.NewNormals,
                NewUvs = context.NewUvs,
                NewTriangles = context.NewTriangles,
                CutEdges = context.CutEdges.AsParallelWriter()
            };

            JobHandle triangleCutHandle = triangleCutJob.Schedule(triangleCount, batchCount);

            await triangleCutHandle.ToUniTask(PlayerLoopTiming.Update);

            Debug.Log($"面切断完了{triangleCount}  newTriangles {context.NewTriangles.Length}");

            #endregion

            #region 切断面の穴埋め処理

            //切断した三角形を追加
            for (int i = 0; i < context.NewTriangles.Length; i++)
            {
                var nt = context.NewTriangles[i];
                int objIdx = context.TriangleObjectIndex[i / 3];
                var target = context.breakMeshes[objIdx * 2 + (nt.Side == 1 ? 0 : 1)];
                AddNewTriangle(target, nt, context);
            }

            // ループ抽出と耳切り法
            var allLoops = FindAllLoops(context, breakables.Length);
            for (int i = 0; i < breakables.Length; i++)
            {
                context.breakMeshes[i * 2].AddSubmesh(); // 断面用サブメッシュ
                context.breakMeshes[i * 2 + 1].AddSubmesh();
                foreach (var loop in allLoops[i])
                {
                    FillCapFan(i, loop, context, context.breakMeshes[i * 2], true);
                    FillCapFan(i, loop, context, context.breakMeshes[i * 2 + 1], false);
                }
            }

            Debug.Log("耳切法により断面生成完了");

            #endregion

            List<List<Vector3>> colliderVerticesPerFragment = new();

            for (int i = 0; i < context.breakMeshes.Count; i++)
            {
                var source = context.breakMeshes[i];
                var rawVerts = source.Vertices.AsArray();
                int totalCount = rawVerts.Length;

                List<Vector3> simplifiedVerts = new List<Vector3>();

                if (totalCount <= 200)
                {
                    // 頂点数が少なければそのまま全コピー
                    for (int j = 0; j < totalCount; j++) simplifiedVerts.Add(rawVerts[j]);
                }
                else
                {
                    // 均等にサンプリング（例：150点程度を目標にする）
                    float step = (float)totalCount / sampling;
                    for (int j = 0; j < sampling; j++)
                    {
                        int index = (int)(j * step);
                        simplifiedVerts.Add(rawVerts[index]);
                    }
                }

                colliderVerticesPerFragment.Add(simplifiedVerts);
            }

            Debug.Log("メッシュ生成完了");

            CutMesh = FinalizeMeshes(context.breakMeshes);
            SamplingPoints = colliderVerticesPerFragment;
            Complete = true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw new Exception(e.Message);
        }
        finally
        {
            context.Dispose();
            totalStopwatch.Stop();
            Debug.Log($"計測: MultiMeshCut.CutAsync 全体処理時間 - {totalStopwatch.ElapsedMilliseconds} ms");
        }
    }

    private void AddNewTriangle(BurstBreakMesh target, NewTriangle nt, MultiCutContext context)
    {
        if (target == null) return;
        // 頂点データの解決（インデックスが負ならBase、正ならNewから取得）
        float3
            v1 = GetVertex(nt.Vertex1, context),
            v2 = GetVertex(nt.Vertex2, context),
            v3 = GetVertex(nt.Vertex3, context);
        float3
            n1 = GetNormal(nt.Vertex1, context),
            n2 = GetNormal(nt.Vertex2, context),
            n3 = GetNormal(nt.Vertex3, context);
        float2
            u1 = GetUv(nt.Vertex1, context),
            u2 = GetUv(nt.Vertex2, context),
            u3 = GetUv(nt.Vertex3, context);
        target.AddTriangle(v1, v2, v3, n1, n2, n3, u1, u2, u3, n1, nt.Submesh);
    }

    /// <summary>
    /// 旧頂点と新頂点で適切なものを取得する
    /// </summary>
    /// <param name="index"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    private float3 GetVertex(int index, MultiCutContext c)
    {
        return index < 0 ? c.BaseVertices[-(index + 1)] : c.NewVertices[index];
    }

    /// <summary>
    /// 旧法線と旧法線で適切なものを取得する
    /// </summary>
    /// <param name="index"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    private float3 GetNormal(int index, MultiCutContext c)
    {
        return index < 0 ? c.BaseNormals[-(index + 1)] : c.NewNormals[index];
    }

    /// <summary>
    /// 旧UVと新UVで適切なものを取得する
    /// </summary>
    /// <param name="index"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    private float2 GetUv(int index, MultiCutContext c)
    {
        return index < 0 ? c.BaseUvs[-(index + 1)] : c.NewUvs[index];
    }

    /// <summary>
    /// 全オブジェクト群の切断面のループを捜索する
    /// </summary>
    /// <returns></returns>
    private List<List<int>>[] FindAllLoops(MultiCutContext context, int objectCount)
    {
        List<List<int>>[] allLoops = new List<List<int>>[objectCount];

        for (int i = 0; i < objectCount; i++)
        {
            allLoops[i] = new List<List<int>>();
        }

        var allCutEdges = context.CutEdges;
        const float precision = 10000f; // 0.1mm単位で丸める

        for (int objIndex = 0; objIndex < objectCount; objIndex++)
        {
            // 座標(量子化済み)から代表インデックスへのマップ
            Dictionary<long3, int> posToRepresentative = new Dictionary<long3, int>();
            // 代表インデックス間の隣接リスト
            Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();

            if (allCutEdges.ContainsKey(objIndex))
            {
                var iterator = allCutEdges.GetValuesForKey(objIndex);
                int edgeCount = 0;

                foreach (var edge in iterator)
                {
                    edgeCount++;
                    
                    float3 vA = context.NewVertices[edge.x];
                    float3 vB = context.NewVertices[edge.y];

                    // 座標を整数値に変換して誤差を吸収
                    long3 keyA = new long3(
                        (long)math.round(vA.x * precision),
                        (long)math.round(vA.y * precision),
                        (long)math.round(vA.z * precision)
                    );
                    long3 keyB = new long3(
                        (long)math.round(vB.x * precision),
                        (long)math.round(vB.y * precision),
                        (long)math.round(vB.z * precision)
                    );

                    if (!posToRepresentative.TryGetValue(keyA, out int repA))
                    {
                        repA = edge.x;
                        posToRepresentative.Add(keyA, repA);
                    }
                    if (!posToRepresentative.TryGetValue(keyB, out int repB))
                    {
                        repB = edge.y;
                        posToRepresentative.Add(keyB, repB);
                    }

                    // 代表インデックス同士を繋ぐ
                    if (!adjacency.TryGetValue(repA, out var listA))
                    {
                        listA = new List<int>();
                        adjacency.Add(repA, listA);
                    }
                    // 同じ頂点間のエッジ（縮退エッジ）を避ける
                    if (repA != repB)
                    {
                        if (!listA.Contains(repB)) listA.Add(repB);

                        if (!adjacency.TryGetValue(repB, out var listB))
                        {
                            listB = new List<int>();
                            adjacency.Add(repB, listB);
                        }
                        if (!listB.Contains(repA)) listB.Add(repA);
                    }
                }
                Debug.Log($"Object {objIndex}: {edgeCount} edges processed (quantized). Adjacency keys: {adjacency.Count}");

                iterator.Dispose();
            }
            else
            {
                Debug.Log($"Object {objIndex}: No edges found in CutEdges.");
            }

            HashSet<(int, int)> visitedEdges = new HashSet<(int, int)>();

            foreach (var kv in adjacency)
            {
                int start = kv.Key;

                foreach (var nextStart in kv.Value)
                {
                    if (visitedEdges.Contains((start, nextStart))) continue;

                    List<int> loop = new List<int>();

                    int prev = start;
                    int current = nextStart;

                    loop.Add(start);

                    visitedEdges.Add((start, nextStart));
                    visitedEdges.Add((nextStart, start));

                    while (true)
                    {
                        loop.Add(current);

                        var neighbors = adjacency[current];

                        int next = -1;

                        for (int i = 0; i < neighbors.Count; i++)
                        {
                            if (neighbors[i] != prev)
                            {
                                next = neighbors[i];
                                break;
                            }
                        }

                        if (next == -1)
                            break;

                        if (next == start)
                        {
                            if (loop.Count >= 3)
                            {
                                allLoops[objIndex].Add(loop);
                            }

                            break;
                        }

                        if (visitedEdges.Contains((current, next)))
                            break;

                        visitedEdges.Add((current, next));
                        visitedEdges.Add((next, current));

                        prev = current;
                        current = next;
                    }
                }
            }
        }

        return allLoops;
    }

    /// <summary>
    /// 断面メッシュを重心と外縁の頂点で作成(凹型未対応だが、軽量)
    /// </summary>
    /// <param name="objIdx"></param>
    /// <param name="loop"></param>
    /// <param name="context"></param>
    /// <param name="target"></param>
    /// <param name="isFront"></param>
    private void FillCapFan(
        int objIdx,
        List<int> loop,
        MultiCutContext context,
        BurstBreakMesh target,
        bool isFront)
    {
        if (loop == null || loop.Count < 3)
            return;

        var blade = context.Blades[objIdx];

        // 重心を求める
        float3 center = float3.zero;

        for (int i = 0; i < loop.Count; i++)
        {
            center += context.NewVertices[loop[i]];
        }

        center /= loop.Count;

        // UV座標を定義

        float3 normal = blade.Normal;

        float3 tangent =
            math.abs(normal.y) > 0.999f
                ? math.normalize(math.cross(normal, new float3(1, 0, 0)))
                : math.normalize(math.cross(normal, new float3(0, 1, 0)));

        float3 bitangent = math.normalize(math.cross(normal, tangent));

        // 法線作成

        float3 faceNormal = isFront ? -blade.Normal : blade.Normal;

        int capSubmeshIndex = target.Triangles.Count - 1;

        // 各三角形を作成

        for (int i = 0; i < loop.Count; i++)
        {
            int currentIndex = loop[i];
            int nextIndex = loop[(i + 1) % loop.Count];

            float3 v0 = context.NewVertices[currentIndex];
            float3 v1 = context.NewVertices[nextIndex];
            float3 v2 = center;

            // UVを作成

            float3 d0 = v0 - center;
            float3 d1 = v1 - center;

            float2 uv0 = new float2(0.5f + math.dot(d0, tangent), 0.5f + math.dot(d0, bitangent));

            float2 uv1 = new float2(0.5f + math.dot(d1, tangent), 0.5f + math.dot(d1, bitangent));

            float2 uv2 = new float2(0.5f, 0.5f);

            // 三角形を追加

            target.AddTriangle(
                v0, v1, v2,
                faceNormal, faceNormal, faceNormal,
                uv0, uv1, uv2,
                faceNormal,
                capSubmeshIndex
            );
        }
    }

    private Mesh[] FinalizeMeshes(List<BurstBreakMesh> breakMeshes)
    {
        int fragmentCount = breakMeshes.Count;
        var writableDataArray = Mesh.AllocateWritableMeshData(fragmentCount);

        for (int i = 0; i < fragmentCount; i++)
        {
            var source = breakMeshes[i];
            var data = writableDataArray[i];

            // 頂点属性の設定
            int vertexCount = source.Vertices.Length;

            //  0, 1, 2 と分けて指定する
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, stream: 1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream: 2)
            };

            data.SetVertexBufferParams(vertexCount, layout);

            // 正しいストリームからデータを取得する
            var vertices = data.GetVertexData<float3>(0); // stream 0
            var normals = data.GetVertexData<float3>(1); // stream 1
            var uvs = data.GetVertexData<float2>(2); // stream 2

            // NativeListからコピー
            vertices.CopyFrom(source.Vertices.AsArray());
            normals.CopyFrom(source.Normals.AsArray());
            uvs.CopyFrom(source.Uvs.AsArray());

            // サブメッシュとインデックスの設定
            int totalIndexCount = 0;
            foreach (var subTri in source.Triangles) totalIndexCount += subTri.Length;

            data.SetIndexBufferParams(totalIndexCount, IndexFormat.UInt32);
            var indices = data.GetIndexData<int>();

            int indexOffset = 0;
            data.subMeshCount = source.Triangles.Count;
            for (int s = 0; s < source.Triangles.Count; s++)
            {
                int subCount = source.Triangles[s].Length;
                var subIndices = source.Triangles[s];
                for (int j = 0; j < subCount; j++) indices[indexOffset + j] = subIndices[j];

                data.SetSubMesh(s, new SubMeshDescriptor(indexOffset, subCount), MeshUpdateFlags.DontRecalculateBounds);
                indexOffset += subCount;
            }
        }

        Mesh[] resultMeshes = new Mesh[fragmentCount];
        for (int i = 0; i < fragmentCount; i++) resultMeshes[i] = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(writableDataArray, resultMeshes);

        return resultMeshes;
    }
}

[Serializable]
public struct long3 : IEquatable<long3>
{
    public long x;
    public long y;
    public long z;

    public static readonly long3 zero = new long3(0, 0, 0);
    public static readonly long3 one = new long3(1, 1, 1);

    public long3(long x, long y, long z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public long this[int index]
    {
        get
        {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                _ => throw new IndexOutOfRangeException("Invalid Long3 index!")
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    x = value;
                    break;
                case 1:
                    y = value;
                    break;
                case 2:
                    z = value;
                    break;
                default:
                    throw new IndexOutOfRangeException("Invalid Long3 index!");
            }
        }
    }

    public static long3 operator +(long3 a, long3 b)
    {
        return new long3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static long3 operator -(long3 a, long3 b)
    {
        return new long3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static long3 operator *(long3 a, long b)
    {
        return new long3(a.x * b, a.y * b, a.z * b);
    }

    public static long3 operator /(long3 a, long b)
    {
        return new long3(a.x / b, a.y / b, a.z / b);
    }

    public static bool operator ==(long3 a, long3 b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(long3 a, long3 b)
    {
        return !a.Equals(b);
    }

    public bool Equals(long3 other)
    {
        return x == other.x && y == other.y && z == other.z;
    }

    public override bool Equals(object obj)
    {
        return obj is long3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }

    public long MagnitudeSquared()
    {
        return x * x + y * y + z * z;
    }

    public double Magnitude()
    {
        return Math.Sqrt(MagnitudeSquared());
    }

    public static long Dot(long3 a, long3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    public static long3 Min(long3 a, long3 b)
    {
        return new long3(
            Math.Min(a.x, b.x),
            Math.Min(a.y, b.y),
            Math.Min(a.z, b.z)
        );
    }

    public static long3 Max(long3 a, long3 b)
    {
        return new long3(
            Math.Max(a.x, b.x),
            Math.Max(a.y, b.y),
            Math.Max(a.z, b.z)
        );
    }
}