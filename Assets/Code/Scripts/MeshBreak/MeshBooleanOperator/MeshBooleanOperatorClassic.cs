using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Scripts.MeshBreak.MeshBooleanOperator
{
    public class MeshBooleanOperatorClassic
    {
        public UniTask<GameObject> Boolean(GameObject target, GameObject booleanMesh, Material capMaterial)
        {
            return UniTask.FromResult(booleanMesh);
        }
    }
}