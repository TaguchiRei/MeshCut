using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class CutPlaneCollider : MonoBehaviour
{
    private List<GameObject> _targetObject;

    public CuttableObject[] GetObjects()
    {
        CuttableObject[] objects = new CuttableObject[_targetObject.Count];

        for (int i = 0; i < _targetObject.Count; i++)
        {
            objects[i] = _targetObject[i].GetComponent<CuttableObject>();
        }

        return objects;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!_targetObject.Contains(other.gameObject))
            {
                _targetObject.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (_targetObject.Contains(other.gameObject))
            {
                _targetObject.Remove(other.gameObject);
            }
        }
    }
}