using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct BuildObjectMatrixJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<NativeTransform> ObjectTransforms;

    public NativeArray<float4x4> ObjectMatrices;

    public void Execute(int index)
    {
        var t = ObjectTransforms[index];
        
        var scaleMatrix = float4x4.Scale(t.Scale);
        var rotationMatrix = new float4x4(new quaternion(t.Rotation), float3.zero);
        var translationMatrix = float4x4.Translate(t.Position);

        ObjectMatrices[index] = math.mul(translationMatrix, math.mul(rotationMatrix, scaleMatrix));
    }
}