using System.Collections.Generic;
using BLINDED_AM_ME;
using UnityEngine;
using Attribute;

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
            var cutObjects = CheckOverlapObjects();
            Debug.Log(cutObjects.Length);
            foreach (var obj in cutObjects)
            {
                var plane = new Plane(
                    -obj.transform.InverseTransformDirection(-transform.up), 
                    obj.transform.InverseTransformPoint(transform.position));
                _meshCut[_meshCutNumber].Cut(obj, plane, _capMaterial);
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