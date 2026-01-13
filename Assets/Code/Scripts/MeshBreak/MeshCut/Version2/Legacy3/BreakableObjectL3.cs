using UnityEngine;

public class BreakableObjectL3 : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshFilter;
    public Mesh BreakableMesh => _meshFilter.mesh;

    public int HashCode = -1;
}