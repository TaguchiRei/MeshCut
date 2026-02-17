using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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

                        Debug.Log($"result {result}");

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
                    FillCapForLoop(i, loop, context, context.breakMeshes[i * 2], true);
                    FillCapForLoop(i, loop, context, context.breakMeshes[i * 2 + 1], false);
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

        NativeParallelMultiHashMap<int, int2> allCutEdges = context.CutEdges;

        for (int objIndex = 0; objIndex < objectCount; objIndex++)
        {
            List<int2> objectCutEdges = new List<int2>();
            if (allCutEdges.ContainsKey(objIndex))
            {
                var iterator = allCutEdges.GetValuesForKey(objIndex);
                foreach (var edge in iterator)
                {
                    objectCutEdges.Add(edge);
                }

                iterator.Dispose();
            }

            // 探索しやすい形にデータを整える
            Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();
            foreach (var edge in objectCutEdges)
            {
                // Add edge.x -> edge.y
                if (!adjacencyList.ContainsKey(edge.x))
                    adjacencyList.Add(edge.x, new List<int>());
                if (!adjacencyList[edge.x].Contains(edge.y))
                    adjacencyList[edge.x].Add(edge.y);

                // Add edge.y -> edge.x
                if (!adjacencyList.ContainsKey(edge.y))
                    adjacencyList.Add(edge.y, new List<int>());
                if (!adjacencyList[edge.y].Contains(edge.x))
                    adjacencyList[edge.y].Add(edge.x);
            }

            // オブジェクトごとのループを探す
            HashSet<int> visitedVerticesGlobally = new HashSet<int>();

            foreach (var startVertex in adjacencyList.Keys)
            {
                if (visitedVerticesGlobally.Contains(startVertex)) // 発見済みの場合は決める
                    continue;

                List<int> currentLoop = new List<int>();
                int current = startVertex;
                int previous = -1;

                while (true)
                {
                    if (visitedVerticesGlobally.Contains(current) && current != startVertex)
                    {
                        foreach (var v in currentLoop)
                        {
                            visitedVerticesGlobally.Add(v);
                        }

                        currentLoop.Clear();
                        break;
                    }

                    currentLoop.Add(current);
                    visitedVerticesGlobally
                        .Add(current);

                    List<int> neighbors = adjacencyList[current];
                    int next = -1;

                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor != previous)
                        {
                            next = neighbor;
                            break;
                        }
                    }

                    if (next == -1)
                    {
                        currentLoop.Clear();
                        break;
                    }

                    if (next == startVertex)
                    {
                        if (currentLoop.Count >= 3)
                        {
                            allLoops[objIndex].Add(currentLoop);
                        }

                        break;
                    }

                    previous = current;
                    current = next;
                }
            }
        }

        return allLoops;
    }

    /// <summary>
    /// 改良耳切法を利用して断面メッシュ作成
    /// </summary>
    /// <param name="objIdx"></param>
    /// <param name="loop"></param>
    /// <param name="context"></param>
    /// <param name="target"></param>
    /// <param name="isFront"></param>
    private void FillCapForLoop(int objIdx, List<int> loop, MultiCutContext context, BurstBreakMesh target,
        bool isFront)
    {
        var blade = context.Blades[objIdx];

        // UV方向のベクトルを作成
        float3 normal = blade.Normal;
        float3 tangent = math.abs(normal.y) > 0.999f
            ? math.cross(normal, new float3(1, 0, 0))
            : math.cross(normal, new float3(0, 1, 0));
        tangent = math.normalize(tangent);
        float3 bitangent = math.normalize(math.cross(normal, tangent));

        // UV方向ベクトルを利用して座標を変換
        int vertexCount = loop.Count;
        List<Vector2> projectedVertices = new List<Vector2>(vertexCount); //変換した座標群を入れるリスト
        for (int i = 0; i < vertexCount; i++)
        {
            //ループは必ず新規頂点のみで構成されている
            float3 v3d = context.NewVertices[loop[i]];
            float3 relative = v3d - blade.Position;
            projectedVertices.Add(new Vector2(math.dot(relative, tangent), math.dot(relative, bitangent)));
        }

        // 戻り値は projectedVertices のリストに対するインデックス
        // FastEarClippingが頂点を除去しても、元のインデックスにマッピングされるようにTriangulateMappedを使用
        List<int> resultIndices = FastEarClipping.TriangulateMapped(projectedVertices, loop);

        if (resultIndices == null || resultIndices.Count == 0) return;

        // 断面サブメッシュに追加
        int capSubmeshIndex = target.Triangles.Count - 1;

        // 断面の法線を作成
        float3 faceNormal = isFront ? -blade.Normal : blade.Normal;

        for (int i = 0; i < resultIndices.Count; i += 3)
        {
            // 耳切り法の結果を元の NewVertices のインデックスに復元
            int idx0 = loop[resultIndices[i]];
            int idx1 = loop[resultIndices[i + 1]];
            int idx2 = loop[resultIndices[i + 2]];

            // 頂点属性の取得
            float3 v0 = context.NewVertices[idx0], v1 = context.NewVertices[idx1], v2 = context.NewVertices[idx2];
            float3 n0 = context.NewNormals[idx0], n1 = context.NewNormals[idx1], n2 = context.NewNormals[idx2];
            float2 u0 = context.NewUvs[idx0], u1 = context.NewUvs[idx1], u2 = context.NewUvs[idx2];

            // 断面なので、法線は全て一律で断面法線（faceNormal）を割り当てる
            target.AddTriangle(
                v0, v1, v2,
                faceNormal, faceNormal, faceNormal,
                u0, u1, u2,
                faceNormal, capSubmeshIndex
            );
        }
    }

    private Mesh[] FinalizeMeshesSimple(List<BurstBreakMesh> breakMeshes)
    {
        int fragmentCount = breakMeshes.Count;
        Mesh[] resultMeshes = new Mesh[fragmentCount];

        for (int i = 0; i < fragmentCount; i++)
        {
            var source = breakMeshes[i];
            Mesh mesh = new Mesh();

            // NativeArray -> List に変換
            List<Vector3> verts = new List<Vector3>(source.Vertices.Length);
            for (int v = 0; v < source.Vertices.Length; v++)
                verts.Add(source.Vertices[v]);

            List<Vector3> normals = new List<Vector3>(source.Normals.Length);
            for (int n = 0; n < source.Normals.Length; n++)
                normals.Add(source.Normals[n]);

            List<Vector2> uvs = new List<Vector2>(source.Uvs.Length);
            for (int u = 0; u < source.Uvs.Length; u++)
                uvs.Add(source.Uvs[u]);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            // サブメッシュとインデックス
            mesh.subMeshCount = source.Triangles.Count;
            for (int s = 0; s < source.Triangles.Count; s++)
            {
                // NativeList<int> -> List<int> に変換
                List<int> indices = new List<int>(source.Triangles[s].Length);
                for (int j = 0; j < source.Triangles[s].Length; j++)
                    indices.Add(source.Triangles[s][j]);

                mesh.SetTriangles(indices, s);
            }

            // バウンディングボックスの更新
            mesh.RecalculateBounds();

            resultMeshes[i] = mesh;
        }

        return resultMeshes;
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

            // 修正ポイント：第3引数の 'stream' を 0, 1, 2 と分けて指定する
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