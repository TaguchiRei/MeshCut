using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class MeshCut : MonoBehaviour
{
    [SerializeField] private GameObject _blade;
    [SerializeField] private MeshFilter _meshFilter;
    private Mesh _targetMesh;
    private Plane _cutFace;


    private void Start()
    {
        _cutFace = new Plane(
            _meshFilter.gameObject.transform.InverseTransformDirection(_blade.transform.up),
            _meshFilter.gameObject.transform.InverseTransformPoint(_blade.transform.position));
        _targetMesh = _meshFilter.mesh;
        DeleteOverlap();
    }

    private void DeleteOverlap()
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        Vector3[] defaultVertices = _targetMesh.vertices;
        int subMeshCount = _targetMesh.subMeshCount;

        // 新しいサブメッシュ用の三角形リスト
        List<int>[] upperTriangles = new List<int>[subMeshCount];
        List<Vector3> upperVertices = new();
        List<int>[] downTriangles = new List<int>[subMeshCount];
        List<Vector3> downVertices = new();
        for (int s = 0; s < subMeshCount; s++)
        {
            upperTriangles[s] = new List<int>();
            downTriangles[s] = new List<int>();
            int[] triangles = _targetMesh.GetTriangles(s);
            foreach (var vertex in defaultVertices)
            {
                if (_cutFace.GetSide(vertex))
                {
                    upperVertices.Add(vertex);
                }
                else
                {
                    downVertices.Add(vertex);
                }
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 vertex1 = defaultVertices[triangles[i]];
                Vector3 vertex2 = defaultVertices[triangles[i + 1]];
                Vector3 vertex3 = defaultVertices[triangles[i + 2]];

                bool v1Side = _cutFace.GetSide(vertex1);
                bool v2Side = _cutFace.GetSide(vertex2);
                bool v3Side = _cutFace.GetSide(vertex3);

                #region 計算の説明

                /*
                  三角形分割時の孤立頂点判定について

                  1. 孤立頂点について
                     三角形の3頂点のうち、カット面で他の2頂点とは異なる側に存在する頂点のこと。
                     この頂点を基準に新規頂点を生成して三角形を分割する。

                  2. 合計値を見るとわかること:
                     各頂点に重みを付けて合計値を計算することで、
                       - 孤立頂点が面の上側か下側か
                       - どの頂点が孤立しているか
                     を1回の計算で判定可能。

                     重み付けの例:
                       v1Side = true -> 1
                       v2Side = true -> 2
                       v3Side = true -> 4

                  3. 合計値の意味の対応表:
                  | 合計(sum) | 孤立頂点      | 孤立頂点がある面の側 |
                  |-----------|---------------|----------------|
                  | 1         | v1            | 上             |
                  | 2         | v2            | 上             |
                  | 4         | v3            | 上             |
                  | 3         | v3            | 下             |
                  | 5         | v2            | 下             |
                  | 6         | v1            | 下             |
                  | 0         | 前頂点が下      | 下             |
                  | 7         | 全頂点が上      | 上             |

                  この方式により、if文や複雑な条件分岐を減らして高速にメッシュ分割処理が可能。
                 */

                #endregion
                
                //切断面と重なっているか、重なっていないなら上下どちらにあるかを調べる
                int sum = (v1Side ? 1 : 0) + (v2Side ? 2 : 0) + (v3Side ? 4 : 0);

                if (sum is 0 or 7)
                {
                    if (sum == 7)
                    {
                        //7なら面の法線側
                        upperTriangles[s].Add(triangles[i]);
                        upperTriangles[s].Add(triangles[i + 1]);
                        upperTriangles[s].Add(triangles[i + 2]);
                    }
                    else
                    {
                        //0なら面の法線と反対方向
                        downTriangles[s].Add(triangles[i]);
                        downTriangles[s].Add(triangles[i + 1]);
                        downTriangles[s].Add(triangles[i + 2]);
                    }
                }
                else
                {
                    //孤立しているのが上側の場合、3以下になる
                    bool isolation = sum <= 3;
                    int isolationV;
                    int otherV1;
                    int otherV2;
                    switch (sum)
                    {
                        case 1 or 6:
                            isolationV = triangles[i];
                            otherV1 = triangles[i + 1];
                            otherV2 = triangles[i + 2];
                            break;
                        case 2 or 5:
                            isolationV = triangles[i + 1];
                            otherV1 = triangles[i];
                            otherV2 = triangles[i + 2];
                            break;
                        case 3 or 4:
                            isolationV = triangles[i + 2];
                            otherV1 = triangles[i];
                            otherV2 = triangles[i + 1];
                            break;
                        default:
                            isolationV = triangles[i];
                            otherV1 = triangles[i + 1];
                            otherV2 = triangles[i + 2];
                            break;
                    }

                    //新規頂点の作成を行う
                    var rayVector1 = (defaultVertices[isolationV] - defaultVertices[otherV1]).normalized;
                    var rayVector2 = (defaultVertices[isolationV] - defaultVertices[otherV2]).normalized;
                    _cutFace.Raycast(
                        new Ray(defaultVertices[isolationV], rayVector1),
                        out var result1);
                    _cutFace.Raycast(
                        new Ray(defaultVertices[isolationV], rayVector2),
                        out var result2);
                    Vector3 newVertex1 = rayVector1 * result1;
                    Vector3 newVertex2 = rayVector2 * result2;
                    
                    if(isolation)
                    {
                        //isolationVが切断面の上側
                        upperVertices.Add(newVertex1);
                        upperVertices.Add(newVertex2);
                        downVertices.Add(newVertex1);
                        downVertices.Add(newVertex2);
                    }
                    else
                    {
                        //otherV1とotherV2が切断面の上側
                        upperVertices.Add(newVertex1);
                        upperVertices.Add(newVertex2);
                        downVertices.Add(newVertex1);
                        downVertices.Add(newVertex2);
                    }
                }
            }
        }

        // 新しいメッシュ作成
        Mesh newMesh = new Mesh
        {
            vertices = defaultVertices,
            subMeshCount = subMeshCount
        };

        for (int s = 0; s < subMeshCount; s++)
        {
            newMesh.SetTriangles(upperTriangles[s], s);
        }

        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();
        _meshFilter.mesh = newMesh;
    }
}