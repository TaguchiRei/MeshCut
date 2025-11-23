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
        public List<EdgeData> Edges;
    }

    [Serializable]
    public class EdgeData
    {
        public int Start;
        public int End;
    }
}