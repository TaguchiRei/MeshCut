using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace BLINDED_AM_ME
{
    public class SampleMeshCut
    {
        public class SampleMeshCutSide
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector3> normals = new List<Vector3>();
            public List<Vector2> uvs = new List<Vector2>();
            public List<int> triangles = new List<int>();
            public List<List<int>> subIndices = new List<List<int>>();

            public void ClearAll()
            {
                vertices.Clear();
                normals.Clear();
                uvs.Clear();
                triangles.Clear();
                subIndices.Clear();
            }

            /// <summary>
            /// トライアングルとして3頂点を追加
            /// ※ 頂点情報は元のメッシュからコピーする
            /// </summary>
            /// <param name="p1">頂点1</param>
            /// <param name="p2">頂点2</param>
            /// <param name="p3">頂点3</param>
            /// <param name="submesh">対象のサブメシュ</param>
            public void AddTriangle(int p1, int p2, int p3, int submesh)
            {
                // triangle index order goes 1,2,3,4....

                // 頂点配列のカウント。随時追加されていくため、ベースとなるindexを定義する。
                // ※ AddTriangleが呼ばれるたびに頂点数は増えていく。
                int base_index = vertices.Count;

                // 対象サブメッシュのインデックスに追加していく
                subIndices[submesh].Add(base_index + 0);
                subIndices[submesh].Add(base_index + 1);
                subIndices[submesh].Add(base_index + 2);

                // 三角形郡の頂点を設定
                triangles.Add(base_index + 0);
                triangles.Add(base_index + 1);
                triangles.Add(base_index + 2);

                // 対象オブジェクトの頂点配列から頂点情報を取得し設定する
                // （victim_meshはstaticメンバなんだけどいいんだろうか・・）
                vertices.Add(_targetMesh.vertices[p1]);
                vertices.Add(_targetMesh.vertices[p2]);
                vertices.Add(_targetMesh.vertices[p3]);

                // 同様に、対象オブジェクトの法線配列から法線を取得し設定する
                normals.Add(_targetMesh.normals[p1]);
                normals.Add(_targetMesh.normals[p2]);
                normals.Add(_targetMesh.normals[p3]);

                // 同様に、UVも。
                try
                {
                    uvs.Add(_targetMesh.uv[p1]);
                    uvs.Add(_targetMesh.uv[p2]);
                    uvs.Add(_targetMesh.uv[p3]);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"エラーを無視しました: {e.Message}");
                }
            }

            /// <summary>
            /// トライアングルを追加する
            /// ※ オーバーロードしている他メソッドとは異なり、引数の値で頂点（ポリゴン）を追加する
            /// </summary>
            /// <param name="points3">トライアングルを形成する3頂点</param>
            /// <param name="normals3">3頂点の法線</param>
            /// <param name="uvs3">3頂点のUV</param>
            /// <param name="faceNormal">ポリゴンの法線</param>
            /// <param name="submesh">サブメッシュID</param>
            public void AddTriangle(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal,
                int submesh)
            {
                // 引数の3頂点から法線を計算
                Vector3 calculatedNormal = Vector3.Cross((points3[1] - points3[0]).normalized,
                    (points3[2] - points3[0]).normalized);

                int p1 = 0;
                int p2 = 1;
                int p3 = 2;

                // 引数で指定された法線と逆だった場合はインデックスの順番を逆順にする（つまり面を裏返す）
                if (Vector3.Dot(calculatedNormal, faceNormal) < 0)
                {
                    p1 = 2;
                    p2 = 1;
                    p3 = 0;
                }

                int base_index = vertices.Count;

                subIndices[submesh].Add(base_index + 0);
                subIndices[submesh].Add(base_index + 1);
                subIndices[submesh].Add(base_index + 2);

                triangles.Add(base_index + 0);
                triangles.Add(base_index + 1);
                triangles.Add(base_index + 2);

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
        }

        private static SampleMeshCutSide _leftSide = new();
        private static SampleMeshCutSide _rightSide = new();

        private static Plane _blade;
        private static Mesh _targetMesh;

        // capping stuff
        private static List<Vector3> _newVertices = new List<Vector3>();

        /// <summary>
        /// Cut the specified victim, blade_plane and capMaterial.
        /// （指定された「victim」をカットする。ブレード（平面）とマテリアルから切断を実行する）
        /// </summary>
        /// <param name="target">Victim.</param>
        /// <param name="blade_plane">Blade plane.</param>
        /// <param name="capMaterial">Cap material.</param>
        public static GameObject[] Cut(GameObject target, GameObject bladeObj, Material capMaterial)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // set the blade relative to victim
            // victimから相対的な平面（ブレード）をセット
            // 具体的には、対象オブジェクトのローカル座標での平面の法線と位置から平面を生成する
            _blade = new Plane(
                target.transform.InverseTransformDirection(-bladeObj.transform.up),
                target.transform.InverseTransformPoint(bladeObj.transform.position)
            );

            // get the victims mesh
            // 対象のメッシュを取得
            _targetMesh = target.GetComponent<MeshFilter>().mesh;

            // reset values
            // 新しい頂点郡
            _newVertices.Clear();

            // 平面より左の頂点郡（MeshCutSide）
            _leftSide.ClearAll();

            //平面より右の頂点郡（MeshCutSide）
            _rightSide.ClearAll();

            // ここでの「3」はトライアングル？
            bool[] sides = new bool[3];
            int[] triangles;
            int p1, p2, p3;

            // go throught the submeshes
            // サブメッシュの数だけループ
            for (int submesh = 0; submesh < _targetMesh.subMeshCount; submesh++)
            {
                // サブメッシュのインデックス数を取得
                triangles = _targetMesh.GetIndices(submesh);

                // List<List<int>>型のリスト。サブメッシュ一つ分のインデックスリスト
                _leftSide.subIndices.Add(new List<int>()); // 左
                _rightSide.subIndices.Add(new List<int>()); // 右

                // サブメッシュのインデックス数分ループ
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // p1 - p3のインデックスを取得。つまりトライアングル
                    p1 = triangles[i + 0];
                    p2 = triangles[i + 1];
                    p3 = triangles[i + 2];

                    // それぞれ評価中のメッシュの頂点が、冒頭で定義された平面の左右どちらにあるかを評価。
                    // `GetSide` メソッドによりboolを得る。
                    sides[0] = _blade.GetSide(_targetMesh.vertices[p1]);
                    sides[1] = _blade.GetSide(_targetMesh.vertices[p2]);
                    sides[2] = _blade.GetSide(_targetMesh.vertices[p3]);

                    // whole triangle
                    // 頂点０と頂点１および頂点２がどちらも同じ側にある場合はカットしない
                    if (sides[0] == sides[1] && sides[0] == sides[2])
                    {
                        if (sides[0])
                        {
                            // left side
                            // GetSideメソッドでポジティブ（true）の場合は左側にあり
                            _leftSide.AddTriangle(p1, p2, p3, submesh);
                        }
                        else
                        {
                            _rightSide.AddTriangle(p1, p2, p3, submesh);
                        }
                    }
                    else
                    {
                        // cut the triangle
                        // そうではなく、どちらかの点が平面の反対側にある場合はカットを実行する
                        Cut_this_Face(submesh, sides, p1, p2, p3);
                    }
                }
            }

            // 設定されているマテリアル配列を取得
            Material[] mats = target.GetComponent<MeshRenderer>().sharedMaterials;

            // 取得したマテリアル配列の最後のマテリアルが、カット面のマテリアルでない場合
            if (mats[mats.Length - 1].name != capMaterial.name)
            {
                // add cap indices
                // カット面用のインデックス配列を追加？
                _leftSide.subIndices.Add(new List<int>());
                _rightSide.subIndices.Add(new List<int>());

                // カット面分増やしたマテリアル配列を準備
                Material[] newMats = new Material[mats.Length + 1];

                // 既存のものを新しい配列にコピー
                mats.CopyTo(newMats, 0);

                // 新しいマテリアル配列の最後に、カット面用マテリアルを追加
                newMats[mats.Length] = capMaterial;

                // 生成したマテリアルリストを再設定
                mats = newMats;
            }
            Debug.Log($"左右に振り分け完了。所要時間{stopwatch.ElapsedMilliseconds}ms");
            // cap the opennings
            // カット開始
            Capping();
            Debug.Log($"切断面穴埋め完了。所要時間{stopwatch.ElapsedMilliseconds}ms");

            // Left Mesh
            // 左側のメッシュを生成
            // MeshCutSideクラスのメンバから各値をコピー
            Mesh left_HalfMesh = new Mesh();
            left_HalfMesh.name = "Split Mesh Left";
            left_HalfMesh.vertices = _leftSide.vertices.ToArray();
            left_HalfMesh.triangles = _leftSide.triangles.ToArray();
            left_HalfMesh.normals = _leftSide.normals.ToArray();
            left_HalfMesh.uv = _leftSide.uvs.ToArray();

            left_HalfMesh.subMeshCount = _leftSide.subIndices.Count;
            for (int i = 0; i < _leftSide.subIndices.Count; i++)
            {
                left_HalfMesh.SetIndices(_leftSide.subIndices[i].ToArray(), MeshTopology.Triangles, i);
            }


            // Right Mesh
            // 右側のメッシュも同様に生成
            Mesh right_HalfMesh = new Mesh();
            right_HalfMesh.name = "Split Mesh Right";
            right_HalfMesh.vertices = _rightSide.vertices.ToArray();
            right_HalfMesh.triangles = _rightSide.triangles.ToArray();
            right_HalfMesh.normals = _rightSide.normals.ToArray();
            right_HalfMesh.uv = _rightSide.uvs.ToArray();

            right_HalfMesh.subMeshCount = _rightSide.subIndices.Count;
            for (int i = 0; i < _rightSide.subIndices.Count; i++)
            {
                right_HalfMesh.SetIndices(_rightSide.subIndices[i].ToArray(), MeshTopology.Triangles, i);
            }


            // assign the game objects

            // 元のオブジェクトを左側のオブジェクトに
            target.name = "left side";
            target.GetComponent<MeshFilter>().mesh = left_HalfMesh;


            // 右側のオブジェクトは新規作成
            GameObject leftSideObj = target;

            GameObject rightSideObj = new GameObject("right side", typeof(MeshFilter), typeof(MeshRenderer));
            rightSideObj.transform.position = target.transform.position;
            rightSideObj.transform.rotation = target.transform.rotation;
            rightSideObj.GetComponent<MeshFilter>().mesh = right_HalfMesh;

            // assign mats
            // 新規生成したマテリアルリストをそれぞれのオブジェクトに適用する
            leftSideObj.GetComponent<MeshRenderer>().materials = mats;
            rightSideObj.GetComponent<MeshRenderer>().materials = mats;

            Debug.Log($"オブジェクト生成完了。所要時間{stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Stop();
            
            // 左右のGameObjectの配列を返す
            return new GameObject[] { leftSideObj, rightSideObj };
        }

        /// <summary>
        /// カットを実行する。ただし、実際のメッシュの操作ではなく、あくまで頂点の振り分け、事前準備としての実行
        /// </summary>
        /// <param name="submesh">サブメッシュのインデックス</param>
        /// <param name="sides">評価した3頂点の左右情報</param>
        /// <param name="index1">頂点1</param>
        /// <param name="index2">頂点2</param>
        /// <param name="index3">頂点3</param>
        static void Cut_this_Face(int submesh, bool[] sides, int index1, int index2, int index3)
        {
            // 左右それぞれの情報を保持するための配列郡
            Vector3[] leftPoints = new Vector3[2];
            Vector3[] leftNormals = new Vector3[2];
            Vector2[] leftUvs = new Vector2[2];
            Vector3[] rightPoints = new Vector3[2];
            Vector3[] rightNormals = new Vector3[2];
            Vector2[] rightUvs = new Vector2[2];

            bool didset_left = false;
            bool didset_right = false;

            // 3頂点分繰り返す
            // 処理内容としては、左右を判定して、左右の配列に3頂点を振り分ける処理を行っている
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

                // sides[side]がtrue、つまり左側の場合
                if (sides[side])
                {
                    // すでに左側の頂点が設定されているか（3頂点が左右に振り分けられるため、必ず左右どちらかは2つの頂点を持つことになる）
                    if (!didset_left)
                    {
                        didset_left = true;

                        // ここは0,1ともに同じ値にしているのは、続く処理で
                        // leftPoints[0,1]の値を使って分割点を求める処理をしているため。
                        // つまり、アクセスされる可能性がある

                        // 頂点の設定
                        leftPoints[0] = _targetMesh.vertices[p];
                        leftPoints[1] = leftPoints[0];

                        // UVの設定
                        leftUvs[0] = _targetMesh.uv[p];
                        leftUvs[1] = leftUvs[0];

                        // 法線の設定
                        leftNormals[0] = _targetMesh.normals[p];
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
                    if (!didset_right)
                    {
                        didset_right = true;

                        rightPoints[0] = _targetMesh.vertices[p];
                        rightPoints[1] = rightPoints[0];
                        rightUvs[0] = _targetMesh.uv[p];
                        rightUvs[1] = rightUvs[0];
                        rightNormals[0] = _targetMesh.normals[p];
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


            // ---------------------------
            // 左側の処理

            // 定義した面と交差する点を探す。
            // つまり、平面によって分割される点を探す。
            // 左の点を起点に、右の点に向けたレイを飛ばし、その分割点を探る。
            _blade.Raycast(new Ray(leftPoints[0], (rightPoints[0] - leftPoints[0]).normalized), out distance);

            // 見つかった交差点を、頂点間の距離で割ることで、分割点の左右の割合を算出する
            normalizedDistance = distance / (rightPoints[0] - leftPoints[0]).magnitude;

            // カット後の新頂点に対する処理。フラグメントシェーダでの補完と同じく、分割した位置に応じて適切に補完した値を設定する
            Vector3 newVertex1 = Vector3.Lerp(leftPoints[0], rightPoints[0], normalizedDistance);
            Vector2 newUv1 = Vector2.Lerp(leftUvs[0], rightUvs[0], normalizedDistance);
            Vector3 newNormal1 = Vector3.Lerp(leftNormals[0], rightNormals[0], normalizedDistance);

            // 新頂点郡に新しい頂点を追加
            _newVertices.Add(newVertex1);


            // ---------------------------
            // 右側の処理

            _blade.Raycast(new Ray(leftPoints[1], (rightPoints[1] - leftPoints[1]).normalized), out distance);

            normalizedDistance = distance / (rightPoints[1] - leftPoints[1]).magnitude;
            Vector3 newVertex2 = Vector3.Lerp(leftPoints[1], rightPoints[1], normalizedDistance);
            Vector2 newUv2 = Vector2.Lerp(leftUvs[1], rightUvs[1], normalizedDistance);
            Vector3 newNormal2 = Vector3.Lerp(leftNormals[1], rightNormals[1], normalizedDistance);

            // 新頂点郡に新しい頂点を追加
            _newVertices.Add(newVertex2);


            // 計算された新しい頂点を使って、新トライアングルを左右ともに追加する
            // memo: どう分割されても、左右どちらかは1つの三角形になる気がするけど、縮退三角形的な感じでとにかく2つずつ追加している感じだろうか？
            _leftSide.AddTriangle(
                new Vector3[] { leftPoints[0], newVertex1, newVertex2 },
                new Vector3[] { leftNormals[0], newNormal1, newNormal2 },
                new Vector2[] { leftUvs[0], newUv1, newUv2 },
                newNormal1,
                submesh
            );

            _leftSide.AddTriangle(
                new Vector3[] { leftPoints[0], leftPoints[1], newVertex2 },
                new Vector3[] { leftNormals[0], leftNormals[1], newNormal2 },
                new Vector2[] { leftUvs[0], leftUvs[1], newUv2 },
                newNormal2,
                submesh
            );

            _rightSide.AddTriangle(
                new Vector3[] { rightPoints[0], newVertex1, newVertex2 },
                new Vector3[] { rightNormals[0], newNormal1, newNormal2 },
                new Vector2[] { rightUvs[0], newUv1, newUv2 },
                newNormal1,
                submesh
            );

            _rightSide.AddTriangle(
                new Vector3[] { rightPoints[0], rightPoints[1], newVertex2 },
                new Vector3[] { rightNormals[0], rightNormals[1], newNormal2 },
                new Vector2[] { rightUvs[0], rightUvs[1], newUv2 },
                newNormal2,
                submesh
            );
        }


        /// <summary>
        /// 切断面を埋める処理？
        /// </summary>
        static void Capping()
        {
            List<Vector3> capVertTracker = new List<Vector3>();
            List<Vector3> capVertpolygon = new List<Vector3>();

            // カット用頂点追跡リスト
            // 具体的には新頂点全部に対する調査を行う。その過程で調査済みをマークする目的で利用する。
            capVertTracker.Clear();

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
                capVertpolygon.Clear();

                // 調査頂点と次の頂点をポリゴン配列に保持する
                capVertpolygon.Add(_newVertices[i + 0]);
                capVertpolygon.Add(_newVertices[i + 1]);

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
                        // go through the pairs
                        // ペアとなる頂点を探す
                        // ここでのペアとは、いちトライアングルから生成される新頂点のペア。
                        // トライアングルからは必ず2頂点が生成されるため、それを探す。
                        // また、全ポリゴンに対して分割点を生成しているため、ほぼ必ず、まったく同じ位置に存在する、別トライアングルの分割頂点が存在するはずである。
                        if (_newVertices[k] == capVertpolygon[capVertpolygon.Count - 1] &&
                            !capVertTracker.Contains(_newVertices[k + 1]))
                        {
                            // if so add the other
                            // ペアの頂点が見つかったらそれをポリゴン配列に追加し、
                            // 調査済マークをつけて、次のループ処理に回す
                            isDone = false;
                            capVertpolygon.Add(_newVertices[k + 1]);
                            capVertTracker.Add(_newVertices[k + 1]);
                        }
                        else if (_newVertices[k + 1] == capVertpolygon[capVertpolygon.Count - 1] &&
                                 !capVertTracker.Contains(_newVertices[k]))
                        {
                            // if so add the other
                            isDone = false;
                            capVertpolygon.Add(_newVertices[k]);
                            capVertTracker.Add(_newVertices[k]);
                        }
                    }
                }

                // 見つかった頂点郡を元に、ポリゴンを形成する
                FillCap(capVertpolygon);
            }
        }

        /// <summary>
        /// カット面を埋める？
        /// </summary>
        /// <param name="vertices">ポリゴンを形成する頂点リスト</param>
        static void FillCap(List<Vector3> vertices)
        {
            // center of the cap
            // カット平面の中心点を計算する
            Vector3 center = Vector3.zero;

            // 引数で渡された頂点位置をすべて合計する
            foreach (Vector3 point in vertices)
            {
                center += point;
            }

            // それを頂点数の合計で割り、中心とする
            center = center / vertices.Count;

            // you need an axis based on the cap
            // カット平面をベースにしたupward
            Vector3 upward = Vector3.zero;

            // 90 degree turn
            // カット平面の法線を利用して、「上」方向を求める
            // 具体的には、平面の左側を上として利用する
            upward.x = _blade.normal.y;
            upward.y = -_blade.normal.x;
            upward.z = _blade.normal.z;

            // 法線と「上方向」から、横軸を算出
            Vector3 left = Vector3.Cross(_blade.normal, upward);

            Vector3 displacement = Vector3.zero;
            Vector3 newUV1 = Vector3.zero;
            Vector3 newUV2 = Vector3.zero;

            // 引数で与えられた頂点分ループを回す
            for (int i = 0; i < vertices.Count; i++)
            {
                // 計算で求めた中心点から、各頂点への方向ベクトル
                displacement = vertices[i] - center;

                // 新規生成するポリゴンのUV座標を求める。
                // displacementが中心からのベクトルのため、UV的な中心である0.5をベースに、内積を使ってUVの最終的な位置を得る
                newUV1 = Vector3.zero;
                newUV1.x = 0.5f + Vector3.Dot(displacement, left);
                newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
                newUV1.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

                // 次の頂点。ただし、最後の頂点の次は最初の頂点を利用するため、若干トリッキーな指定方法をしている（% vertices.Count）
                displacement = vertices[(i + 1) % vertices.Count] - center;

                newUV2 = Vector3.zero;
                newUV2.x = 0.5f + Vector3.Dot(displacement, left);
                newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
                newUV2.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

                // uvs.Add(new Vector2(relativePosition.x, relativePosition.y));
                // normals.Add(blade.normal);

                // 左側のポリゴンとして、求めたUVを利用してトライアングルを追加
                _leftSide.AddTriangle(
                    new Vector3[]
                    {
                        vertices[i],
                        vertices[(i + 1) % vertices.Count],
                        center
                    },
                    new Vector3[]
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
                    new Vector3[]
                    {
                        vertices[i],
                        vertices[(i + 1) % vertices.Count],
                        center
                    },
                    new Vector3[]
                    {
                        _blade.normal,
                        _blade.normal,
                        _blade.normal
                    },
                    new Vector2[]
                    {
                        newUV1,
                        newUV2,
                        new Vector2(0.5f, 0.5f)
                    },
                    _blade.normal,
                    _rightSide.subIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
                );
            }
        }
    }
}