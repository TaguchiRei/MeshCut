using System.Collections.Generic;
using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    /// <summary>
    /// ステージに登場するメッシュを登録するScriptableObject
    /// ステージごとに1つ作成してアサインする
    /// </summary>
    [CreateAssetMenu(fileName = "MeshRegistry", menuName = "MeshBreak/MeshRegistry")]
    public class MeshRegistry : ScriptableObject
    {
        [SerializeField] private List<Mesh> _meshes = new();
        public IReadOnlyList<Mesh> Meshes => _meshes;
    }
}