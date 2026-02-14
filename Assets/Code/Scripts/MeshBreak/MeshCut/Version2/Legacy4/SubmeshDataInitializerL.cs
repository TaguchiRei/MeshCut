using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

public class SubmeshDataInitializerL : MonoBehaviour
{
    public CuttableObjectL[] cuttableObjects;

    private void Start()
    {
        cuttableObjects = GetComponentsInChildren<CuttableObjectL>();
        cuttableObjects = cuttableObjects.OrderBy(c => c.mesh.vertexCount).ToArray();
    }

    private async UniTask InitializeSubmeshData()
    {
        
    }
}