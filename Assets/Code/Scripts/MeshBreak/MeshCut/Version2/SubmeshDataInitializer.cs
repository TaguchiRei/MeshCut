using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

public class SubmeshDataInitializer : MonoBehaviour
{
    public CuttableObject[] cuttableObjects;

    private void Start()
    {
        cuttableObjects = GetComponentsInChildren<CuttableObject>();
        cuttableObjects = cuttableObjects.OrderBy(c => c.mesh.vertexCount).ToArray();
    }

    private async UniTask InitializeSubmeshData()
    {
        
    }
}