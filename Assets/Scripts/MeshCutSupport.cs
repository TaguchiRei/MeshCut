using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 切断可能なオブジェクトにつけるコンポーネント
/// </summary>
public static class MeshCutSupport
{
    private static CutSide _leftSide;
    private static CutSide _rightSide;
    private static Plane _cutPlane;
    private static Mesh _defaultMesh;

    private static List<Vector3> _newVertices = new();

    /// <summary>
    /// カットしたオブジェクトの情報を保存するネストクラス
    /// </summary>
    private class CutSide
    {
        public List<Vector3> _vertices = new();
        public List<Vector3> _normals = new();
        public List<Vector2> _uvs = new();
        public List<int> _triangles = new();
        public List<List<int>> _subIndices = new();

        public void ClearAll()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();
            _subIndices.Clear();
        }

        /// <summary>
        /// もともとあるメッシュの情報を利用して三角面を追加する
        /// </summary>
        /// <param name="p1">頂点１</param>
        /// <param name="p2">頂点２</param>
        /// <param name="p3">頂点３</param>
        /// <param name="submesh">対象のサブメッシュ</param>
        public void AddTriangle(int p1, int p2, int p3, int submesh)
        {
            int baseInd = _vertices.Count;

            // 対象サブメッシュのインデックスに追加
            _subIndices[submesh].Add(baseInd + 0);
            _subIndices[submesh].Add(baseInd + 1);
            _subIndices[submesh].Add(baseInd + 2);

            // 三角形郡の頂点を設定
            _triangles.Add(baseInd + 0);
            _triangles.Add(baseInd + 1);
            _triangles.Add(baseInd + 2);

            // 対象オブジェクトの頂点配列から頂点情報を取得し設定
            _vertices.Add(_defaultMesh.vertices[p1]);
            _vertices.Add(_defaultMesh.vertices[p2]);
            _vertices.Add(_defaultMesh.vertices[p3]);

            // 対象オブジェクトの法線配列から法線を取得し設定
            _normals.Add(_defaultMesh.normals[p1]);
            _normals.Add(_defaultMesh.normals[p2]);
            _normals.Add(_defaultMesh.normals[p3]);

            // 対象オブジェクトのUV配列からUVを取得し設定
            _uvs.Add(_defaultMesh.uv[p1]);
            _uvs.Add(_defaultMesh.uv[p2]);
            _uvs.Add(_defaultMesh.uv[p3]);
        }
    }

    public static GameObject[] CutObject(GameObject victim, Transform cutPlane, Material capMaterial)
    {
        //切断面のTransformから面を生成する
        _cutPlane = new Plane(
            victim.transform.InverseTransformDirection(cutPlane.up),
            victim.transform.InverseTransformPoint(cutPlane.position)
        );
        
        _defaultMesh = victim.GetComponent<MeshFilter>().mesh;
        _newVertices.Clear();
        _leftSide = new();
        _leftSide = new();
        bool[] sides = new bool[3];
        int[] indices;
        int p1, p2, p3;


        // go throught the submeshes
        // サブメッシュの数だけループ
        for (int sub = 0; sub < _defaultMesh.subMeshCount; sub++)
        {
            // サブメッシュのインデックス数を取得
            indices = _defaultMesh.GetIndices(sub);

            // List<List<int>>型のリスト。サブメッシュ一つ分のインデックスリスト
            _leftSide._subIndices.Add(new List<int>()); // 左
            _rightSide._subIndices.Add(new List<int>()); // 右

            // サブメッシュのインデックス数分ループ
            for (int i = 0; i < indices.Length; i += 3)
            {
                // p1 - p3のインデックスを取得。つまりトライアングル
                p1 = indices[i + 0];
                p2 = indices[i + 1];
                p3 = indices[i + 2];

                // それぞれ評価中のメッシュの頂点が、冒頭で定義された平面の左右どちらにあるかを評価。
                // `GetSide` メソッドによりboolを得る。
                sides[0] = _cutPlane.GetSide(_defaultMesh.vertices[p1]);
                sides[1] = _cutPlane.GetSide(_defaultMesh.vertices[p2]);
                sides[2] = _cutPlane.GetSide(_defaultMesh.vertices[p3]);

                // whole triangle
                // 頂点０と頂点１および頂点２がどちらも同じ側にある場合はカットしない
                if (sides[0] == sides[1] && sides[0] == sides[2])
                {
                    if (sides[0])
                    {
                        // left side
                        // GetSideメソッドでポジティブ（true）の場合は左側にあり
                        _leftSide.AddTriangle(p1, p2, p3, sub);
                    }
                    else
                    {
                        _rightSide.AddTriangle(p1, p2, p3, sub);
                    }
                }
                else
                {
                    //メッシュを切断する処理を書く
                   // Cut_this_Face(sub, sides, p1, p2, p3);
                }
            }
        }

        // 設定されているマテリアル配列を取得
        Material[] mats = victim.GetComponent<MeshRenderer>().sharedMaterials;

        // 取得したマテリアル配列の最後のマテリアルが、カット面のマテリアルでない場合
        if (mats[^1].name != capMaterial.name)
        {
            // add cap indices
            // カット面用のインデックス配列を追加？
            _leftSide._subIndices.Add(new List<int>());
            _rightSide._subIndices.Add(new List<int>());

            // カット面分増やしたマテリアル配列を準備
            Material[] newMats = new Material[mats.Length + 1];

            // 既存のものを新しい配列にコピー
            mats.CopyTo(newMats, 0);

            // 新しいマテリアル配列の最後に、カット面用マテリアルを追加
            newMats[mats.Length] = capMaterial;

            // 生成したマテリアルリストを再設定
            mats = newMats;
        }

        // cap the opennings
        // カット開始
        //Capping();


        // Left Mesh
        // 左側のメッシュを生成
        // MeshCutSideクラスのメンバから各値をコピー
        Mesh left_HalfMesh = new Mesh();
        left_HalfMesh.name = "Split Mesh Left";
        left_HalfMesh.vertices = _leftSide._vertices.ToArray();
        left_HalfMesh.triangles = _leftSide._triangles.ToArray();
        left_HalfMesh.normals = _leftSide._normals.ToArray();
        left_HalfMesh.uv = _leftSide._uvs.ToArray();

        left_HalfMesh.subMeshCount = _leftSide._subIndices.Count;
        for (int i = 0; i < _leftSide._subIndices.Count; i++)
        {
            left_HalfMesh.SetIndices(_leftSide._subIndices[i].ToArray(), MeshTopology.Triangles, i);
        }


        // Right Mesh
        // 右側のメッシュも同様に生成
        Mesh right_HalfMesh = new Mesh();
        right_HalfMesh.name = "Split Mesh Right";
        right_HalfMesh.vertices = _rightSide._vertices.ToArray();
        right_HalfMesh.triangles = _rightSide._triangles.ToArray();
        right_HalfMesh.normals = _rightSide._normals.ToArray();
        right_HalfMesh.uv = _rightSide._uvs.ToArray();

        right_HalfMesh.subMeshCount = _rightSide._subIndices.Count;
        for (int i = 0; i < _rightSide._subIndices.Count; i++)
        {
            right_HalfMesh.SetIndices(_rightSide._subIndices[i].ToArray(), MeshTopology.Triangles, i);
        }


        // assign the game objects

        // 元のオブジェクトを左側のオブジェクトに
        victim.name = "left side";
        victim.GetComponent<MeshFilter>().mesh = left_HalfMesh;


        // 右側のオブジェクトは新規作成
        GameObject leftSideObj = victim;

        GameObject rightSideObj = new GameObject("right side", typeof(MeshFilter), typeof(MeshRenderer));
        rightSideObj.transform.position = victim.transform.position;
        rightSideObj.transform.rotation = victim.transform.rotation;
        rightSideObj.GetComponent<MeshFilter>().mesh = right_HalfMesh;

        // assign mats
        // 新規生成したマテリアルリストをそれぞれのオブジェクトに適用する
        leftSideObj.GetComponent<MeshRenderer>().materials = mats;
        rightSideObj.GetComponent<MeshRenderer>().materials = mats;

        // 左右のGameObjectの配列を返す
        return new GameObject[] { leftSideObj, rightSideObj };
    }
}