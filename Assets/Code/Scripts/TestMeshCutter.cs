using System.Collections.Generic;
using BLINDED_AM_ME;
using UnityEngine;

public class TestMeshCutter : MonoBehaviour
{
    [SerializeField] private MeshCut _meshCut;
    [SerializeField] private Collider _myCollider;
    [SerializeField] private Material _capMaterial;
    [SerializeField] private bool _useSample;

    public void CutMesh()
    {
        if (_useSample)
        {
            List<GameObject> newObjects = new();
            var plane = new Plane(transform.up, transform.position);
            var cutObjects = CheckOverlapObjects();
            Debug.Log(cutObjects.Length);
            foreach (var obj in cutObjects)
            {
                SampleMeshCut.Cut(obj, gameObject, _capMaterial);
            }
            Debug.Log("GeneratedMesh");
        }
        else
        {
            List<GameObject> newObjects = new();
            var plane = new Plane(transform.forward, transform.position);
            var cutObjects = CheckOverlapObjects();
            Debug.Log(cutObjects.Length);
            foreach (var obj in cutObjects)
            {
                _meshCut.Cut(obj,plane,_capMaterial);
            }
            Debug.Log("GeneratedMesh");
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