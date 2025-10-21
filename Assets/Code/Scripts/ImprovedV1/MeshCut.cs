using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Ver2
{
    /// <summary>
    /// メッシュデータへのアクセスは配列コピーを伴うという情報をもとに、キャッシュしたデータの利用による解決を試みる
    /// </summary>
    public class MeshCut : MeshCutBase
    {
        #region 切断した左右の形状を保持するためのクラス

        private class CutSide
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<Vector3> Normals = new();
            public readonly List<Vector2> Uvs = new();
            public readonly List<List<int>> SubIndices = new();

            private readonly Dictionary<int, int> _addedVertices = new();

            public void ClearAll()
            {
                Vertices.Clear();
                Normals.Clear();
                Uvs.Clear();
                SubIndices.Clear();
                _addedVertices.Clear();
            }

            public void AddTriangle(int p1, int p2, int p3, int submesh)
            {
                int p1Index = GetOrAddVertex(p1);
                int p2Index = GetOrAddVertex(p2);
                int p3Index = GetOrAddVertex(p3);

                SubIndices[submesh].Add(p1Index);
                SubIndices[submesh].Add(p2Index);
                SubIndices[submesh].Add(p3Index);
            }

            public void AddTriangle(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal,
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

                int baseIndex = Vertices.Count;

                SubIndices[submesh].Add(baseIndex + 0);
                SubIndices[submesh].Add(baseIndex + 1);
                SubIndices[submesh].Add(baseIndex + 2);

                Vertices.Add(points3[p1]);
                Vertices.Add(points3[p2]);
                Vertices.Add(points3[p3]);

                Normals.Add(normals3[p1]);
                Normals.Add(normals3[p2]);
                Normals.Add(normals3[p3]);

                Uvs.Add(uvs3[p1]);
                Uvs.Add(uvs3[p2]);
                Uvs.Add(uvs3[p3]);
            }

            private int GetOrAddVertex(int index)
            {
                if (_addedVertices.TryGetValue(index, out var existingIndex))
                {
                    return existingIndex;
                }

                int newIndex = Vertices.Count;
                _addedVertices[index] = newIndex;

                Vertices.Add(_baseVertices[index]);
                Normals.Add(_baseNormals[index]);
                Uvs.Add(_baseUVs[index]);

                return newIndex;
            }
        }

        #endregion

        private readonly CutSide _leftSide = new();
        private readonly CutSide _rightSide = new();
        private readonly List<Vector3> _newVertices = new();
        private readonly Dictionary<Vector3, List<Vector3>> _capConnections = new();


        private Plane _blade;
        private static Mesh _targetMesh;
        private static List<Vector3> _baseVertices = new();
        private static List<Vector3> _baseNormals = new();
        private static List<Vector2> _baseUVs = new();


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
        public override GameObject[] Cut(GameObject target, Plane blade, Material capMaterial)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Initialize();
            _blade = blade;
            _targetMesh = target.GetComponent<MeshFilter>().mesh;

            //ToListを避け、UnSafeアクセスを行うメソッドを利用
            _baseVertices = new List<Vector3>(_targetMesh.vertexCount);
            _targetMesh.GetVertices(_baseVertices);
            _baseNormals = new List<Vector3>(_targetMesh.vertexCount);
            _targetMesh.GetNormals(_baseNormals);
            _baseUVs = new List<Vector2>(_targetMesh.vertexCount);
            _targetMesh.GetUVs(0, _baseUVs);

            // 頂点を初期化
            _newVertices.Clear();
            _leftSide.ClearAll();
            _rightSide.ClearAll();

            //頂点画面の左右どちらにあるかの計算結果を保持するための変数群
            bool[] sides = new bool[3];

            for (int submesh = 0; submesh < _targetMesh.subMeshCount; submesh++)
            {
                var triangles = _targetMesh.GetTriangles(submesh);
                _leftSide.SubIndices.Add(new List<int>());
                _rightSide.SubIndices.Add(new List<int>());

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
                        _leftSide.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }
                    else if (right && !left)
                    {
                        _rightSide.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    // どちらにも跨る → 切断実行
                    CutFace(submesh, sides, p1, p2, p3);
                }
            }

            Debug.Log($"左右に振り分け完了。所要時間{stopwatch.ElapsedMilliseconds}ms");

            Material[] mats = target.GetComponent<MeshRenderer>().sharedMaterials;
            // 取得したマテリアル配列の最後のマテリアルが、カット面のマテリアルでない場合
            if (mats[^1].name != capMaterial.name)
            {
                _leftSide.SubIndices.Add(new List<int>());
                _rightSide.SubIndices.Add(new List<int>());
                Material[] newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[mats.Length] = capMaterial;
                mats = newMats;
            }

            Capping();

            Debug.Log($"面の穴埋め完了。所要時間{stopwatch.ElapsedMilliseconds}ms");

            Mesh leftMesh = new Mesh
            {
                name = "Split Mesh Left"
            };
            if (_leftSide.Vertices.Count > 65535)
            {
                leftMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            leftMesh.vertices = _leftSide.Vertices.ToArray();
            leftMesh.normals = _leftSide.Normals.ToArray();
            leftMesh.uv = _leftSide.Uvs.ToArray();
            leftMesh.subMeshCount = _leftSide.SubIndices.Count;
            for (int i = 0; i < _leftSide.SubIndices.Count; i++)
            {
                leftMesh.SetIndices(_leftSide.SubIndices[i].ToArray(), MeshTopology.Triangles, i);
            }


            Mesh rightMesh = new Mesh
            {
                name = "Split Mesh Right"
            };

            if (_rightSide.Vertices.Count > 65535)
            {
                rightMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            rightMesh.vertices = _rightSide.Vertices.ToArray();
            rightMesh.normals = _rightSide.Normals.ToArray();
            rightMesh.uv = _rightSide.Uvs.ToArray();
            rightMesh.subMeshCount = _rightSide.SubIndices.Count;
            for (int i = 0; i < _rightSide.SubIndices.Count; i++)
            {
                rightMesh.SetIndices(_rightSide.SubIndices[i].ToArray(), MeshTopology.Triangles, i);
            }


            // 元のオブジェクトを左側のオブジェクトに
            target.name = "left side";
            target.GetComponent<MeshFilter>().mesh = leftMesh;
            Debug.Log($"左側メッシュの頂点数{leftMesh.vertices.Length}個");

            // 右側のオブジェクトは新規作成
            GameObject leftSideObj = target;

            GameObject rightSideObj = new GameObject("right side", typeof(MeshFilter), typeof(MeshRenderer));
            rightSideObj.transform.position = target.transform.position;
            rightSideObj.transform.rotation = target.transform.rotation;
            rightSideObj.GetComponent<MeshFilter>().mesh = rightMesh;
            rightSideObj.AddComponent<CuttableObject>();
            rightSideObj.AddComponent<MeshCollider>();

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
            float denom1 = Vector3.Dot(_blade.normal, dir1);

            // 内積計算で交差点を求める
            // Plane は法線 n と距離 d で表される: n・p + d = 0
            // distance = -(n・左頂点 + d) / (n・dir)
            float t1 = (-Vector3.Dot(_blade.normal, leftPoints[0]) - _blade.distance) / denom1;

            // 新頂点とUV、法線の補間を手動計算
            Vector3 newVertex1 = leftPoints[0] + dir1 * t1;
            Vector2 newUv1 = leftUvs[0] + (rightUvs[0] - leftUvs[0]) * t1;
            Vector3 newNormal1 = leftNormals[0] + (rightNormals[0] - leftNormals[0]) * t1;

            // 新頂点郡に追加
            _newVertices.Add(newVertex1);

            #endregion

            #region 新規頂点２を生成

            Vector3 dir2 = rightPoints[1] - leftPoints[1];
            float denom2 = Vector3.Dot(_blade.normal, dir2);
            float t2 = (-Vector3.Dot(_blade.normal, leftPoints[1]) - _blade.distance) / denom2;

            Vector3 newVertex2 = leftPoints[1] + dir2 * t2;
            Vector2 newUv2 = leftUvs[1] + (rightUvs[1] - leftUvs[1]) * t2;
            Vector3 newNormal2 = leftNormals[1] + (rightNormals[1] - leftNormals[1]) * t2;

            _newVertices.Add(newVertex2);

            #endregion
            
            //辺で登録
            AddCapConnection(newVertex1, newVertex2);
            AddCapConnection(newVertex2, newVertex1);


            bool leftDoubleCheck = false;

            // 計算された新しい頂点を使って、新トライアングルを追加する
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
        /// 新しく生成された頂点の中で重複やペアを追跡して面を埋める
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

                // 次の頂点をたどってポリゴン形成
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

                // 完成したポリゴンをカット面として埋める
                FillCap(polygon);
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
                    _leftSide.SubIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
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
                    _rightSide.SubIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
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