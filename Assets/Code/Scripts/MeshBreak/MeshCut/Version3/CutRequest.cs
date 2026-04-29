// CutRequest.cs
// 切断リクエストの単位。キューに積む際に使う。

using UnityEngine;

namespace MeshBreak.MeshCut.Version3
{
    public readonly struct CutRequest
    {
        public readonly GameObject Target;
        public readonly Plane      Blade;
        public readonly Material[] OriginalMaterials;
        public readonly MeshCutInput Input;

        public CutRequest(
            GameObject target,
            Plane blade,
            Material[] originalMaterials,
            MeshCutInput input)
        {
            Target            = target;
            Blade             = blade;
            OriginalMaterials = originalMaterials;
            Input             = input;
        }
    }
}