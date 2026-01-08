using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

/// <summary>
/// メッシュカットのスケジュールを担当する
/// </summary>
public class BurstMeshCutScheduler
{
    //三角形の分類パターンは８通り詳細はTriangleSideCountJobのコメントを参照
    private const int TRIANGLES_CLASSIFY = 8;
    private INativeMeshRepository meshRepository;

    private UniTask cutTask;

    public void Cut(NativePlane blade, NativeMeshData[] meshData, int batchCount)
    {
        cutTask = CutTaskAsync(blade, meshData, batchCount);
    }

    private async UniTask CutTaskAsync(NativePlane blade, NativeMeshData[] meshData, int batchCount)
    {
        Stopwatch totalTime = new Stopwatch();
        totalTime.Start();
        // すべてのDisposeの必要がある物はここで宣言
        NativeArray<int2> arraysPath = default;
        NativeArray<float3> positions = default, scale = default;
        NativeArray<quaternion> rotation = default;
        NativeArray<float3> baseVertices = default, baseNormals = default;
        NativeArray<float2> baseUvs = default;
        NativeArray<SubmeshTriangle> baseTriangles = default;
        NativeArray<int3> baseTrianglesStartLengthID = default;
        NativeArray<NativePlane> blades = default;
        NativeArray<int> bladesIndex = default;
        NativeArray<int> vertSide = default;
        NativeArray<int2> lengthAndStart = default;
        NativeArray<int> trianglesArrayNumber = default;

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
            arraysPath = new NativeArray<int2>(meshData.Length, Allocator.TempJob);
            positions = new NativeArray<float3>(meshData.Length, Allocator.TempJob);
            scale = new NativeArray<float3>(meshData.Length, Allocator.TempJob);
            rotation = new NativeArray<quaternion>(meshData.Length, Allocator.TempJob);

            baseVertices = new NativeArray<float3>(vertexArrayLength, Allocator.TempJob);
            baseNormals = new NativeArray<float3>(vertexArrayLength, Allocator.TempJob);
            baseUvs = new NativeArray<float2>(vertexArrayLength, Allocator.TempJob);
            baseTriangles = new NativeArray<SubmeshTriangle>(trianglesArrayLength, Allocator.TempJob);
            baseTrianglesStartLengthID = new NativeArray<int3>(meshData.Length, Allocator.TempJob);

            int vOffset = 0;
            int tOffset = 0;

            for (int i = 0; i < meshData.Length; i++)
            {
                var data = meshData[i];

                // Transform取得
                positions[i] = data.Transform.position;
                scale[i] = data.Transform.localScale;
                rotation[i] = data.Transform.rotation;
                arraysPath[i] = new int2(data.Vertices.Length, vOffset);

                // 頂点データコピー
                baseVertices.Slice(vOffset, data.Vertices.Length).CopyFrom(data.Vertices);
                baseNormals.Slice(vOffset, data.Vertices.Length).CopyFrom(data.Normals);
                baseUvs.Slice(vOffset, data.Vertices.Length).CopyFrom(data.Uvs);

                // 三角形データコピー
                baseTriangles.Slice(tOffset, data.Triangles.Length).CopyFrom(data.Triangles);
                baseTrianglesStartLengthID[i] = new int3(tOffset, data.Triangles.Length, i);

                vOffset += data.Vertices.Length;
                tOffset += data.Triangles.Length;
            }

            #endregion

            #region 実際の処理

            #region 切断面初期化

            blades = new(meshData.Length, Allocator.TempJob);
            bladesIndex = new(vertexArrayLength, Allocator.TempJob);
            int planeIdIndex = 0;
            for (int i = 0; i < arraysPath.Length; i++)
            {
                for (int j = 0; j < arraysPath[i].x; j++)
                {
                    bladesIndex[planeIdIndex++] = i;
                }
            }

            var bladeInitializeJob = new BladeInitializeJob
            {
                Blade = blade,
                Positions = positions,
                Quaternions = rotation,
                Scales = scale,
                LocalBlades = blades
            };

            JobHandle bladeInitHandle = bladeInitializeJob.Schedule(blades.Length, 64);

            #endregion

            #region VertexGetSide

            vertSide = new(baseVertices.Length, Allocator.TempJob);
            var vertGetSideJob = new VertexGetSideJob
            {
                Vertices = baseVertices,
                Blades = blades,
                BladeIndex = bladesIndex,
                Results = vertSide
            };

            JobHandle vertGetSideHandle = vertGetSideJob.Schedule(vertSide.Length, batchCount, bladeInitHandle);

            #endregion

            #region 面を左右に振り分け、重なっている物を探し出す

            lengthAndStart = new(TRIANGLES_CLASSIFY, Allocator.TempJob);
            trianglesArrayNumber = new(trianglesArrayLength, Allocator.TempJob);

            var triangleGetSideJob = new TriangleSideCountJob
            {
                Triangles = baseTriangles,
                VertexSide = vertSide,
                LengthAndStart = lengthAndStart,
                TrianglesArrayNumber = trianglesArrayNumber
            };

            JobHandle trianglesGetSideHandle = triangleGetSideJob.Schedule(vertGetSideHandle);
            
            trianglesGetSideHandle.Complete();

            #endregion

            #endregion
        }
        catch (System.Exception e)
        {
            Debug.LogException(e); // エラーが起きても finally へ行く
        }
        finally
        {
            // 3. 確保されたものを安全にすべて破棄
            if (arraysPath.IsCreated) arraysPath.Dispose();
            if (positions.IsCreated) positions.Dispose();
            if (scale.IsCreated) scale.Dispose();
            if (rotation.IsCreated) rotation.Dispose();
            if (baseVertices.IsCreated) baseVertices.Dispose();
            if (baseNormals.IsCreated) baseNormals.Dispose();
            if (baseUvs.IsCreated) baseUvs.Dispose();
            if (baseTriangles.IsCreated) baseTriangles.Dispose();
            if (baseTrianglesStartLengthID.IsCreated) baseTrianglesStartLengthID.Dispose();
            if (blades.IsCreated) blades.Dispose();
            if (bladesIndex.IsCreated) bladesIndex.Dispose();
            if (vertSide.IsCreated) vertSide.Dispose();
            if (lengthAndStart.IsCreated) lengthAndStart.Dispose();
            if (trianglesArrayNumber.IsCreated) trianglesArrayNumber.Dispose();

            totalTime.Stop();
            Debug.Log($"合計処理時間 : {totalTime.ElapsedMilliseconds}ms");
        }
    }
}

public interface INativeMeshRepository
{
    bool GetMesh(int hash, bool cutMesh, out NativeMeshData meshData);
}