using UnityEngine;
using BLINDED_AM_ME;

// MeshCut 名前空間を参照

public class TestCutSide : MonoBehaviour
{
    void Start()
    {
        // 対象オブジェクトを取得
        GameObject victim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh originalMesh = victim.GetComponent<MeshFilter>().mesh;

        Debug.Log($"1. 複製前の頂点数: {originalMesh.vertexCount}");

        // MeshCutSide を利用してメッシュを複製
        SampleMeshCut.MeshCutSide testSide = new SampleMeshCut.MeshCutSide();

        // victim_mesh としてアクセスされるため、一時的に MeshCut.victim_mesh を設定
        // ただし MeshCut.victim_mesh は private なので直接アクセスできないため、
        // ここでは AddTriangle(Vector3[]...) のオーバーロードを使用して代替。
        int submeshCount = originalMesh.subMeshCount;
        for (int i = 0; i < submeshCount; i++)
            testSide.subIndices.Add(new System.Collections.Generic.List<int>());

        int[] indices = originalMesh.triangles;

        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3[] verts = new Vector3[3];
            Vector3[] norms = new Vector3[3];
            Vector2[] uvs = new Vector2[3];

            for (int j = 0; j < 3; j++)
            {
                int idx = indices[i + j];
                verts[j] = originalMesh.vertices[idx];
                norms[j] = originalMesh.normals[idx];
                uvs[j] = originalMesh.uv[idx];
            }

            // 面法線（正規化済み）
            Vector3 faceNormal = Vector3.Cross(
                (verts[1] - verts[0]).normalized,
                (verts[2] - verts[0]).normalized
            ).normalized;

            // 三角形を追加（submesh=0）
            testSide.AddTriangle(verts, norms, uvs, faceNormal, 0);
        }

        // 新しい Mesh を生成してオブジェクト化
        Mesh newMesh = new Mesh();
        newMesh.name = "CopiedMesh";
        newMesh.vertices = testSide.vertices.ToArray();
        newMesh.normals = testSide.normals.ToArray();
        newMesh.uv = testSide.uvs.ToArray();
        newMesh.triangles = testSide.triangles.ToArray();

        GameObject copy = new GameObject("DuplicatedCube", typeof(MeshFilter), typeof(MeshRenderer));
        copy.GetComponent<MeshFilter>().mesh = newMesh;
        copy.GetComponent<MeshRenderer>().material = victim.GetComponent<MeshRenderer>().material;

        Debug.Log($"2. 複製後の頂点数: {newMesh.vertexCount}");
    }
}
