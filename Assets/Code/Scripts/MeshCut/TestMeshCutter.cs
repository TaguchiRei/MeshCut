using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BLINDED_AM_ME;
using UnityEngine;
using Attribute;
using MeshBreak;
using Debug = UnityEngine.Debug;

public class TestMeshCutter : MonoBehaviour
{
    [SerializeField] private int _meshCutNumber;
    [SerializeField] private MeshCutBase[] _meshCut;
    [SerializeField] private Collider _myCollider;
    [SerializeField] private Material _capMaterial;
    [SerializeField] private bool _useSample;

    [MethodExecutor("メッシュカットを実行", false)]
    public void CutMesh()
    {
        if (_useSample)
        {
            List<GameObject> newObjects = new();
            var cutObjects = CheckOverlapObjects();
            Debug.Log(cutObjects.Length);
            foreach (var obj in cutObjects)
            {
                var plane = new Plane(
                    -obj.transform.InverseTransformDirection(-transform.up),
                    obj.transform.InverseTransformPoint(transform.position));
                SampleMeshCut.Cut(obj, plane, _capMaterial);
            }

            Debug.Log("GeneratedMesh");
        }
        else
        {
            List<GameObject> newObjects = new();
            var cutObjects = CheckOverlapObjects().ToHashSet();

            Stopwatch stopwatch = new();
            stopwatch.Start();
            foreach (var obj in cutObjects)
            {
                var plane = new Plane(
                    -obj.transform.InverseTransformDirection(-transform.up),
                    obj.transform.InverseTransformPoint(transform.position));
                _meshCut[_meshCutNumber].Cut(obj, plane, _capMaterial);
            }

            Debug.Log($"メッシュ切断完了。総オブジェクト数:{cutObjects.Count} 全体処理時間:{stopwatch.ElapsedMilliseconds}ms");
        }
    }

    private GameObject[] CheckOverlapObjects()
    {
        // コライダーの範囲内にあるオブジェクトを取得
        List<GameObject> objects = new();
        Collider[] hits = Physics.OverlapBox(
            _myCollider.bounds.center,
            _myCollider.bounds.extents,
            Quaternion.identity
        );

        foreach (Collider hit in hits)
        {
            if (!hit.gameObject.TryGetComponent<BreakableObject>(out BreakableObject cuttable)) continue;
            objects.Add(hit.gameObject);
        }

        return objects.ToArray();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_myCollider == null) return;

        float _planeSize = 10.0f;
        int _gridCount = 10;

        Vector3 planePos = transform.position;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;

        Color _planeColor = new(0f, 1f, 1f, 0.15f);
        Color _outlineColor = Color.cyan;
        Color _gridColor = new(0f, 1f, 1f, 0.3f);

        // デプス（Zテスト）を有効にして描画
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        // === 中央（基準サイズ）の平面 ===
        Vector3 r = right * _planeSize;
        Vector3 f = forward * _planeSize;

        Vector3 p1 = planePos + r + f;
        Vector3 p2 = planePos + r - f;
        Vector3 p3 = planePos - r - f;
        Vector3 p4 = planePos - r + f;

        UnityEditor.Handles.color = _planeColor;
        UnityEditor.Handles.DrawSolidRectangleWithOutline(
            new[] { p1, p2, p3, p4 },
            _planeColor,
            _outlineColor
        );

        // === グリッド線 ===
        UnityEditor.Handles.color = _gridColor;
        for (int i = 1; i < _gridCount; i++)
        {
            float t = i / (float)_gridCount;
            Vector3 startH = Vector3.Lerp(p4, p1, t);
            Vector3 endH = Vector3.Lerp(p3, p2, t);
            UnityEditor.Handles.DrawLine(startH, endH);

            Vector3 startV = Vector3.Lerp(p1, p2, t);
            Vector3 endV = Vector3.Lerp(p4, p3, t);
            UnityEditor.Handles.DrawLine(startV, endV);
        }
        
        DrawOutline(planePos, right, forward, _planeSize, Color.green);

        DrawOutline(planePos, right, forward, _planeSize * 1.5f, Color.green);

        DrawOutline(planePos, right, forward, _planeSize * 0.5f, Color.green);
        
        DrawOutline(planePos, right, forward, _planeSize * 0.25f, Color.green);

        // Zテスト設定を戻す
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
    }

    /// <summary>
    /// 任意サイズの外枠を描画する補助メソッド
    /// </summary>
    private void DrawOutline(Vector3 center, Vector3 right, Vector3 forward, float size, Color color)
    {
        Vector3 r = right * size;
        Vector3 f = forward * size;

        Vector3 p1 = center + r + f;
        Vector3 p2 = center + r - f;
        Vector3 p3 = center - r - f;
        Vector3 p4 = center - r + f;

        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(p1, p2);
        UnityEditor.Handles.DrawLine(p2, p3);
        UnityEditor.Handles.DrawLine(p3, p4);
        UnityEditor.Handles.DrawLine(p4, p1);
    }

#endif
}