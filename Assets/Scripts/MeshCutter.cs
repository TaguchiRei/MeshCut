using UnityEngine;

public class MeshCutter : MonoBehaviour
{
    void Start()
    {
        var plane = new Plane(transform.up, transform.position);
    }
}
