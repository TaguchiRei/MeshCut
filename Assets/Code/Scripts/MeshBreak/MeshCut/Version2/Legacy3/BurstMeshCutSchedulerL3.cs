using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

/// <summary>
/// メッシュカットのスケジュールを担当する
/// </summary>
public class BurstMeshCutSchedulerL3
{
    //三角形の分類パターンは８通り詳細はTriangleSideCountJobのコメントを参照
    private const int TRIANGLES_CLASSIFY = 8;

    private UniTask cutTask;

    private MeshCutContextL _heavyMeshCutContextL;

    public void Cut(NativePlane blade, NativeMeshDataL3[] meshData, int batchCount)
    {
        CutTaskAsync(blade, meshData, batchCount);
    }

    private void CutTaskAsync(NativePlane blade, NativeMeshDataL3[] meshData, int batchCount)
    {
        Stopwatch totalTime = new Stopwatch();
        totalTime.Start();
        // すべてのDisposeの必要がある物はここで宣言
        NativeArray<int2> arraysPath = default;
        NativeArray<float3> baseObjectPositions = default;
        NativeArray<float3> baseObjectScale = default;
        NativeArray<quaternion> baseObjectRotation = default;
        NativeArray<float3> baseVertices = default, baseNormals = default;
        NativeArray<float2> baseUvs = default;
        NativeArray<SubmeshTriangleL3> baseTriangles = default;

        //ベース配列の開始位置と長さ、オブジェクトIDをそれぞれ保存している
        NativeArray<int3> baseTriangleArrayData = default;

        //各種オブジェクトに対応させた切断面を保持している
        NativeArray<NativePlane> blades = default;
        //各頂点毎にどの切断面に対応しているかを保持(blades配列のインデックスを指す)
        NativeArray<int> vertexObjectIndex = default;
        NativeArray<int> vertSide = default;
        NativeArray<int> trianglesArrayNumber = default;
        //オブジェクトごとの三角形の開始位置を保持している
        NativeArray<int> trianglesObjectStartIndex = default;
        //生成された新規頂点群を保存する
        NativeArray<float3> newVertices = default;
        NativeArray<float3> newNormals = default;
        NativeArray<float2> newUvs = default;
        NativeArray<NewTriangleDataL3> newTriangle = default;

        NativeArray<int> activeResultVertexIndex = default;
        NativeArray<int> activeResultTriangleIndex = default;

        try
        {
            #region 処理全体で利用する配列初期化

            int vertexArrayLength = 0;
            int trianglesArrayLength = 0;

            for (int i = 0; i < meshData.Length; i++)
            {
                vertexArrayLength += meshData[i].Vertices.Length;
                trianglesArrayLength += meshData[i].Triangles.Length;
            }

            // メモリ確保 
            Stopwatch memoryGet = Stopwatch.StartNew();
            arraysPath = new(meshData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseObjectPositions = new(meshData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseObjectScale = new(meshData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseObjectRotation = new(meshData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            baseVertices = new(vertexArrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseNormals = new(vertexArrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseUvs = new(vertexArrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseTriangles = new(trianglesArrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            baseTriangleArrayData = new(meshData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            activeResultVertexIndex = new(trianglesArrayLength * 2, Allocator.TempJob);
            newVertices = new(trianglesArrayLength * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            newNormals = new(trianglesArrayLength * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            newUvs = new(trianglesArrayLength * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            activeResultTriangleIndex = new(trianglesArrayLength * 3, Allocator.TempJob);
            newTriangle = new(trianglesArrayLength * 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Debug.Log($"メモリ確保時間 : {memoryGet.ElapsedMilliseconds}ms");


            Stopwatch init = Stopwatch.StartNew();
            int vOffset = 0;
            int tOffset = 0;

            for (int i = 0; i < meshData.Length; i++)
            {
                var data = meshData[i];

                // Transform取得
                baseObjectPositions[i] = data.Transform.position;
                baseObjectScale[i] = data.Transform.localScale;
                baseObjectRotation[i] = data.Transform.rotation;
                arraysPath[i] = new int2(data.Vertices.Length, vOffset);

                // 頂点データコピー
                baseVertices.Slice(vOffset, data.Vertices.Length).CopyFrom(data.Vertices);
                baseNormals.Slice(vOffset, data.Vertices.Length).CopyFrom(data.Normals);
                baseUvs.Slice(vOffset, data.Vertices.Length).CopyFrom(data.Uvs);

                // 三角形データコピー
                baseTriangles.Slice(tOffset, data.Triangles.Length).CopyFrom(data.Triangles);
                baseTriangleArrayData[i] = new int3(tOffset, data.Triangles.Length, i);

                vOffset += data.Vertices.Length;
                tOffset += data.Triangles.Length;
            }

            #endregion

            #region 実際の処理

            #region 内部で使う一時配列初期化

            //各頂点がどのオブジェクトに対応しているかを保存。必然的に切断面とも対応が取れる
            //各頂点のインデックスと同じインデックスに保存される値が切断面配列と同じ値になる
            vertexObjectIndex = new(vertexArrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //各三角形がどのオブジェクトに対応しているかを保持する配列を初期化
            //各種三角形の頂点インデックスに三角形のインデックスと同じインデックスに保存される値を足すと結合後のインデックスが取得できる
            trianglesObjectStartIndex =
                new(baseTriangles.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var initializeArrayJob = new InitializeArrayJobL3()
            {
                ArrayPath = arraysPath,
                BaseTriangleArrayData = baseTriangleArrayData,
                VertexObjectIndex = vertexObjectIndex,
                TrianglesObjectStartIndex = trianglesObjectStartIndex,
            };
            JobHandle initializeArrayHandle = initializeArrayJob.Schedule();

            Debug.Log($"配列初期化時間{init.ElapsedMilliseconds}ms   meshData{meshData.Length}");

            #endregion

            #region 切断面初期化

            Stopwatch jobTime = Stopwatch.StartNew();

            //オブジェクトごとのローカル空間に移した切断面を生成する
            blades = new(meshData.Length, Allocator.TempJob);
            var bladeInitializeJob = new BladeInitializeJobL3
            {
                Blade = blade,
                Positions = baseObjectPositions,
                Quaternions = baseObjectRotation,
                Scales = baseObjectScale,
                LocalBlades = blades
            };

            JobHandle bladeInitHandle = bladeInitializeJob.Schedule(blades.Length, batchCount, initializeArrayHandle);

            #endregion

            #region 頂点が面に対してどちら方向か調べる

            vertSide = new(baseVertices.Length, Allocator.TempJob);
            var vertGetSideJob = new VertexGetSideJobL3
            {
                Vertices = baseVertices,
                Blades = blades,
                BladeIndex = vertexObjectIndex,
                Results = vertSide
            };

            JobHandle vertGetSideHandle = vertGetSideJob.Schedule(vertSide.Length, batchCount, bladeInitHandle);

            #endregion

            #region 面を左右に振り分け、重なっている物を探し出す

            trianglesArrayNumber = new(trianglesArrayLength, Allocator.TempJob);

            var triangleGetSideJob = new TriangleSideCountJobL3
            {
                Triangles = baseTriangles,
                VertexSide = vertSide,
                TrianglesGroupNumber = trianglesArrayNumber
            };

            JobHandle trianglesGetSideHandle =
                triangleGetSideJob.Schedule(baseTriangles.Length, batchCount, vertGetSideHandle);

            #endregion

            #region 新たに頂点を生成する

            var triangleCutJob = new TrianglesCutJobL3
            {
                BaseVertices = baseVertices,
                BaseNormals = baseNormals,
                BaseUvs = baseUvs,
                BaseTriangles = baseTriangles,
                Blades = blades,
                TrianglesObjectStartIndex = trianglesObjectStartIndex,
                VertexObjectIndex = vertexObjectIndex,
                TrianglesArrayNumber = trianglesArrayNumber,
                NewVertices = newVertices,
                NewNormals = newNormals,
                NewUvs = newUvs,
                NewTriangles = newTriangle,
                ActiveResultVertexIndex = activeResultVertexIndex,
                ActiveResultTriangleIndex = activeResultTriangleIndex,
            };

            JobHandle triangleCutHandle =
                triangleCutJob.Schedule(baseTriangles.Length, batchCount, trianglesGetSideHandle);
            triangleCutHandle.Complete();

            #endregion

            #endregion

            //最後のJob完了時点ですべてDisposeする
            arraysPath.Dispose(triangleCutHandle);
            baseObjectPositions.Dispose(triangleCutHandle);
            baseObjectScale.Dispose(triangleCutHandle);
            baseObjectRotation.Dispose(triangleCutHandle);
            baseVertices.Dispose(triangleCutHandle);
            baseNormals.Dispose(triangleCutHandle);
            baseUvs.Dispose(triangleCutHandle);
            baseTriangles.Dispose(triangleCutHandle);
            baseTriangleArrayData.Dispose(triangleCutHandle);
            blades.Dispose(triangleCutHandle);
            vertexObjectIndex.Dispose(triangleCutHandle);
            vertSide.Dispose(triangleCutHandle);
            trianglesArrayNumber.Dispose(triangleCutHandle);
            trianglesObjectStartIndex.Dispose(triangleCutHandle);
            newVertices.Dispose(triangleCutHandle);
            newNormals.Dispose(triangleCutHandle);
            newUvs.Dispose(triangleCutHandle);
            newTriangle.Dispose(triangleCutHandle);
            activeResultVertexIndex.Dispose(triangleCutHandle);
            activeResultTriangleIndex.Dispose(triangleCutHandle);

            Debug.Log($"Jobの総処理時間 : {jobTime.ElapsedMilliseconds}ms");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            if (arraysPath.IsCreated) arraysPath.Dispose();
            if (baseObjectPositions.IsCreated) baseObjectPositions.Dispose();
            if (baseObjectScale.IsCreated) baseObjectScale.Dispose();
            if (baseObjectRotation.IsCreated) baseObjectRotation.Dispose();
            if (baseVertices.IsCreated) baseVertices.Dispose();
            if (baseNormals.IsCreated) baseNormals.Dispose();
            if (baseUvs.IsCreated) baseUvs.Dispose();
            if (baseTriangles.IsCreated) baseTriangles.Dispose();
            if (baseTriangleArrayData.IsCreated) baseTriangleArrayData.Dispose();
            if (blades.IsCreated) blades.Dispose();
            if (vertexObjectIndex.IsCreated) vertexObjectIndex.Dispose();
            if (vertSide.IsCreated) vertSide.Dispose();
            if (trianglesArrayNumber.IsCreated) trianglesArrayNumber.Dispose();
            if (trianglesObjectStartIndex.IsCreated) trianglesObjectStartIndex.Dispose();
            if (newVertices.IsCreated) newVertices.Dispose();
            if (newNormals.IsCreated) newNormals.Dispose();
            if (newUvs.IsCreated) newUvs.Dispose();
            if (newTriangle.IsCreated) newTriangle.Dispose();
            if (activeResultVertexIndex.IsCreated) activeResultVertexIndex.Dispose();
            if (activeResultTriangleIndex.IsCreated) activeResultTriangleIndex.Dispose();
        }
        finally
        {
            totalTime.Stop();
            Debug.Log($"合計処理時間 : {totalTime.ElapsedMilliseconds}ms");
        }
    }
}