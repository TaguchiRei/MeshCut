using UnityEngine;

public abstract class MeshCutBase : MonoBehaviour
{
    public abstract GameObject[] Cut(GameObject target, Plane blade, Material capMaterial);
}