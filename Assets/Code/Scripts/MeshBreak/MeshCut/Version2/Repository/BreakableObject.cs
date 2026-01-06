using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshFilter;
    public Mesh BreakableMesh => _meshFilter.mesh;
}