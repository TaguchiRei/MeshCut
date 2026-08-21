using UnityEngine;

namespace UsefulMesh.Application
{
    // インフラが継承するInput Port
    public interface IBakeInputPort
    {
        void ExecuteBake(Mesh targetMesh);
    }
}