using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class BurstCutScheduler
{
    private const int TRIANGLES_CLASSIFY = 8;

    private MeshCutContext heavyMeshCutContext;

    /// <summary>
    /// 軽量なメッシュを切断するためのコード
    /// </summary>
    /// <param name="blade"></param>
    /// <param name="mesh"></param>
    /// <param name="transforms"></param>
    /// <returns></returns>
    public MeshCutContext SchedulingCutLight(NativePlane blade, CuttableObject[] cuttables)
    {
        Stopwatch st = Stopwatch.StartNew();

        var context = new MeshCutContext();

        return context;
    }
}