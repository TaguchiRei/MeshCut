using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshBooleanOperator
{
    public class BVHBuilder
    {
        /// <summary>
        /// Boundsを構築
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Bounds GetTriangleBounds(Vector3 a, Vector3 b, Vector3 c)
        {
            var min = Vector3.Min(a, Vector3.Min(b, c));
            var max = Vector3.Max(a, Vector3.Max(b, c));
            Bounds bound = new Bounds();
            bound.SetMinMax(min, max);
            return bound;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static Bounds MergeBounds(List<BVHNode> nodes)
        {
            Bounds merged = nodes[0]._bounds;
            for (int i = 1; i < nodes.Count; i++)
            {
                merged.Encapsulate(nodes[i]._bounds);
            }

            return merged;
        }
        
        
        public static BVHNode Build(List<BVHNode> leaves)
        {
            if (leaves.Count == 1)
                return leaves[0]; // 葉ノード

            int mid = leaves.Count / 2;
            var left = Build(leaves.GetRange(0, mid));
            var right = Build(leaves.GetRange(mid, leaves.Count - mid));

            var parent = new BVHNode();
            parent.children.Add(left);
            parent.children.Add(right);
            parent._bounds = MergeBounds(parent.children);

            return parent;
        }
    }

    /// <summary>
    /// ノードの構造
    /// </summary>
    public class BVHNode
    {
        public Bounds _bounds;
        public List<BVHNode> children = new();
        public int triangleIndex = -1; // 葉ノードなら三角形IDを入れる

        public bool IsLeaf => triangleIndex >= 0;
    }
}