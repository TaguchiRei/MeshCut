using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Code.Scripts.ImprovedV1
{
    /// <summary>
    /// メッシュデータへのアクセスは配列コピーを伴うという情報をもとに、キャッシュしたデータの利用による解決を試みる
    /// </summary>
    public class MeshCut : MeshCutBase
    {
        #region 切断した左右の形状を保持する

        private List<Vector3> _leftVertices = new();
        private List<Vector3> _leftNormals = new();
        private List<Vector2> _leftUvs = new();
        private List<List<int>> _leftSubIndices = new();
        private int[] _leftAddVerticesArray;

        private List<Vector3> _rightVertices = new();
        private List<Vector3> _rightNormals = new();
        private List<Vector2> _rightUvs = new();
        private List<List<int>> _rightSubIndices = new();
        private int[] _rightAddVerticesArray;

        private List<Vector3> _centers = new();
        [SerializeField] private CutObjectPool _cutObjectPool;

        /// <summary>
        /// すべてのリストを初期化
        /// </summary>
        private void ClearAll()
        {
            _leftVertices.Clear();
            _leftNormals.Clear();
            _leftUvs.Clear();
            _leftSubIndices.Clear();
            _rightVertices.Clear();
            _rightNormals.Clear();
            _rightUvs.Clear();
            _rightSubIndices.Clear();
            _centers.Clear();

            _leftAddVerticesArray = new int[_baseVertices.Length];
            _rightAddVerticesArray = new int[_baseVertices.Length];

            Array.Fill(_leftAddVerticesArray, -1);
            Array.Fill(_rightAddVerticesArray, -1);
        }

        #region 左側の処理

        private int GetOrAddVertexLeft(int index)
        {
            if (_leftAddVerticesArray[index] != -1)
            {
                return _leftAddVerticesArray[index];
            }

            int newIndex = _leftVertices.Count;
            _leftAddVerticesArray[index] = newIndex;

            _leftVertices.Add(_baseVertices[index]);
            _leftNormals.Add(_baseNormals[index]);
            _leftUvs.Add(_baseUVs[index]);

            return newIndex;
        }

        private void AddTriangleLeft(int p1, int p2, int p3, int submesh)
        {
            int p1Index = GetOrAddVertexLeft(p1);
            int p2Index = GetOrAddVertexLeft(p2);
            int p3Index = GetOrAddVertexLeft(p3);

            _leftSubIndices[submesh].Add(p1Index);
            _leftSubIndices[submesh].Add(p2Index);
            _leftSubIndices[submesh].Add(p3Index);
        }

        private void AddTriangleLeft(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal,
            int submesh)
        {
            Vector3 calculatedNormal = Vector3.Cross(
                points3[1] - points3[0],
                points3[2] - points3[0]);

            int p1 = 0;
            int p2 = 1;
            int p3 = 2;

            if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
            {
                p1 = 2;
                p2 = 1;
                p3 = 0;
            }

            int baseIndex = _leftVertices.Count;

            _leftSubIndices[submesh].Add(baseIndex + 0);
            _leftSubIndices[submesh].Add(baseIndex + 1);
            _leftSubIndices[submesh].Add(baseIndex + 2);

            _leftVertices.Add(points3[p1]);
            _leftVertices.Add(points3[p2]);
            _leftVertices.Add(points3[p3]);

            _leftNormals.Add(normals3[p1]);
            _leftNormals.Add(normals3[p2]);
            _leftNormals.Add(normals3[p3]);

            _leftUvs.Add(uvs3[p1]);
            _leftUvs.Add(uvs3[p2]);
            _leftUvs.Add(uvs3[p3]);
        }

        #endregion

        #region 右側の処理

        private int GetOrAddVertexRight(int index)
        {
            if (_rightAddVerticesArray[index] != -1)
            {
                return _rightAddVerticesArray[index];
            }

            int newIndex = _rightVertices.Count;
            _rightAddVerticesArray[index] = newIndex;

            _rightVertices.Add(_baseVertices[index]);
            _rightNormals.Add(_baseNormals[index]);
            _rightUvs.Add(_baseUVs[index]);

            return newIndex;
        }

        private void AddTriangleRight(int p1, int p2, int p3, int submesh)
        {
            int p1Index = GetOrAddVertexRight(p1);
            int p2Index = GetOrAddVertexRight(p2);
            int p3Index = GetOrAddVertexRight(p3);

            _rightSubIndices[submesh].Add(p1Index);
            _rightSubIndices[submesh].Add(p2Index);
            _rightSubIndices[submesh].Add(p3Index);
        }

        private void AddTriangleRight(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal,
            int submesh)
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

            int baseIndex = _rightVertices.Count;

            _rightSubIndices[submesh].Add(baseIndex + 0);
            _rightSubIndices[submesh].Add(baseIndex + 1);
            _rightSubIndices[submesh].Add(baseIndex + 2);

            _rightVertices.Add(points3[p1]);
            _rightVertices.Add(points3[p2]);
            _rightVertices.Add(points3[p3]);

            _rightNormals.Add(normals3[p1]);
            _rightNormals.Add(normals3[p2]);
            _rightNormals.Add(normals3[p3]);

            _rightUvs.Add(uvs3[p1]);
            _rightUvs.Add(uvs3[p2]);
            _rightUvs.Add(uvs3[p3]);
        }

        #endregion

        #endregion

        private readonly Dictionary<Vector3, List<Vector3>> _capConnections = new();

        private Plane _blade;
        private Mesh _targetMesh;
        private Vector3[] _baseVertices;
        private Vector3[] _baseNormals;
        private Vector2[] _baseUVs;

        /// <summary>
        /// 対象のメッシュを切断する。
        /// </summary>
        /// <param name="target"></param>
        /// <param name="blade"></param>
        /// <param name="capMaterial"></param>
        /// <returns></returns>
        public override GameObject[] Cut(GameObject target, Plane blade, Material capMaterial)
        {
#if UNITY_EDITOR
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            _blade = blade;
            _targetMesh = target.GetComponent<MeshFilter>().mesh;

            _baseVertices = _targetMesh.vertices;
            _baseNormals = _targetMesh.normals;
            _baseUVs = _targetMesh.uv;

            // 頂点を初期化
            ClearAll();

            //頂点画面の左右どちらにあるかの計算結果を保持するための変数群
            bool[] sides = new bool[3];

            for (int submesh = 0; submesh < _targetMesh.subMeshCount; submesh++)
            {
                var triangles = _targetMesh.GetTriangles(submesh);
                _leftSubIndices.Add(new List<int>());
                _rightSubIndices.Add(new List<int>());

                // サブメッシュのインデックス数分ループ
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // p1 - p3のインデックスを取得。つまりトライアングル
                    var p1 = triangles[i + 0];
                    var p2 = triangles[i + 1];
                    var p3 = triangles[i + 2];

                    bool left = false;
                    bool right = false;

                    sides[0] = _blade.GetSide(_baseVertices[p1]);
                    sides[1] = _blade.GetSide(_baseVertices[p2]);
                    sides[2] = _blade.GetSide(_baseVertices[p3]);

                    // 左右両方に存在するか判定
                    for (int s = 0; s < 3; s++)
                    {
                        if (sides[s]) left = true;
                        else right = true;

                        // 両方trueになった時点で抜ける（分岐予測が単純化される）
                        if (left & right)
                            break;
                    }

                    // 完全に片側なら即追加
                    if (left && !right)
                    {
                        AddTriangleLeft(p1, p2, p3, submesh);
                        continue;
                    }

                    if (right && !left)
                    {
                        AddTriangleRight(p1, p2, p3, submesh);
                        continue;
                    }

                    // 面の左右にまたがっている場合、切断を行う
                    CutFace(submesh, sides, p1, p2, p3);
                }
            }
#if UNITY_EDITOR

            Debug.Log($"左右に振り分け完了。所要時間{stopwatch.ElapsedMilliseconds}ms");
#endif


            Material[] mats = target.GetComponent<MeshRenderer>().sharedMaterials;
            // 取得したマテリアル配列の最後のマテリアルが、カット面のマテリアルでない場合
            if (mats[^1].name != capMaterial.name)
            {
                _leftSubIndices.Add(new List<int>());
                _rightSubIndices.Add(new List<int>());
                Material[] newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[mats.Length] = capMaterial;
                mats = newMats;
            }

            Capping();

#if UNITY_EDITOR

            Debug.Log($"面の穴埋め完了。所要時間{stopwatch.ElapsedMilliseconds}ms");
#endif

            var centers = _centers.ToArray();
            GameObject leftObj = null;
            GameObject rightObj = null;

            if (_leftVertices.Count >= 2)
            {
                Mesh leftMesh = new Mesh
                {
                    name = "Split Mesh Left"
                };
                if (_leftVertices.Count > 65535)
                {
                    leftMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }
                
                leftMesh.SetVertices(_leftVertices);
                leftMesh.SetNormals(_leftNormals);
                leftMesh.SetUVs(0, _leftUvs);
                leftMesh.subMeshCount = _leftSubIndices.Count;
                for (int i = 0; i < _leftSubIndices.Count; i++)
                {
                    leftMesh.SetIndices(_leftSubIndices[i].ToArray(), MeshTopology.Triangles, i);
                }
                var result = _cutObjectPool.GenerateCutObject(target, _leftVertices.ToList(), mats, centers);

                if (!result.Item2) result.Item1.GetComponent<MeshCollider>().sharedMesh = leftMesh;

                result.Item1.GetComponent<MeshFilter>().sharedMesh = leftMesh;

                leftObj = result.Item1;
            }

            if (_rightVertices.Count >= 2)
            {
                Mesh rightMesh = new Mesh
                {
                    name = "Split Mesh Right"
                };

                if (_rightVertices.Count > 65535)
                {
                    rightMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }
                
                rightMesh.SetVertices(_rightVertices);
                rightMesh.SetNormals(_rightNormals);
                rightMesh.SetUVs(0, _rightUvs);
                
                rightMesh.subMeshCount = _rightSubIndices.Count;
                for (int i = 0; i < _rightSubIndices.Count; i++)
                {
                    rightMesh.SetIndices(_rightSubIndices[i].ToArray(), MeshTopology.Triangles, i);
                }
                
                var result = _cutObjectPool.GenerateCutObject(target, _rightVertices.ToList(), mats, centers);

                if (!result.Item2) result.Item1.GetComponent<MeshCollider>().sharedMesh = rightMesh;

                result.Item1.GetComponent<MeshFilter>().sharedMesh = rightMesh;

                rightObj = result.Item1;
            }

            target.SetActive(false);


            // assign mats
            // 新規生成したマテリアルリストをそれぞれのオブジェクトに適用する
#if UNITY_EDITOR
            Debug.Log($"オブジェクト生成完了。所要時間{stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Stop();
#endif

            // 左右のGameObjectの配列を返す
            return new[] { leftObj, rightObj };
        }

        void CutFace(int submesh, bool[] sides, int index1, int index2, int index3)
        {
            Vector3[] leftPoints = new Vector3[2];
            Vector3[] leftNormals = new Vector3[2];
            Vector2[] leftUvs = new Vector2[2];
            Vector3[] rightPoints = new Vector3[2];
            Vector3[] rightNormals = new Vector3[2];
            Vector2[] rightUvs = new Vector2[2];

            bool setLeft = false;
            bool setRight = false;

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
                    if (!setLeft)
                    {
                        setLeft = true;
                        // 頂点およびUV、法線の設定
                        leftPoints[0] = _baseVertices[p];
                        leftUvs[0] = _baseUVs[p];
                        leftNormals[0] = _baseNormals[p];

                        //アクセスされる可能性のある[1]に値を複製
                        leftPoints[1] = leftPoints[0];
                        leftUvs[1] = leftUvs[0];
                        leftNormals[1] = leftNormals[0];
                    }
                    else
                    {
                        // 2頂点目の場合は2番目に直接頂点情報を設定する
                        leftPoints[1] = _baseVertices[p];
                        leftUvs[1] = _baseUVs[p];
                        leftNormals[1] = _baseNormals[p];
                    }
                }
                else
                {
                    // 左と同様の操作を右にも行う
                    if (!setRight)
                    {
                        setRight = true;

                        rightPoints[0] = _baseVertices[p];
                        rightUvs[0] = _baseUVs[p];
                        rightNormals[0] = _baseNormals[p];

                        rightPoints[1] = rightPoints[0];
                        rightUvs[1] = rightUvs[0];
                        rightNormals[1] = rightNormals[0];
                    }
                    else
                    {
                        rightPoints[1] = _baseVertices[p];
                        rightUvs[1] = _baseUVs[p];
                        rightNormals[1] = _baseNormals[p];
                    }
                }
            }

            #region 新規頂点１を生成

            Vector3 dir1 = rightPoints[0] - leftPoints[0];
            float dot1 = Vector3.Dot(_blade.normal, dir1);

            // 内積計算で交差点を求める
            // Plane は法線 n と距離 d で表される: n・p + d = 0
            // distance = -(n・左頂点 + d) / (n・dir)
            float t1 = (-Vector3.Dot(_blade.normal, leftPoints[0]) - _blade.distance) / dot1;

            // 新頂点とUV、法線の補間を手動計算
            Vector3 newVertex1 = leftPoints[0] + dir1 * t1;
            Vector2 newUv1 = leftUvs[0] + (rightUvs[0] - leftUvs[0]) * t1;
            Vector3 newNormal1 = leftNormals[0] + (rightNormals[0] - leftNormals[0]) * t1;

            // 新頂点郡に追加

            #endregion

            #region 新規頂点２を生成

            Vector3 dir2 = rightPoints[1] - leftPoints[1];
            float dot2 = Vector3.Dot(_blade.normal, dir2);
            float t2 = (-Vector3.Dot(_blade.normal, leftPoints[1]) - _blade.distance) / dot2;

            Vector3 newVertex2 = leftPoints[1] + dir2 * t2;
            Vector2 newUv2 = leftUvs[1] + (rightUvs[1] - leftUvs[1]) * t2;
            Vector3 newNormal2 = leftNormals[1] + (rightNormals[1] - leftNormals[1]) * t2;

            #endregion

            //辺で登録
            AddCapConnection(newVertex1, newVertex2);
            AddCapConnection(newVertex2, newVertex1);


            bool leftDoubleCheck = false;

            // 計算された新しい頂点を使って、新トライアングルを追加する
            AddTriangleLeft(
                new[] { leftPoints[0], newVertex1, newVertex2 },
                new[] { leftNormals[0], newNormal1, newNormal2 },
                new[] { leftUvs[0], newUv1, newUv2 },
                newNormal1,
                submesh
            );

            if (leftPoints[0] != leftPoints[1])
            {
                AddTriangleLeft(
                    new[] { leftPoints[0], leftPoints[1], newVertex2 },
                    new[] { leftNormals[0], leftNormals[1], newNormal2 },
                    new[] { leftUvs[0], leftUvs[1], newUv2 },
                    newNormal2,
                    submesh
                );
                leftDoubleCheck = true;
            }

            AddTriangleRight(
                new[] { rightPoints[0], newVertex1, newVertex2 },
                new[] { rightNormals[0], newNormal1, newNormal2 },
                new[] { rightUvs[0], newUv1, newUv2 },
                newNormal1,
                submesh
            );

            if (!leftDoubleCheck)
            {
                AddTriangleRight(
                    new[] { rightPoints[0], rightPoints[1], newVertex2 },
                    new[] { rightNormals[0], rightNormals[1], newNormal2 },
                    new[] { rightUvs[0], rightUvs[1], newUv2 },
                    newNormal2,
                    submesh
                );
            }
        }

        /// <summary>
        /// 新しく生成された頂点からループを作り、作ったループごとに面を埋める処理
        /// </summary>
        private void Capping()
        {
            HashSet<Vector3> visited = new();

            foreach (var kv in _capConnections)
            {
                if (visited.Contains(kv.Key))
                    continue;

                List<Vector3> polygon = new();
                Vector3 current = kv.Key;
                polygon.Add(current);
                visited.Add(current);

                // 次の頂点を順にたどって、最初の頂点に戻ってきたらループ完成
                while (true)
                {
                    if (!_capConnections.TryGetValue(current, out var neighbors))
                        break;

                    Vector3 next = neighbors.FirstOrDefault(v => !visited.Contains(v));
                    if (next == default)
                        break;

                    polygon.Add(next);
                    visited.Add(next);
                    current = next;
                }

                // 完成した切断面のループを新たなポリゴンを利用して閉じる
                FillCap(polygon);
            }
        }

        /// <summary>
        /// 与えられたループをもとに面を埋める処理
        /// </summary>
        /// <param name="vertices">ポリゴンを形成する頂点リスト</param>
        private void FillCap(List<Vector3> vertices)
        {
            // 与えられたループの構成要素の頂点の中心を求める
            Vector3 center = Vector3.zero;
            foreach (Vector3 point in vertices)
            {
                center += point;
            }

            center /= vertices.Count;
            _centers.Add(center);

            // _bladeの法線をもとにUV座標系のどちらをUとして扱うかを決定する。(xyを入れ替えるのはxy平面上で90度回転させる行為)
            Vector3 upward = Vector3.zero;
            upward.x = _blade.normal.y;
            upward.y = -_blade.normal.x;
            upward.z = _blade.normal.z;
            // 法線と「上方向」から、横軸を算出
            Vector3 left = Vector3.Cross(_blade.normal, upward);

            //_blade.normal	切断面の法線方向	切断面の向き（Z軸的）
            //upward	法線に直交する軸	切断面の「縦方向（V軸）」
            //left	_blade.normal × upward	切断面の「横方向（U軸）」

            // 全頂点に対する処理
            for (int i = 0; i < vertices.Count; i++)
            {
                // 中心から各頂点へのベクトル
                var displacement = vertices[i] - center;

                // 新規生成するポリゴンのUV座標を求める。
                // displacementが中心からのベクトルのため、UV的な中心である0.5をベースに、内積を使ってUVの最終的な位置を得る
                var newUV1 = Vector2.zero;
                newUV1.x = 0.5f + Vector3.Dot(displacement, left);
                newUV1.y = 0.5f + Vector3.Dot(displacement, upward);

                // 次の頂点は隣り合うもう一つの頂点。
                // Countで割ることでリストの範囲外に出ないようにしつつ、最後の面は配列の最後尾と最初の頂点からなる面にして面を閉じている
                displacement = vertices[(i + 1) % vertices.Count] - center;

                var newUV2 = Vector2.zero;
                newUV2.x = 0.5f + Vector3.Dot(displacement, left);
                newUV2.y = 0.5f + Vector3.Dot(displacement, upward);

                // 左側のポリゴンとして、求めたUVを利用してトライアングルを追加
                AddTriangleLeft(
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
                    new[]
                    {
                        newUV1,
                        newUV2,
                        new(0.5f, 0.5f)
                    },
                    -_blade.normal,
                    _leftSubIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
                );

                // 右側のトライアングル。基本は左側と同じだが、法線だけ逆向き。
                AddTriangleRight(
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
                    new[]
                    {
                        newUV1,
                        newUV2,
                        new(0.5f, 0.5f)
                    },
                    _blade.normal,
                    _rightSubIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
                );
            }
        }

        private void AddCapConnection(Vector3 a, Vector3 b)
        {
            if (!_capConnections.TryGetValue(a, out var list))
            {
                list = new List<Vector3>();
                _capConnections[a] = list;
            }

            list.Add(b);
        }


        private void FillCutFace(List<Vector3> vertices)
        {
        }

        /// <summary>
        /// ３頂点のなす角が180度以内かを調べる
        /// </summary>
        /// <param name="o"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool IsAngleLessThan180(
            Vector3 o,
            Vector3 a,
            Vector3 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x) > 0;
        }
    }
}