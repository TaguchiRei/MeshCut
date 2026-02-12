using System.Collections.Generic;
using System.Linq;
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

    public void Cut(BreakableObject[] breakables, NativePlane blade)
    {
        Complete = true;
        _cutTask = CutAsync(breakables, blade, _batchCount, _sampling);
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

    private UniTask CutAsync(BreakableObject[] breakables, NativePlane blade, int batchCount, int sampling)
    {
        Mesh[] mesh = new Mesh[breakables.Length];
        MultiCutContext context = new MultiCutContext(breakables.Length);

        //MeshDataArrayを取得
        for (int i = 0; i < breakables.Length; i++)
        {
            mesh[i] = breakables[i].Mesh;
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
            new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.BaseNormals = new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.BaseUvs = new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.BaseVertexSide = new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        context.Blades =
            new(context.BaseMeshDataArray.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.VertexObjectIndex =
            new(totalVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        int startIndex = 0;
        NativeList<float3> vertexAndNormalBuffer = new NativeList<float3>(maxVerticesCount, Allocator.Temp);
        NativeList<float2> uvBuffer = new NativeList<float2>(maxVerticesCount, Allocator.Temp);

        //オブジェクト毎にループする初期化を行う
        for (int i = 0; i < context.BaseMeshDataArray.Length; i++)
        {
            #region Base頂点配列初期化

            var data = context.BaseMeshDataArray[i];
            vertexAndNormalBuffer.ResizeUninitialized(data.vertexCount);
            //NativeSliceには直接書き込めない
            data.GetVertices(vertexAndNormalBuffer.AsArray().Reinterpret<Vector3>());
            context.BaseVertices.Slice(startIndex, data.vertexCount).CopyFrom(vertexAndNormalBuffer.AsArray());
            data.GetNormals(vertexAndNormalBuffer.AsArray().Reinterpret<Vector3>());
            context.BaseNormals.Slice(startIndex, data.vertexCount).CopyFrom(vertexAndNormalBuffer.AsArray());
            data.GetUVs(0, vertexAndNormalBuffer.AsArray().Reinterpret<Vector2>());
            context.BaseUvs.Slice(startIndex, data.vertexCount).CopyFrom(uvBuffer.AsArray());

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

        vertexAndNormalBuffer.Dispose();
        uvBuffer.Dispose();

        #region 頂点のサイドを取得

        var vertexGetSideJob = new VertexGetSideJob
        {
            Vertices = context.BaseVertices,
            BladeIndex = context.VertexObjectIndex,
            Blades = context.Blades,
            VertexSides = context.BaseVertexSide
        };

        JobHandle vertexGetSideHandle = vertexGetSideJob.Schedule(context.BaseVertices.Length, batchCount);
        vertexGetSideHandle.Complete();

        #endregion

        #region 左右分け

        List<BurstBreakMesh> breakMeshes = new();
        List<int> triangleObjectTable = new();
        context.CutFaces = new();
        context.CutFaceSubmeshId = new();
        context.TriangleObjectIndex = new();

        var vertices = context.BaseVertices;
        var normals = context.BaseNormals;
        var uvs = context.BaseUvs;

        //オブジェクト数分ループ
        for (int objIndex = 0; objIndex < context.BaseMeshDataArray.Length; objIndex++)
        {
            var objectStartIndex = context.StartIndex;
            var meshData = context.BaseMeshDataArray[objIndex];
            var triangles = meshData.GetIndexData<int>();
            BurstBreakMesh frontSide = new BurstBreakMesh(meshData.vertexCount);
            BurstBreakMesh backSide = new BurstBreakMesh(meshData.vertexCount);

            //サブメッシュ数分ループ
            for (int submesh = 0; submesh < meshData.subMeshCount; submesh++)
            {
                SubMeshDescriptor subMeshDesc = meshData.GetSubMesh(submesh);
                int start = subMeshDesc.indexStart;
                int count = subMeshDesc.indexCount;

                var indexData = triangles.GetSubArray(start, count);

                //三角形ごとにループ
                for (int i = 0; i < indexData.Length; i += 3)
                {
                    //ここで取得できるのはオブジェクトごとのインデックス番号なので、開始位置でオフセットを書ける
                    var p1 = indexData[i + 0] + objectStartIndex[objIndex];
                    var p2 = indexData[i + 1] + objectStartIndex[objIndex];
                    var p3 = indexData[i + 2] + objectStartIndex[objIndex];

                    int result =
                        (1 << context.BaseVertexSide[p1]) |
                        (1 << context.BaseVertexSide[p2]) |
                        (1 << context.BaseVertexSide[p3]);

                    switch (result)
                    {
                        case 0: //0なら裏側
                            backSide.AddTriangleLegacyIndex(
                                p1, p2, p3,
                                vertices[p1], vertices[p2], vertices[p3],
                                normals[p1], normals[p2], normals[p3],
                                uvs[p1], uvs[p2], uvs[p3],
                                submesh);
                            break;
                        case 7:
                            frontSide.AddTriangleLegacyIndex(
                                p1, p2, p3,
                                vertices[p1], vertices[p2], vertices[p3],
                                normals[p1], normals[p2], normals[p3],
                                uvs[p1], uvs[p2], uvs[p3],
                                submesh);
                            break;
                        default:
                            triangleObjectTable.Add(objIndex);
                            context.CutFaces.Add(new(p1, p2, p3));
                            context.CutStatus.Add(result);
                            context.CutFaceSubmeshId.Add(submesh);
                            context.TriangleObjectIndex.Add(objIndex);
                            break;
                    }
                }
            }

            breakMeshes.Add(frontSide);
            breakMeshes.Add(backSide);
        }

        #endregion

        #region 三角形の分割

        int triangleCount = triangleObjectTable.Count;
        context.NewVertices = new(triangleCount * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.NewNormals = new(triangleCount * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.NewUvs = new(triangleCount * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.NewTriangles = new(triangleCount * 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        context.CutEdges = new(triangleCount, Allocator.TempJob);

        var triangleCutJob = new TriangleCutJob
        {
            CutFaces = context.CutFaces.AsArray(),
            CutStatus = context.CutStatus.AsArray(),
            CutFaceSubmeshId = context.CutFaceSubmeshId.AsArray(),
            Blades = context.Blades,
            TriangleObjectIndex = context.TriangleObjectIndex.AsArray(),
            BaseNormals = context.BaseNormals,
            BaseUvs = context.BaseUvs,

            NewVertices = context.NewVertices,
            NewNormals = context.NewNormals,
            NewUvs = context.NewUvs,
            NewTriangles = context.NewTriangles,
            CutEdges = context.CutEdges.AsParallelWriter()
        };

        JobHandle triangleCutHandle = triangleCutJob.Schedule(triangleCount, batchCount);

        triangleCutHandle.Complete();

        #endregion

        #region 切断面の穴埋め処理

        //切断した三角形を追加
        for (int i = 0; i < context.NewTriangles.Length; i++)
        {
            var nt = context.NewTriangles[i];
            int objIdx = context.TriangleObjectIndex[i / 3];
            var target = breakMeshes[objIdx * 2 + (nt.Side == 1 ? 0 : 1)];
            AddNewTriangle(target, nt, context);
        }

        // ループ抽出と耳切り法
        var allLoops = FindAllLoops(context, breakables.Length);
        for (int i = 0; i < breakables.Length; i++)
        {
            breakMeshes[i * 2].AddSubmesh(); // 断面用サブメッシュ
            breakMeshes[i * 2 + 1].AddSubmesh();
            foreach (var loop in allLoops[i])
            {
                FillCapForLoop(i, loop, context, breakMeshes[i * 2], true);
                FillCapForLoop(i, loop, context, breakMeshes[i * 2 + 1], false);
            }
        }

        #endregion

        List<List<Vector3>> colliderVerticesPerFragment = new();

        for (int i = 0; i < breakMeshes.Count; i++)
        {
            var source = breakMeshes[i];
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

        CutMesh = FinalizeMeshes(breakMeshes);
        SamplingPoints = colliderVerticesPerFragment;
        context.Dispose();

        return default;
    }

    private void AddNewTriangle(BurstBreakMesh target, NewTriangle nt, MultiCutContext context)
    {
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

    private List<List<int>>[] FindAllLoops(MultiCutContext context, int count)
    {
        var results = new List<List<int>>[count];
        for (int i = 0; i < count; i++) results[i] = new();
        if (!context.CutEdges.IsCreated) return results;

        var visited = new HashSet<int>();
        var keys = context.CutEdges.GetKeyArray(Allocator.Temp);
        foreach (var k in keys)
        {
            if (visited.Contains(k)) continue;
            var loop = new List<int>();
            int curr = k;
            while (!visited.Contains(curr) && context.CutEdges.TryGetValue(curr, out int next))
            {
                visited.Add(curr);
                loop.Add(curr);
                curr = next;
            }

            if (loop.Count >= 3) results[context.TriangleObjectIndex[loop[0] / 2]].Add(loop);
        }

        return results;
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
        List<int> resultIndices = FastEarClipping.Triangulate(projectedVertices);

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

    private Mesh[] FinalizeMeshes(List<BurstBreakMesh> breakMeshes)
    {
        int fragmentCount = breakMeshes.Count;
        var writableDataArray = Mesh.AllocateWritableMeshData(fragmentCount);

        for (int i = 0; i < fragmentCount; i++)
        {
            var source = breakMeshes[i];
            var data = writableDataArray[i];

            // 頂点属性の設定（Position, Normal, UV）
            int vertexCount = source.Vertices.Length;
            data.SetVertexBufferParams(vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));

            var vertices = data.GetVertexData<float3>(0);
            var normals = data.GetVertexData<float3>(1);
            var uvs = data.GetVertexData<float2>(2);

            // NativeListからコピー
            vertices.CopyFrom(source.Vertices.AsArray());
            normals.CopyFrom(source.Normals.AsArray());
            uvs.CopyFrom(source.Uvs.AsArray());

            // サブメッシュとインデックスの設定
            int totalIndexCount = 0;
            foreach (var subTri in source.Triangles) totalIndexCount += subTri.Count();

            data.SetIndexBufferParams(totalIndexCount, IndexFormat.UInt32);
            var indices = data.GetIndexData<int>();

            int indexOffset = 0;
            data.subMeshCount = source.Triangles.Count;
            for (int s = 0; s < source.Triangles.Count; s++)
            {
                int subCount = source.Triangles[s].Count();
                // List<int> から NativeArray へのコピーは少し工夫が必要（あるいは source.Triangles を最初から NativeList にするか）
                var subIndices = source.Triangles[s];
                for (int j = 0; j < subCount; j++) indices[indexOffset + j] = subIndices[j];

                data.SetSubMesh(s, new SubMeshDescriptor(indexOffset, subCount), MeshUpdateFlags.DontRecalculateBounds);
                indexOffset += subCount;
            }
        }

        // 新しいメッシュを作成して適用
        Mesh[] resultMeshes = new Mesh[fragmentCount];
        for (int i = 0; i < fragmentCount; i++) resultMeshes[i] = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(writableDataArray, resultMeshes);

        return resultMeshes;
    }
}