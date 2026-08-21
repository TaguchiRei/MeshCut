using UnityEngine;

namespace UsefulMesh.Application
{
    /// <summary>
    /// SDFデータの永続化（ScriptableObject保存）の抽象化
    /// </summary>
    public interface ISdfAssetRepository
    {
        void SaveAsset(float[] sdfData, Vector3 boundsMin, Vector3 boundsMax, int resolution, string meshName);
    }
}