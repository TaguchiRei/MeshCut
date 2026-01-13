using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 処理に使う各種配列の初期化を行う
/// </summary>
[BurstCompile]
public struct ArrayInitializeJob : IJob
{
    [ReadOnly] public NativeArray<NativeTransform> Transforms;
    [ReadOnly] public Mesh.MeshDataArray MeshDataArray;
    [ReadOnly] public NativePlane Blade;

    [WriteOnly] public NativeList<NativeTriangle> Triangles;
    [WriteOnly] public NativeArray<NativePlane> LocalBlades;
    [WriteOnly] public NativeArray<int> VerticesObjectIdList;


    [BurstCompile]
    public void Execute()
    {
        int verticesObjectIdCount = 0;
        for (int i = 0; i < MeshDataArray.Length; i++)
        {
            var meshData = MeshDataArray[i];

            //サブメッシュごとの処理
            for (int j = 0; j < meshData.subMeshCount; j++)
            {
                SubMeshDescriptor submeshDescriptor = meshData.GetSubMesh(j);

                // インデックス形式に応じて取得
                if (meshData.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16)
                {
                    var indices = meshData.GetIndexData<ushort>()
                        .Slice(submeshDescriptor.indexStart, submeshDescriptor.indexCount);
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
                else // IndexFormat.UInt32
                {
                    var indices = meshData.GetIndexData<int>()
                        .Slice(submeshDescriptor.indexStart, submeshDescriptor.indexCount);
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

            //オブジェクトごとの処理(Bladeの初期化)
            quaternion invRot = math.inverse(Transforms[i].Rotation);
            float3 reciprocal = math.rcp(Transforms[i].Scale);
            float3 bladePosition = math.mul(invRot, (Blade.Position - Transforms[i].Position)) * reciprocal;
            float3 bladeNormal = math.mul(invRot, Blade.Normal) * reciprocal;
            LocalBlades[i] = new NativePlane(bladePosition, bladeNormal);

            for (int j = 0; j < meshData.vertexCount; j++)
            {
                VerticesObjectIdList[verticesObjectIdCount++] = i;
            }
        }
    }
}