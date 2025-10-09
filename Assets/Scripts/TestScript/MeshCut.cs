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

        Vector3[] vertices = _targetMesh.vertices;
        int subMeshCount = _targetMesh.subMeshCount;

        // 新しいサブメッシュ用の三角形リスト
        List<int>[] upperTriangles = new List<int>[subMeshCount];
        List<int>[] downTriangles = new List<int>[subMeshCount];
        for (int s = 0; s < subMeshCount; s++)
        {
            upperTriangles[s] = new List<int>();
            int[] triangles = _targetMesh.GetTriangles(s);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 vertex1 = vertices[triangles[i]];
                Vector3 vertex2 = vertices[triangles[i + 1]];
                Vector3 vertex3 = vertices[triangles[i + 2]];

                bool v1Side = _cutFace.GetSide(vertex1);
                bool v2Side = _cutFace.GetSide(vertex2);
                bool v3Side = _cutFace.GetSide(vertex3);

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
                    //孤立しているのが上側の場合、
                    bool isolation = sum == 1 || sum == 2 || sum == 3;
                    switch (sum)
                    {
                        case 1:
                            break;
                        case 2:
                            break;
                        case 4:
                            break;
                        case 3:
                            break;
                        case 5:
                            break;
                        case 6:
                            break;
                        case 7:
                    }
                }
            }
        }

        // 新しいメッシュ作成
        Mesh newMesh = new Mesh
        {
            vertices = vertices,
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