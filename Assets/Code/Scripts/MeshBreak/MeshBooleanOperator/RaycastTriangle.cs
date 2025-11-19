using MeshBreak;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Code.Scripts.MeshBreak.MeshBooleanOperator
{
    public static class RaycastTriangle
    {
        public static bool CheckIntersectPoint(TriangleData triangle, Vector3 start, Vector3 end, out Vector3 point)
        {
            point = Vector3.Zero;
            return false;
        }
    }
}