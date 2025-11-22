using System;
using MeshBreak;
using MeshBreak.MeshBooleanOperator;
using Unity.Mathematics;
using UnityEngine;

public class TestRay : MonoBehaviour
{
    [SerializeField] private GameObject[] _triangle;
    [SerializeField] private GameObject _rayStartPos;
    [SerializeField] private GameObject _rayEndPos;
    [SerializeField] private GameObject _hitPos;

    private void OnDrawGizmos()
    {
        if (_triangle == null || _rayStartPos == null || _rayEndPos == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < _triangle.Length; i++)
        {
            Gizmos.DrawLine(_triangle[i].transform.position, _triangle[(i + 1) % _triangle.Length].transform.position);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_rayStartPos.transform.position, _rayEndPos.transform.position);

        TriangleData triangle = new TriangleData();
        triangle.SetVertexes(_triangle[0].transform.position, _triangle[1].transform.position,
            _triangle[2].transform.position);
        var isHit = MeshCalculationSupport.RayCast(triangle, _rayStartPos.transform.position,
            _rayEndPos.transform.position, out var point);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(point, 0.1f);

        if (_hitPos != null)
        {
            if (isHit)
            {
                _hitPos.transform.position = point;
            }
            else
            {
                _hitPos.transform.position = Vector3.down;
            }
        }
    }
}