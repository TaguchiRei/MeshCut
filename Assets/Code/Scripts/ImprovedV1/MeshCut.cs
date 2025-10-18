using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MeshCut : MonoBehaviour
{
    #region 切断した左右の形状を保持するためのクラス

    private class CutSide
    {
        public List<Vector3> vertices = new();
        public List<Vector3> normals = new();
        public List<Vector2> uvs = new();
        public List<int> triangles = new();
        public List<List<int>> subIndices = new();

        private readonly Dictionary<int, int> _addedVertices = new();

        public void ClearAll()
        {
            vertices.Clear();
            normals.Clear();
            uvs.Clear();
            triangles.Clear();
            subIndices.Clear();
            _addedVertices.Clear();
        }

        public void AddTriangle(int p1, int p2, int p3, int submesh)
        {
            int p1Index = GetOrAddVertex(p1);
            int p2Index = GetOrAddVertex(p2);
            int p3Index = GetOrAddVertex(p3);

            subIndices[submesh].Add(p1Index);
            subIndices[submesh].Add(p2Index);
            subIndices[submesh].Add(p3Index);

            triangles.Add(p1Index);
            triangles.Add(p2Index);
            triangles.Add(p3Index);
        }

        public void AddTriangle(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal, int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                (points3[1] - points3[0]).normalized,
                (points3[2] - points3[0]).normalized);

            int p1 = 0;
            int p2 = 1;
            int p3 = 2;

            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
                p1 = 2;
                p2 = 1;
                p3 = 0;
            }

            int baseIndex = vertices.Count;

            subIndices[submesh].Add(baseIndex + 0);
            subIndices[submesh].Add(baseIndex + 1);
            subIndices[submesh].Add(baseIndex + 2);

            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);

            vertices.Add(points3[p1]);
            vertices.Add(points3[p2]);
            vertices.Add(points3[p3]);

            normals.Add(normals3[p1]);
            normals.Add(normals3[p2]);
            normals.Add(normals3[p3]);

            uvs.Add(uvs3[p1]);
            uvs.Add(uvs3[p2]);
            uvs.Add(uvs3[p3]);
        }
        
        private int GetOrAddVertex(int index)
        {
            if (_addedVertices.TryGetValue(index, out var existingIndex))
            {
                return existingIndex;
            }

            int newIndex = vertices.Count;
            _addedVertices[index] = newIndex;

            vertices.Add(_targetMesh.vertices[index]);
            normals.Add(_targetMesh.normals[index]);
            uvs.Add(_targetMesh.uv[index]);

            return newIndex;
        }
    }

    #endregion

    private readonly CutSide _leftSide = new();
    private readonly CutSide _rightSide = new();
    private readonly List<Vector3> _newVertices = new();

    private Plane _blade;
    private static Mesh _targetMesh;

    private void Initialize()
    {
        _newVertices.Clear();
        _leftSide.ClearAll();
        _rightSide.ClearAll();
    }

    /// <summary>
    /// 対象のメッシュを切断する。
    /// </summary>
    /// <param name="target"></param>
    /// <param name="blade"></param>
    /// <param name="capMaterial"></param>
    /// <returns></returns>
    public GameObject[] Cut(GameObject target, Plane blade, Material capMaterial)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        Initialize();
        _blade = blade;

        _targetMesh = target.GetComponent<MeshFilter>().mesh;

        // 頂点を初期化
        _newVertices.Clear();
        _leftSide.ClearAll();
        _rightSide.ClearAll();

        //頂点画面の左右どちらにあるかの計算結果を保持するための変数群
        bool[] sides = new bool[3];
        int[] triangles;
        int p1, p2, p3;

        for (int submesh = 0; submesh < _targetMesh.subMeshCount; submesh++)
        {
            triangles = _targetMesh.GetTriangles(submesh);
            _leftSide.subIndices.Add(new List<int>());
            _rightSide.subIndices.Add(new List<int>());

            // サブメッシュのインデックス数分ループ
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // p1 - p3のインデックスを取得。つまりトライアングル
                p1 = triangles[i + 0];
                p2 = triangles[i + 1];
                p3 = triangles[i + 2];

                // 3頂点が面に対してどちら側にあるのかを得る
                sides[0] = _blade.GetSide(_targetMesh.vertices[p1]);
                sides[1] = _blade.GetSide(_targetMesh.vertices[p2]);
                sides[2] = _blade.GetSide(_targetMesh.vertices[p3]);

                if (sides[0] == sides[1] && sides[0] == sides[2])
                {
                    //頂点がすべて同じ側にある場合、単純に追加して終了
                    if (sides[0])
                    {
                        _leftSide.AddTriangle(p1, p2, p3, submesh);
                    }
                    else
                    {
                        _rightSide.AddTriangle(p1, p2, p3, submesh);
                    }
                }
                else
                {
                    // 三角形が切断面に重なっている場合、切断を実行
                    CutFace(submesh, sides, p1, p2, p3);
                }
            }
        }

        Material[] mats = target.GetComponent<MeshRenderer>().sharedMaterials;
        // 取得したマテリアル配列の最後のマテリアルが、カット面のマテリアルでない場合
        if (mats[^1].name != capMaterial.name)
        {
            _leftSide.subIndices.Add(new List<int>());
            _rightSide.subIndices.Add(new List<int>());
            Material[] newMats = new Material[mats.Length + 1];
            mats.CopyTo(newMats, 0);
            newMats[mats.Length] = capMaterial;
            mats = newMats;
        }
        
        Debug.Log($"左右に振り分け完了。所要時間{stopwatch.ElapsedMilliseconds}ms");

        Capping();
        
        Debug.Log($"切断面穴埋め完了。所要時間{stopwatch.ElapsedMilliseconds}ms");

        // Left Mesh
        // 左側のメッシュを生成
        // MeshCutSideクラスのメンバから各値をコピー
        Mesh leftMesh = new Mesh
        {
            name = "Split Mesh Left",
            vertices = _leftSide.vertices.ToArray(),
            triangles = _leftSide.triangles.ToArray(),
            normals = _leftSide.normals.ToArray(),
            uv = _leftSide.uvs.ToArray(),
            subMeshCount = _leftSide.subIndices.Count
        };

        for (int i = 0; i < _leftSide.subIndices.Count; i++)
        {
            leftMesh.SetIndices(_leftSide.subIndices[i].ToArray(), MeshTopology.Triangles, i);
        }


        // Right Mesh
        // 右側のメッシュも同様に生成
        Mesh rightMesh = new Mesh
        {
            name = "Split Mesh Right",
            vertices = _rightSide.vertices.ToArray(),
            triangles = _rightSide.triangles.ToArray(),
            normals = _rightSide.normals.ToArray(),
            uv = _rightSide.uvs.ToArray(),
            subMeshCount = _rightSide.subIndices.Count
        };

        for (int i = 0; i < _rightSide.subIndices.Count; i++)
        {
            rightMesh.SetIndices(_rightSide.subIndices[i].ToArray(), MeshTopology.Triangles, i);
        }


        // assign the game objects

        // 元のオブジェクトを左側のオブジェクトに
        target.name = "left side";
        target.GetComponent<MeshFilter>().mesh = leftMesh;


        // 右側のオブジェクトは新規作成
        GameObject leftSideObj = target;

        GameObject rightSideObj = new GameObject("right side", typeof(MeshFilter), typeof(MeshRenderer));
        rightSideObj.transform.position = target.transform.position;
        rightSideObj.transform.rotation = target.transform.rotation;
        rightSideObj.GetComponent<MeshFilter>().mesh = rightMesh;

        // assign mats
        // 新規生成したマテリアルリストをそれぞれのオブジェクトに適用する
        leftSideObj.GetComponent<MeshRenderer>().materials = mats;
        rightSideObj.GetComponent<MeshRenderer>().materials = mats;

        Debug.Log($"オブジェクト生成完了。所要時間{stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Stop();
        
        // 左右のGameObjectの配列を返す
        return new[] { leftSideObj, rightSideObj };
    }

    void CutFace(int submesh, bool[] sides, int index1, int index2, int index3)
    {
        Vector3[] leftPoints = new Vector3[2];
        Vector3[] leftNormals = new Vector3[2];
        Vector2[] leftUvs = new Vector2[2];
        Vector3[] rightPoints = new Vector3[2];
        Vector3[] rightNormals = new Vector3[2];
        Vector2[] rightUvs = new Vector2[2];

        bool didsetLeft = false;
        bool didsetRight = false;

        int p = index1;

        //各頂点を左右どちらにあるかで振り分ける
        for (int side = 0; side < 3; side++)
        {
            //どの頂点について処理するか決定
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
            if (sides[side])
            {
                // すでに左側の頂点が設定されているか（3頂点が左右に振り分けられるため、必ず左右どちらかは2つの頂点を持つことになる）
                if (!didsetLeft)
                {
                    didsetLeft = true;
                    // 頂点およびUV、法線の設定
                    leftPoints[0] = _targetMesh.vertices[p];
                    leftUvs[0] = _targetMesh.uv[p];
                    leftNormals[0] = _targetMesh.normals[p];

                    //アクセスされる可能性のある[1]に値を複製
                    leftPoints[1] = leftPoints[0];
                    leftUvs[1] = leftUvs[0];
                    leftNormals[1] = leftNormals[0];
                }
                else
                {
                    // 2頂点目の場合は2番目に直接頂点情報を設定する
                    leftPoints[1] = _targetMesh.vertices[p];
                    leftUvs[1] = _targetMesh.uv[p];
                    leftNormals[1] = _targetMesh.normals[p];
                }
            }
            else
            {
                // 左と同様の操作を右にも行う
                if (!didsetRight)
                {
                    didsetRight = true;

                    rightPoints[0] = _targetMesh.vertices[p];
                    rightUvs[0] = _targetMesh.uv[p];
                    rightNormals[0] = _targetMesh.normals[p];

                    rightPoints[1] = rightPoints[0];
                    rightUvs[1] = rightUvs[0];
                    rightNormals[1] = rightNormals[0];
                }
                else
                {
                    rightPoints[1] = _targetMesh.vertices[p];
                    rightUvs[1] = _targetMesh.uv[p];
                    rightNormals[1] = _targetMesh.normals[p];
                }
            }
        }

        // 分割された点の比率計算のための距離
        float normalizedDistance = 0f;

        // 距離
        float distance = 0f;


        #region 左側の処理

        // 左の点を基準に定義した面と交差する座標を探す。
        _blade.Raycast(new Ray(leftPoints[0], (rightPoints[0] - leftPoints[0]).normalized), out distance);

        // 見つかった交差点を、頂点間の距離で割ることで、分割点の左右の割合を算出する
        normalizedDistance = distance / (rightPoints[0] - leftPoints[0]).magnitude;

        // カット後の新頂点に対する処理。フラグメントシェーダでの補完と同じく、分割した位置に応じて適切に補完した値を設定する
        Vector3 newVertex1 = Vector3.Lerp(leftPoints[0], rightPoints[0], normalizedDistance);
        Vector2 newUv1 = Vector2.Lerp(leftUvs[0], rightUvs[0], normalizedDistance);
        Vector3 newNormal1 = Vector3.Lerp(leftNormals[0], rightNormals[0], normalizedDistance);

        // 新頂点郡に新しい頂点を追加
        _newVertices.Add(newVertex1);

        #endregion

        #region 右側の処理

        _blade.Raycast(new Ray(leftPoints[1], (rightPoints[1] - leftPoints[1]).normalized), out distance);

        normalizedDistance = distance / (rightPoints[1] - leftPoints[1]).magnitude;
        Vector3 newVertex2 = Vector3.Lerp(leftPoints[1], rightPoints[1], normalizedDistance);
        Vector2 newUv2 = Vector2.Lerp(leftUvs[1], rightUvs[1], normalizedDistance);
        Vector3 newNormal2 = Vector3.Lerp(leftNormals[1], rightNormals[1], normalizedDistance);

        // 新頂点郡に新しい頂点を追加
        _newVertices.Add(newVertex2);

        #endregion

        bool leftDoubleCheck = false;

        // 計算された新しい頂点を使って、新トライアングルを左右ともに追加する
        // memo: どう分割されても、左右どちらかは1つの三角形になる気がするけど、縮退三角形的な感じでとにかく2つずつ追加している感じだろうか？
        _leftSide.AddTriangle(
            new[] { leftPoints[0], newVertex1, newVertex2 },
            new[] { leftNormals[0], newNormal1, newNormal2 },
            new[] { leftUvs[0], newUv1, newUv2 },
            newNormal1,
            submesh
        );

        if (leftPoints[0] != leftPoints[1])
        {
            _leftSide.AddTriangle(
                new[] { leftPoints[0], leftPoints[1], newVertex2 },
                new[] { leftNormals[0], leftNormals[1], newNormal2 },
                new[] { leftUvs[0], leftUvs[1], newUv2 },
                newNormal2,
                submesh
            );
            leftDoubleCheck = true;
        }

        _rightSide.AddTriangle(
            new[] { rightPoints[0], newVertex1, newVertex2 },
            new[] { rightNormals[0], newNormal1, newNormal2 },
            new[] { rightUvs[0], newUv1, newUv2 },
            newNormal1,
            submesh
        );

        if (!leftDoubleCheck)
        {
            _rightSide.AddTriangle(
                new[] { rightPoints[0], rightPoints[1], newVertex2 },
                new[] { rightNormals[0], rightNormals[1], newNormal2 },
                new[] { rightUvs[0], rightUvs[1], newUv2 },
                newNormal2,
                submesh
            );
        }
    }

    /// <summary>
    /// 切断面を埋める処理？
    /// </summary>
    private void Capping()
    {
        // 新規頂点の操作を行うので、操作済み頂点を入れる
        List<Vector3> capVertTracker = new List<Vector3>();
        List<Vector3> capVertPolygon = new List<Vector3>();

        // 新しく生成した頂点分だけループする＝全新頂点に対してポリゴンを形成するため調査を行う
        // 具体的には、カット面を構成するポリゴンを形成するため、カット時に重複した頂点を網羅して「面」を形成する頂点を調査する
        for (int i = 0; i < _newVertices.Count; i++)
        {
            // 対象頂点がすでに調査済みのマークされて（追跡配列に含まれて）いたらスキップ
            if (capVertTracker.Contains(_newVertices[i]))
            {
                continue;
            }

            // カット用ポリゴン配列をクリア
            capVertPolygon.Clear();

            // 調査頂点と次の頂点をポリゴン配列に保持する
            capVertPolygon.Add(_newVertices[i + 0]);
            capVertPolygon.Add(_newVertices[i + 1]);

            // 追跡配列に自身と次の頂点を追加する（調査済みのマークをつける）
            capVertTracker.Add(_newVertices[i + 0]);
            capVertTracker.Add(_newVertices[i + 1]);

            // 重複頂点がなくなるまでループし調査する
            bool isDone = false;
            while (!isDone)
            {
                isDone = true;

                // 新頂点郡をループし、「面」を構成する要因となる頂点をすべて抽出する。抽出が終わるまでループを繰り返す
                // 2頂点ごとに調査を行うため、ループは2単位ですすめる
                for (int k = 0; k < _newVertices.Count; k += 2)
                {
                    // 三角形を切ると必ずペアとなる２箇所の頂点が生まれる。
                    // また、全ポリゴンに対して分割点を生成しているため、ほぼ必ず、まったく同じ位置に存在する、別トライアングルの分割頂点が存在するはずである。
                    if (_newVertices[k] == capVertPolygon[^1] &&
                        !capVertTracker.Contains(_newVertices[k + 1]))
                    {
                        // if so add the other
                        // ペアの頂点が見つかったらそれをポリゴン配列に追加し、
                        // 調査済マークをつけて、次のループ処理に回す
                        isDone = false;
                        capVertPolygon.Add(_newVertices[k + 1]);
                        capVertTracker.Add(_newVertices[k + 1]);
                    }
                    else if (_newVertices[k + 1] == capVertPolygon[^1] &&
                             !capVertTracker.Contains(_newVertices[k]))
                    {
                        // if so add the other
                        isDone = false;
                        capVertPolygon.Add(_newVertices[k]);
                        capVertTracker.Add(_newVertices[k]);
                    }
                }
            }

            // 見つかった頂点郡を元に、ポリゴンを形成する
            FillCap(capVertPolygon);
        }
    }

    /// <summary>
    /// カット面を埋める？
    /// </summary>
    /// <param name="vertices">ポリゴンを形成する頂点リスト</param>
    private void FillCap(List<Vector3> vertices)
    {
        // 切断によって生まれた頂点の中心点を計算する
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in vertices)
        {
            center += point;
        }

        center /= vertices.Count;

        // 切断面の上側(今回は左側)を求める
        Vector3 upward = Vector3.zero;
        upward.x = _blade.normal.y;
        upward.y = -_blade.normal.x;
        upward.z = _blade.normal.z;

        // 法線と「上方向」から、横軸を算出
        Vector3 left = Vector3.Cross(_blade.normal, upward);

        // 全頂点に対する処理
        for (int i = 0; i < vertices.Count; i++)
        {
            // 中心から各頂点へのベクトル
            var displacement = vertices[i] - center;

            // 新規生成するポリゴンのUV座標を求める。
            // displacementが中心からのベクトルのため、UV的な中心である0.5をベースに、内積を使ってUVの最終的な位置を得る
            var newUV1 = Vector3.zero;
            newUV1.x = 0.5f + Vector3.Dot(displacement, left);
            newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
            newUV1.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

            // 次の頂点。ただし、最後の頂点の次は最初の頂点を利用するため、若干トリッキーな指定方法をしている（% vertices.Count）
            displacement = vertices[(i + 1) % vertices.Count] - center;

            var newUV2 = Vector3.zero;
            newUV2.x = 0.5f + Vector3.Dot(displacement, left);
            newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
            newUV2.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

            // 左側のポリゴンとして、求めたUVを利用してトライアングルを追加
            _leftSide.AddTriangle(
                new[]
                {
                    vertices[i],
                    vertices[(i + 1) % vertices.Count],
                    center
                },
                new[]
                {
                    -_blade.normal,
                    -_blade.normal,
                    -_blade.normal
                },
                new Vector2[]
                {
                    newUV1,
                    newUV2,
                    new Vector2(0.5f, 0.5f)
                },
                -_blade.normal,
                _leftSide.subIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
            );

            // 右側のトライアングル。基本は左側と同じだが、法線だけ逆向き。
            _rightSide.AddTriangle(
                new[]
                {
                    vertices[i],
                    vertices[(i + 1) % vertices.Count],
                    center
                },
                new[]
                {
                    _blade.normal,
                    _blade.normal,
                    _blade.normal
                },
                new Vector2[]
                {
                    newUV1,
                    newUV2,
                    new(0.5f, 0.5f)
                },
                _blade.normal,
                _rightSide.subIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
            );
        }
    }
}