using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshBooleanOperator
{
    public class BVHBuilder
    {
    }

    public class BVHNode
    {
        /// <summary> 葉ノードかどうか </summary>
        public bool IsLeaf { get; private set; }

        /// <summary> 三角形 </summary>
        private int[] _triangles;

        /// <summary> 子ノード </summary>
        private BVHNode[] _children;

        /// <summary> 自身のボックス </summary>
        private Bounds _bounds;

        public BVHNode(BVHNode[] children)
        {
            _children = children ?? throw new ArgumentNullException(nameof(children));

            Vector3 maxPosition = new(float.MinValue, float.MinValue, float.MinValue);
            Vector3 minPosition = new(float.MaxValue, float.MaxValue, float.MaxValue);
            foreach (var bvhNode in _children)
            {
                Vector3 max = bvhNode._bounds.max;
                Vector3 min = bvhNode._bounds.min;

                if (maxPosition.x < max.x) maxPosition.x = max.x;
                if (maxPosition.y < max.y) maxPosition.y = max.y;
                if (maxPosition.z < max.z) maxPosition.z = max.z;
                if (minPosition.x > min.x) minPosition.x = min.x;
                if (minPosition.y > min.y) minPosition.y = min.y;
                if (minPosition.z > min.z) minPosition.z = min.z;
            }

            _bounds = new Bounds(center: (minPosition + maxPosition) * 0.5f, maxPosition - minPosition);
            IsLeaf = false;
        }

        public BVHNode(int[] triangles, Vector3[] positions)
        {
            if (triangles == null)
            {
                throw new ArgumentNullException(nameof(triangles));
            }
            else if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            _triangles = triangles;

            Vector3 maxPosition = new(float.MinValue, float.MinValue, float.MinValue);
            Vector3 minPosition = new(float.MaxValue, float.MaxValue, float.MaxValue);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = positions[triangles[i]];
                Vector3 v2 = positions[triangles[i + 1]];
                Vector3 v3 = positions[triangles[i + 2]];

                maxPosition = Vector3.Max(Vector3.Max(Vector3.Max(v1, v2), v3), maxPosition);
                minPosition = Vector3.Min(Vector3.Min(Vector3.Min(v1, v2), v3), minPosition);
            }

            _bounds = new Bounds(center: (minPosition + maxPosition) * 0.5f, maxPosition - minPosition);
            IsLeaf = true;
        }

        public BVHNode[] GetChildren()
        {
            if (!IsLeaf)
            {
                return _children;
            }

            throw new InvalidOperationException("Cannot get the child list from a leaf node");
        }

        public int[] GetTriangles()
        {
            if (IsLeaf)
            {
                return _triangles;
            }

            throw new InvalidOperationException("Cannot get the triangles from a non-leaf node");
        }
    }
}