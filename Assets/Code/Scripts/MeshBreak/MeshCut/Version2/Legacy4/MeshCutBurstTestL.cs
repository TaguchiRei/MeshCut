using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UsefulAttribute;
using Debug = UnityEngine.Debug;

public class MeshCutBurstTestL : MonoBehaviour
{
    [SerializeField] private Collider _myCollider;
    [SerializeField] private int _batchCount;

    private BurstCutSchedulerL4 _schedulerL4;

    private UniTask _cutTask;

    private void Start()
    {
        _schedulerL4 = new BurstCutSchedulerL4();
        _cutTask = default;
    }

    [MethodExecutor("メッシュ切断をテスト", false)]
    private void TestMethod()
    {
        _cutTask = TestCut();
    }

    private async UniTask TestCut()
    {
        var ret = CheckOverlapObjects();

        Stopwatch allTime = Stopwatch.StartNew();
        var context = _schedulerL4.SchedulingCutLight(new(transform), ret, _batchCount);
        context.Dispose(context.CutJobHandle);
        try
        {
            await context.Complete();
            Debug.Log($"全体処理時間 {allTime.ElapsedMilliseconds}ms");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            if (context.VerticesObjectIdList.IsCreated) context.Dispose();
        }
    }


    private CuttableObjectL[] CheckOverlapObjects()
    {
        // コライダーの範囲内にあるオブジェクトを取得
        List<CuttableObjectL> cuttables = new();
        Collider[] hits = Physics.OverlapBox(
            _myCollider.bounds.center,
            _myCollider.bounds.extents,
            Quaternion.identity
        );

        foreach (Collider hit in hits)
        {
            if (!hit.gameObject.TryGetComponent(out CuttableObjectL cuttable)) continue;
            cuttables.Add(cuttable);
        }

        return cuttables.ToArray();
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