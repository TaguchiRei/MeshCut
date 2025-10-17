using System.Collections.Generic;
using UnityEngine;

public class TestMeshCutter : MonoBehaviour
{
    [SerializeField] private MeshCut _meshCut;
    [SerializeField] private Collider _myCollider;
    [SerializeField] private Material _capMaterial;

    public void CutMesh()
    {
        var plane = new Plane(transform.up, transform.position);
        var cutObjects = CheckOverlapObjects();
        foreach (var obj in cutObjects)
        {
            _meshCut.Cut(obj, plane, _capMaterial);
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
            if (!hit.gameObject.TryGetComponent<CuttableObject>(out CuttableObject cuttable)) continue;
            objects.Add(hit.gameObject);
        }

        return objects.ToArray();
    }
}