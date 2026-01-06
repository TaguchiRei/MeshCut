
/*using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct BurstMeshCutL : IJob
{
    // 外部から渡すデータ
    [ReadOnly] public BaseMeshData BaseMeshData;
    public float3 BladePosition;
    public float3 BladeNormal;
    public int ConnectionCapacity;

    // 外部で生成して渡す
    public NativeList<NativeBreakMeshDataL> Results;

    // 内部で初期化
    private NativeBreakMeshDataL _leftMeshDataL;
    private NativeBreakMeshDataL _rightMeshDataL;
    private NativeParallelMultiHashMap<float3, float3> _capConnections;
    private NativeParallelMultiHashMap<int, int3> _overlapTriangles;
    private NativeArray<bool> _baseVerticesSide;
    private NativeTriangleData _triangleData;
    private float _bladeDistance;

    [BurstCompile]
    private void Cut()
    {
        //すべての頂点がどちら側なのかを調べる
        for (int i = 0; i < BaseMeshData.Vertices.Length; i++)
        {
            _baseVerticesSide[i] = math.dot(BaseMeshData.Vertices[i] - BladePosition, BladeNormal) > 0f;
        }

        //サブメッシュごとに左右を分けるためのループ
        for (int submesh = 0; submesh < BaseMeshData.SubMeshCount; submesh++)
        {
            var allTriangles = BaseMeshData.SubIndices;

            //サブメッシュ毎に処理を行う
            if (allTriangles.TryGetFirstValue(submesh, out int3 triangle, out var iterator))
            {
                //三角形が切断面に対してどちら側にあるか、あるいは重なっているかを調べる
                do
                {
                    var p1 = triangle.x;
                    var p2 = triangle.y;
                    var p3 = triangle.z;

                    var left = _baseVerticesSide[p1] || _baseVerticesSide[p2] || _baseVerticesSide[p3];
                    var right = !_baseVerticesSide[p1] || !_baseVerticesSide[p2] || !_baseVerticesSide[p3];
                    if (left && !right)
                    {
                        _leftMeshDataL.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    if (right && !left)
                    {
                        _rightMeshDataL.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    //面の左右にまたがっていたら切断対象として保持
                    _overlapTriangles.Add(submesh, new int3(p1, p2, p3));
                    //一周ごとに次の三角形を取り出す
                } while (allTriangles.TryGetNextValue(out triangle, ref iterator));
            }
        }

        _baseVerticesSide.Dispose();

        //切断を要する面をまとめて切断
        CutFaces();
        //ループを作って面を埋める
        CreateLoop();

        Results.Add(_leftMeshDataL);
        Results.Add(_rightMeshDataL);
        return;
    }

    [BurstCompile]
    private void CutFaces()
    {
        for (int submesh = 0; submesh < BaseMeshData.SubMeshCount; submesh++)
        {
            if (_overlapTriangles.TryGetFirstValue(submesh, out int3 triangle, out var iterator))
            {
                do
                {
                    var p1 = triangle.x;
                    var p2 = triangle.y;
                    var p3 = triangle.z;
                    CutFace(submesh, p1, p2, p3);
                } while (_overlapTriangles.TryGetNextValue(out triangle, ref iterator));
            }
        }
    }

    [BurstCompile]
    private void CutFace(int submesh, int index1, int index2, int index3)
    {
        NativeArray<float3> leftPoints = new NativeArray<float3>(2, Allocator.Temp);
        NativeArray<float3> leftNormals = new NativeArray<float3>(2, Allocator.Temp);
        NativeArray<float2> leftUvs = new NativeArray<float2>(2, Allocator.Temp);
        NativeArray<float3> rightPoints = new NativeArray<float3>(2, Allocator.Temp);
        NativeArray<float3> rightNormals = new NativeArray<float3>(2, Allocator.Temp);
        NativeArray<float2> rightUvs = new NativeArray<float2>(2, Allocator.Temp);

        bool setLeft = false;
        bool setRight = false;

        int p = index1;

        for (int side = 0; side < 3; side++)
        {
            switch (side)
            {
                case 0:
                    p = index1;
                    break;
                case 1:
                    p = index2;
                    break;
                case 2:
                    p = index3;
                    break;
            }

            //頂点を左右どちらにあるかで分ける
            if (_baseVerticesSide[p])
            {
                // すでに左側の頂点が設定されているか（3頂点が左右に振り分けられるため、必ず左右どちらかは2つの頂点を持つことになる）
                if (!setLeft)
                {
                    setLeft = true;
                    // 頂点およびUV、法線の設定
                    leftPoints[0] = BaseMeshData.Vertices[p];
                    leftUvs[0] = BaseMeshData.Uvs[p];
                    leftNormals[0] = BaseMeshData.Normals[p];

                    //アクセスされる可能性のある[1]に値を複製
                    leftPoints[1] = leftPoints[0];
                    leftUvs[1] = leftUvs[0];
                    leftNormals[1] = leftNormals[0];
                }
                else
                {
                    // 2頂点目の場合は2番目に直接頂点情報を設定する
                    leftPoints[1] = BaseMeshData.Vertices[p];
                    leftUvs[1] = BaseMeshData.Uvs[p];
                    leftNormals[1] = BaseMeshData.Normals[p];
                }
            }
            else
            {
                // 左と同様の操作を右にも行う
                if (!setRight)
                {
                    setRight = true;

                    rightPoints[0] = BaseMeshData.Vertices[p];
                    rightUvs[0] = BaseMeshData.Uvs[p];
                    rightNormals[0] = BaseMeshData.Normals[p];

                    rightPoints[1] = rightPoints[0];
                    rightUvs[1] = rightUvs[0];
                    rightNormals[1] = rightNormals[0];
                }
                else
                {
                    rightPoints[1] = BaseMeshData.Vertices[p];
                    rightUvs[1] = BaseMeshData.Uvs[p];
                    rightNormals[1] = BaseMeshData.Normals[p];
                }
            }
        }

        #region 新規頂点を生成

        float3 dir1 = rightPoints[0] - leftPoints[0];
        float dot1 = math.dot(BladeNormal, dir1);
        float t1 = 0.5f;

        if (math.abs(dot1) > 0.000001f)
        {
            t1 = (-math.dot(BladeNormal, leftPoints[0]) - _bladeDistance) / dot1;
        }

        t1 = math.clamp(t1, 0f, 1f); // 0~1の範囲に収める

        float3 newVertex1 = leftPoints[0] + dir1 * t1;
        float2 newUv1 = leftUvs[0] + (rightUvs[0] - leftUvs[0]) * t1;
        float3 newNormal1 = leftNormals[0] + (rightNormals[0] - leftNormals[0]) * t1;

        float3 dir2 = rightPoints[1] - leftPoints[1];
        float dot2 = math.dot(BladeNormal, dir2);
        float t2 = 0.5f;
        if (math.abs(dot2) > 0.000001f)
        {
            t2 = (-math.dot(BladeNormal, leftPoints[1]) - _bladeDistance) / dot2;
        }

        t2 = math.clamp(t2, 0f, 1f);

        float3 newVertex2 = leftPoints[1] + dir2 * t2;
        float2 newUv2 = leftUvs[1] + (rightUvs[1] - leftUvs[1]) * t2;
        float3 newNormal2 = leftNormals[1] + (rightNormals[1] - leftNormals[1]) * t2;

        #endregion

        //辺関係を保存
        _capConnections.Add(newVertex1, newVertex2);
        _capConnections.Add(newVertex2, newVertex1);

        bool leftDoubleCheck = false;

        _triangleData.SetVertices(leftPoints[0], newVertex1, newVertex2);
        _triangleData.SetNormals(leftNormals[0], newNormal1, newNormal2);
        _triangleData.SetUvs(leftUvs[0], newUv1, newUv2);

        _leftMeshDataL.AddTriangle(_triangleData, newNormal1, submesh);

        if (!math.all(leftPoints[0] == leftPoints[1]))
        {
            _triangleData.SetVertices(leftPoints[0], leftPoints[1], newVertex2);
            _triangleData.SetNormals(leftNormals[0], leftNormals[1], newNormal2);
            _triangleData.SetUvs(leftUvs[0], leftUvs[1], newUv2);
            _leftMeshDataL.AddTriangle(_triangleData, newNormal2, submesh);
            leftDoubleCheck = true;
        }

        _triangleData.SetVertices(rightPoints[0], newVertex1, newVertex2);
        _triangleData.SetNormals(rightNormals[0], newNormal1, newNormal2);
        _triangleData.SetUvs(rightUvs[0], newUv1, newUv2);
        _rightMeshDataL.AddTriangle(_triangleData, newNormal1, submesh);

        if (!leftDoubleCheck)
        {
            _triangleData.SetVertices(rightPoints[0], rightPoints[1], newVertex2);
            _triangleData.SetNormals(rightNormals[0], rightNormals[1], newNormal2);
            _triangleData.SetUvs(rightUvs[0], rightUvs[1], newUv2);
            _rightMeshDataL.AddTriangle(_triangleData, newNormal2, submesh);
        }

        leftPoints.Dispose();
        leftNormals.Dispose();
        leftUvs.Dispose();
        rightPoints.Dispose();
        rightNormals.Dispose();
        rightUvs.Dispose();
    }

    [BurstCompile]
    private void CreateLoop()
    {
        NativeList<float3> visited = new(20, Allocator.Temp);
        using var keys = _capConnections.GetKeyArray(Allocator.Temp);

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (visited.Contains(key)) continue;
            NativeList<float3> polygon = new(Allocator.Temp);
            float3 current = key;
            bool loopClosed = false;

            while (true)
            {
                polygon.Add(current);
                visited.Add(current);
                float3 next = new float3(float.NaN);
                bool foundNext = false;
                if (_capConnections.TryGetFirstValue(current, out float3 neighbor, out var iterator))
                {
                    do
                    {
                        if (!visited.Contains(neighbor))
                        {
                            next = neighbor;
                            foundNext = true;
                            break;
                        }
                        // もし隣接が「開始地点」ならループ完成
                        else if (neighbor.Equals(key) && polygon.Length > 2)
                        {
                            loopClosed = true;
                        }
                    } while (_capConnections.TryGetNextValue(out neighbor, ref iterator));
                }

                if (!foundNext) break;
                current = next;
            }

            if (polygon.Length > 3)
            {
                ExecuteEarClipping(polygon);
            }

            polygon.Dispose();
        }
    }

    [BurstCompile]
    private void ExecuteEarClipping(NativeList<float3> polygon)
    {
        int vertexCount = polygon.Length;
        if (vertexCount < 3) return;

        //UV座標系を作成
        float3 absoluteNormal = math.abs(BladeNormal);
        float3 helper = absoluteNormal.x < 0.9f ? new float3(1, 0, 0) : new float3(0, 1, 0);
        float3 u = math.normalize(math.cross(BladeNormal, helper));
        float3 v = math.cross(BladeNormal, u);
        float3 center = float3.zero;
        for (int i = 0; i < vertexCount; i++)
        {
            center += polygon[i];
        }

        center /= vertexCount;

        NativeArray<float2> uvPosition = new(vertexCount, Allocator.Temp);
        for (int i = 0; i < vertexCount; i++)
        {
            uvPosition[i] = new(math.dot(polygon[i], u), math.dot(polygon[i], v));
        }

        int leftIndex = _leftMeshDataL.SubIndices.Count();
        int rightIndex = _rightMeshDataL.SubIndices.Count();


        // 3D頂点を2D座標(UV)に投影
        float3 origin = polygon[0];
        NativeArray<float2> points2D = new NativeArray<float2>(vertexCount, Allocator.Temp);
        NativeList<int> activeIndices = new NativeList<int>(vertexCount, Allocator.Temp);

        for (int i = 0; i < vertexCount; i++)
        {
            float3 rel = polygon[i] - origin;
            points2D[i] = new float2(math.dot(rel, u), math.dot(rel, v));
            activeIndices.Add(i);
        }

        //面積を確認し、時計回りなら反転させる
        float area = 0;
        for (int i = 0; i < vertexCount; i++)
        {
            int next = (i + 1) % vertexCount;
            area += (points2D[i].x * points2D[next].y) - (points2D[next].x * points2D[i].y);
        }

        if (area < 0)
        {
            // インデックスを反転してCCW（反時計回り）として扱う
            for (int i = 0; i < activeIndices.Length / 2; i++)
            {
                (activeIndices[i], activeIndices[activeIndices.Length - 1 - i]) =
                    (activeIndices[activeIndices.Length - 1 - i], activeIndices[i]);
            }
        }

        // 4. 耳切メインループ
        int timeout = activeIndices.Length * 2; // 無限ループ防止用
        while (activeIndices.Length > 3 && timeout > 0)
        {
            timeout--;
            bool earFound = false;

            for (int i = 0; i < activeIndices.Length; i++)
            {
                int prevIdx = activeIndices[GetWrappedIndex(i - 1, activeIndices.Length)];
                int currIdx = activeIndices[i];
                int nextIdx = activeIndices[GetWrappedIndex(i + 1, activeIndices.Length)];

                if (IsEar(prevIdx, currIdx, nextIdx, activeIndices, points2D))
                {
                    //左側の三角形を登録
                    _triangleData.SetVertices(polygon[prevIdx], polygon[currIdx], polygon[nextIdx]);
                    _triangleData.SetNormals(-BladeNormal, -BladeNormal, -BladeNormal);
                    _triangleData.SetUvs(uvPosition[prevIdx], uvPosition[currIdx], uvPosition[nextIdx]);
                    _leftMeshDataL.AddTriangle(_triangleData, -BladeNormal, leftIndex);

                    //右側の三角形を登録
                    _triangleData.SetNormals(BladeNormal, BladeNormal, BladeNormal);
                    _rightMeshDataL.AddTriangle(_triangleData, BladeNormal, rightIndex);

                    // 耳を切り落とす
                    activeIndices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound) break;
        }

        // 最後に残った3点を追加
        if (activeIndices.Length == 3)
        {
            _triangleData.SetVertices(polygon[activeIndices[0]], polygon[activeIndices[1]], polygon[activeIndices[2]]);
            _triangleData.SetNormals(BladeNormal, BladeNormal, BladeNormal);
            _triangleData.SetUvs(uvPosition[activeIndices[0]], uvPosition[activeIndices[1]],
                uvPosition[activeIndices[2]]);
            _rightMeshDataL.AddTriangle(_triangleData, BladeNormal, rightIndex);

            _triangleData.SetNormals(-BladeNormal, -BladeNormal, -BladeNormal);
            _leftMeshDataL.AddTriangle(_triangleData, -BladeNormal, leftIndex);
        }

        uvPosition.Dispose();
        points2D.Dispose();
        activeIndices.Dispose();
    }


    #region ヘルパー関数

    private static int GetWrappedIndex(int i, int length) => (i % length + length) % length;

    private static bool IsEar(int p, int c, int n, NativeList<int> activeIndices, NativeArray<float2> points2D)
    {
        float2 a = points2D[p];
        float2 b = points2D[c];
        float2 d = points2D[n];

        // 凸判定: 外積が正なら左に曲がっている（CCWにおいて凸）
        if ((b.x - a.x) * (d.y - a.y) - (b.y - a.y) * (d.x - a.x) <= 0) return false;

        // 三角形内に他の頂点が含まれていないかチェック
        for (int i = 0; i < activeIndices.Length; i++)
        {
            int testIdx = activeIndices[i];
            if (testIdx == p || testIdx == c || testIdx == n) continue;

            if (IsPointInTriangle(points2D[testIdx], a, b, d)) return false;
        }

        return true;
    }

    private static bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
    {
        float d1 = CrossProduct(a, b, p);
        float d2 = CrossProduct(b, c, p);
        float d3 = CrossProduct(c, a, p);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float CrossProduct(float2 a, float2 b, float2 c)
        => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    #endregion

    public void Execute()
    {
        // 1. 内部ワーク用データの初期化 (Allocator.Temp で高速化)
        _bladeDistance = -math.dot(BladeNormal, BladePosition);
        _baseVerticesSide = new NativeArray<bool>(BaseMeshData.Vertices.Length, Allocator.Temp);
        _triangleData = new NativeTriangleData();

        // 結果格納用のMeshDataを初期化 (Resultsに追加するため、ここではTempを使う)
        _leftMeshDataL = new NativeBreakMeshDataL(BaseMeshData, Allocator.Temp);
        _rightMeshDataL = new NativeBreakMeshDataL(BaseMeshData, Allocator.Temp);
        _capConnections = new NativeParallelMultiHashMap<float3, float3>(ConnectionCapacity, Allocator.Temp);
        _overlapTriangles = new NativeParallelMultiHashMap<int, int3>(ConnectionCapacity, Allocator.Temp);

        // 2. メインロジックの実行
        Cut();

        // 3. 結果を外部のリストに格納
        // (注: NativeBreakMeshDataが内部でNativeArrayを持つ場合、
        // 呼び出し側で適切にDisposeできるようコピー管理に注意してください)
        Results.Add(_leftMeshDataL);
        Results.Add(_rightMeshDataL);

        // 4. 解放 (Allocator.Tempなので自動ですが明示的に)
        _baseVerticesSide.Dispose();
        _capConnections.Dispose();
        _overlapTriangles.Dispose();
    }
}
*/