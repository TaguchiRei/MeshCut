using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 配列の初期化を行う
/// </summary>
[BurstCompile]
public struct ArrayInitializeBurst
{
    public NativeArray<NativeTransform> Transforms;
    public Mesh.MeshDataArray MeshDataArray;
    public NativePlane Blade;

    [WriteOnly] public NativeList<NativeTriangle> Triangles;
    [WriteOnly] public NativeArray<NativePlane> Blades;
    
    
    [BurstCompile]
    public void InitializeArray()
    {
        for (int i = 0; i < MeshDataArray.Length; i++)
        {
            var meshData = MeshDataArray[i];

            //サブメッシュごとの処理
            for (int j = 0; j < meshData.subMeshCount; j++)
            {
                SubMeshDescriptor submeshDescriptor = meshData.GetSubMesh(j);
                int indexCount = submeshDescriptor.indexCount;
                NativeArray<int> indices =
                    meshData.GetIndexData<int>().GetSubArray(submeshDescriptor.indexStart, indexCount);

                for (int k = 0; k < indices.Length; k += 3)
                {
                    Triangles.AddNoResize(
                        new(
                            indices[k],
                            indices[k + 1],
                            indices[k + 2],
                            i,
                            j
                        ));
                }
            }
        }
    }
}