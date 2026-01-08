using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct BladeInitializeJob : IJobParallelFor
{
    [ReadOnly] public NativePlane Blade;

    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public NativeArray<quaternion> Quaternions;
    [ReadOnly] public NativeArray<float3> Scales;

    [WriteOnly] public NativeArray<NativePlane> LocalBlades;


    public void Execute(int index)
    {
        quaternion invRot = math.inverse(Quaternions[index]);

        //計算で複数回出るものを事前計算
        float3 reciprocal = math.rcp(Scales[index]);
        
        //座標を求める
        float3 position = Blade.Position - Positions[index];
        position = math.mul(invRot, position);
        position *= reciprocal;
        
        //法線を求める
        float3 normal = math.mul(invRot, Blade.Normal);
        normal *= reciprocal;

        // Plane 再構築
        LocalBlades[index] = new NativePlane(position, normal);
    }
}