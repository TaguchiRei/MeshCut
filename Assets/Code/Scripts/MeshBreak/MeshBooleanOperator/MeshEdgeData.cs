using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshBooleanOperator
{
    [CreateAssetMenu(fileName = "MeshBreak", menuName = "ScriptableObject/MeshEdgeData")]
    public class MeshEdgeDataList : ScriptableObject
    {
        public List<MeshEdgeData> data;
    }

    [Serializable]
    public class MeshEdgeData
    {
        public int Hash;
        public List<EdgeData> Edges;
    }

    [Serializable]
    public class EdgeData : IEquatable<EdgeData>
    {
        public readonly int Start;
        public readonly int End;

        public EdgeData(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool Equals(EdgeData other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return (Start == other.Start && End == other.End) || (Start == other.End && End == other.Start);
        }

        public override bool Equals(object obj) => Equals(obj as EdgeData);

        public override int GetHashCode()
        {
            int min = Math.Min(Start, End);
            int max = Math.Max(Start, End);
            return HashCode.Combine(min, max);
        }
    }
}