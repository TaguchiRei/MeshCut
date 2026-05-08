using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MeshBreak;
using UnityEngine;
using MeshBreak.MeshCut;
using UsefulAttribute;
using Debug = UnityEngine.Debug;

public class TestMeshCutter : MonoBehaviour
{
    [SerializeField] private MeshCut _meshCut;
    [SerializeField] private Collider _myCollider;
    [SerializeField] private Material _capMaterial;

    [MethodExecutor("メッシュカットを実行", false)]
    public void CutMesh()
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
            _meshCut.Cut(obj, plane, _capMaterial);
        }

        Debug.Log($"メッシュ切断完了。総オブジェクト数:{cutObjects.Count} 全体処理時間:{stopwatch.ElapsedMilliseconds}ms");
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
            if (!hit.gameObject.TryGetComponent(out BreakableObject cuttable)) continue;
            objects.Add(hit.gameObject);
        }

        return objects.ToArray();
    }

    private void OnDrawGizmos()
    {
        BladePlaneDebugger.OnDrawGizmos(transform);
    }
}