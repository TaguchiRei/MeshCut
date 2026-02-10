using ScriptedTalk;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    public Material CapMaterial;
    [ShowOnly] public Mesh Mesh;

    private void Start()
    {
        Mesh = gameObject.GetComponent<MeshFilter>().mesh;
    }
}