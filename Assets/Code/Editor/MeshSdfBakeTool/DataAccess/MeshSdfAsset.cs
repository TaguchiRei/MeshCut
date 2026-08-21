using UnityEngine;

namespace UsefulMesh.DataAccess
{
    public class MeshSDFAsset : ScriptableObject
    {
        public Texture3D sdfTexture;
        public Vector3 boundsMin;
        public Vector3 boundsMax;
        public int resolution;
    }
}