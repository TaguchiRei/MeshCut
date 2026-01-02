using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct BurstMeshCut
{
    private NativeBreakMeshData _leftMeshData;
    private NativeBreakMeshData _rightMeshData;

    /// <summary> 切断面の頂点同士のつながりを保存する </summary>
    private NativeParallelMultiHashMap<float3, float3> _capConnections;

    /// <summary> 面と重なった三角形を保存する </summary>
    private NativeParallelMultiHashMap<int, int3> _overlapTriangles;

    private float3 _bladePosition;
    private float3 _bladeNormal;
    private float _bladeDistance;

    private BaseMeshData _baseMeshData;

    /// <summary> 全頂点の方向を保存する </summary>
    private NativeArray<bool> _baseVerticesSide;

    private NativeTriangleData _triangleData;


    [BurstCompile]
    public NativeBreakMeshData[] Cut(BaseMeshData baseMesh, float3 bladePosition, float3 bladeNormal,
        int connectionCapacity)
    {
        //フィールド変数初期化
        _bladePosition = bladePosition;
        _bladeNormal = bladeNormal;
        _bladeDistance = -math.dot(_bladeNormal, _bladePosition);
        _baseMeshData = baseMesh;
        //切断後メッシュを入れる構造体の生成(数フレームかかる可能性を考慮してTempJob)
        _leftMeshData = new NativeBreakMeshData(baseMesh, Allocator.TempJob);
        _rightMeshData = new NativeBreakMeshData(baseMesh, Allocator.TempJob);
        _capConnections = new NativeParallelMultiHashMap<float3, float3>(connectionCapacity, Allocator.TempJob);

        //すべての頂点がどちら側なのかを調べる
        for (int i = 0; i < baseMesh.Vertices.Length; i++)
        {
            _baseVerticesSide[i] = math.dot(baseMesh.Vertices[i] - _bladePosition, _bladeNormal) > 0f;
        }

        //サブメッシュごとに左右を分けるためのループ
        for (int submesh = 0; submesh < _baseMeshData.SubMeshCount; submesh++)
        {
            var allTriangles = _baseMeshData.SubIndices;

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
                        _leftMeshData.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    if (right && !left)
                    {
                        _rightMeshData.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    //面の左右にまたがっていたら切断対象として保持
                    _overlapTriangles.Add(submesh, new int3(p1, p2, p3));
                    //一周ごとに次の三角形を取り出す
                } while (allTriangles.TryGetNextValue(out triangle, ref iterator));
            }
        }

        //切断を要する面をまとめて切断
        CutFaces();

        //エラーを出さないための狩りの戻り値。完成時はBreakMeshData[]を返す
        return default;
    }

    [BurstCompile]
    private void CutFaces()
    {
        for (int submesh = 0; submesh < _baseMeshData.SubMeshCount; submesh++)
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
                    leftPoints[0] = _baseMeshData.Vertices[p];
                    leftUvs[0] = _baseMeshData.Uvs[p];
                    leftNormals[0] = _baseMeshData.Normals[p];

                    //アクセスされる可能性のある[1]に値を複製
                    leftPoints[1] = leftPoints[0];
                    leftUvs[1] = leftUvs[0];
                    leftNormals[1] = leftNormals[0];
                }
                else
                {
                    // 2頂点目の場合は2番目に直接頂点情報を設定する
                    leftPoints[1] = _baseMeshData.Vertices[p];
                    leftUvs[1] = _baseMeshData.Uvs[p];
                    leftNormals[1] = _baseMeshData.Normals[p];
                }
            }
            else
            {
                // 左と同様の操作を右にも行う
                if (!setRight)
                {
                    setRight = true;

                    rightPoints[0] = _baseMeshData.Vertices[p];
                    rightUvs[0] = _baseMeshData.Uvs[p];
                    rightNormals[0] = _baseMeshData.Normals[p];

                    rightPoints[1] = rightPoints[0];
                    rightUvs[1] = rightUvs[0];
                    rightNormals[1] = rightNormals[0];
                }
                else
                {
                    rightPoints[1] = _baseMeshData.Vertices[p];
                    rightUvs[1] = _baseMeshData.Uvs[p];
                    rightNormals[1] = _baseMeshData.Normals[p];
                }
            }
        }

        #region 新規頂点を生成

        float3 dir1 = rightPoints[0] - leftPoints[0];
        float dot1 = math.dot(_bladeNormal, dir1);
        float t1 = 0.5f;

        if (math.abs(dot1) > 0.000001f)
        {
            t1 = (-math.dot(_bladeNormal, leftPoints[0]) - _bladeDistance) / dot1;
        }

        t1 = math.clamp(t1, 0f, 1f); // 0~1の範囲に収める

        float3 newVertex1 = leftPoints[0] + dir1 * t1;
        float2 newUv1 = leftUvs[0] + (rightUvs[0] - leftUvs[0]) * t1;
        float3 newNormal1 = leftNormals[0] + (rightNormals[0] - leftNormals[0]) * t1;

        float3 dir2 = rightPoints[1] - leftPoints[1];
        float dot2 = math.dot(_bladeNormal, dir2);
        float t2 = 0.5f;
        if (math.abs(dot2) > 0.000001f)
        {
            t2 = (-math.dot(_bladeNormal, leftPoints[1]) - _bladeDistance) / dot2;
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

        _leftMeshData.AddTriangle(_triangleData, newNormal1, submesh);

        if (!math.all(leftPoints[0] == leftPoints[1]))
        {
            _triangleData.SetVertices(leftPoints[0], leftPoints[1], newVertex2);
            _triangleData.SetNormals(leftNormals[0], leftNormals[1], newNormal2);
            _triangleData.SetUvs(leftUvs[0], leftUvs[1], newUv2);
            _leftMeshData.AddTriangle(_triangleData, newNormal2, submesh);
            leftDoubleCheck = true;
        }

        _triangleData.SetVertices(rightPoints[0], newVertex1, newVertex2);
        _triangleData.SetNormals(rightNormals[0], newNormal1, newNormal2);
        _triangleData.SetUvs(rightUvs[0], newUv1, newUv2);
        _rightMeshData.AddTriangle(_triangleData, newNormal1, submesh);

        if (!leftDoubleCheck)
        {
            _triangleData.SetVertices(rightPoints[0], rightPoints[1], newVertex2);
            _triangleData.SetNormals(rightNormals[0], rightNormals[1], newNormal2);
            _triangleData.SetUvs(rightUvs[0], rightUvs[1], newUv2);
            _rightMeshData.AddTriangle(_triangleData, newNormal2, submesh);
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
                if (_capConnections.TryGetFirstValue(key, out float3 neighbor, out var iterator))
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

    private void ExecuteEarClipping(NativeList<float3> polygon)
    {
        
    }
}