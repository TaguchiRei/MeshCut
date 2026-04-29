using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class GPUInstancingExample : MonoBehaviour
{
    public Mesh mesh; // 描画するメッシュ
    public Material material; // GPUインスタンシング対応マテリアル
    public int instanceCount = 50; // インスタンスの数
    public float spacing = 2f; // インスタンス間の間隔

    private List<Matrix4x4> matrices = new List<Matrix4x4>();

    void Start()
    {
        Stopwatch st = Stopwatch.StartNew();
        // 一直線に並べる
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 position = new Vector3(i * spacing, 0f, 0f); // x方向に等間隔
            Quaternion rotation = Quaternion.identity; // 回転なし
            Vector3 scale = Vector3.one; // スケール1

            matrices.Add(Matrix4x4.TRS(position, rotation, scale));
        }

        Debug.Log(st.ElapsedMilliseconds);
        st.Restart();
        int batchSize = 1023;
        for (int i = 0; i < instanceCount; i += batchSize)
        {
            int count = Mathf.Min(batchSize, instanceCount - i);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices.GetRange(i, count));
        }

        Debug.Log(st.ElapsedMilliseconds);
    }

    void Update()
    {
        for (int i = 0; i < instanceCount; i++)
        {
            float x = i * spacing + Mathf.Sin(Time.time + i) * 0.5f; // 少し揺らす
            float y = 0f;
            float z = 0f;

            Quaternion rotation = Quaternion.Euler(0f, Time.time * 30f, 0f); // ゆっくり回転
            Vector3 scale = Vector3.one;

            matrices[i] = Matrix4x4.TRS(new Vector3(x, y, z), rotation, scale);
        }

        // 描画
        int batchSize = 1023;
        for (int i = 0; i < instanceCount; i += batchSize)
        {
            int count = Mathf.Min(batchSize, instanceCount - i);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices.GetRange(i, count));
        }
    }
}