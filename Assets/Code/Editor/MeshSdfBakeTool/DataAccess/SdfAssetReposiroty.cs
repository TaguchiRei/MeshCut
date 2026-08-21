// DataAccess/SdfAssetRepository.cs

using System.IO;
using UnityEngine;
using UnityEditor;
using UsefulMesh.Application;

namespace UsefulMesh.DataAccess
{
    public class SdfAssetRepository : ISdfAssetRepository
    {
        public void SaveAsset(float[] sdfData, Vector3 boundsMin, Vector3 boundsMax, int resolution, string meshName)
        {
            // Texture3Dの作成 (R2026年時点でもエディタでのTexture3D生成の基本は不変。RFloatでSDF値を格納)
            Texture3D texture = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            // データをネイティブ配列かColor/floatの配列としてセット
            texture.SetPixelData(sdfData, 0);
            texture.Apply();

            // ScriptableObjectの構築
            MeshSDFAsset asset = ScriptableObject.CreateInstance<MeshSDFAsset>();
            asset.sdfTexture = texture;
            asset.boundsMin = boundsMin;
            asset.boundsMax = boundsMax;
            asset.resolution = resolution;

            // 保存パスの設定 (Assets直下に生成)
            string directory = "Assets/SdfAssets";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string assetPath = $"{directory}/SDF_{meshName}.asset";

            // 既存アセットがあればテクスチャをネストしてリークを防ぐ
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.AddObjectToAsset(texture, asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}