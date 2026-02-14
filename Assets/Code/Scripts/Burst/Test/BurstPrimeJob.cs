using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UsefulAttribute;
using Debug = UnityEngine.Debug;

public class BurstPrimeJob : MonoBehaviour
{
    [SerializeField] int _checkNum;

    [MethodExecutor]
    private void Test()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        NativeList<int> primes = new NativeList<int>(Allocator.Temp);

        // Job化された同期API呼び出し
        PrimeJobUtility.GetPrimesUpTo(_checkNum, ref primes);

        List<int> managed = new List<int>(primes.Length);
        for (int i = 0; i < primes.Length; i++)
        {
            managed.Add(primes[i]);
        }

        primes.Dispose();

        stopwatch.Stop();
        Debug.Log($"{stopwatch.ElapsedMilliseconds} ms");
    }
}


[BurstCompile]
public static class PrimeJobUtility
{
    public static void GetPrimesUpTo(int maxNumber, ref NativeList<int> primes)
    {
        primes.Clear();
        if (maxNumber < 2) return;

        // 2〜maxNumber を index 0 起点にする
        int count = maxNumber - 1;

        NativeArray<bool> isPrimeFlags =
            new NativeArray<bool>(count, Allocator.TempJob);

        PrimeCheckJob job = new PrimeCheckJob
        {
            Results = isPrimeFlags
        };

        // Job スケジュール
        JobHandle handle = job.Schedule(count, 64);

        // 即 Complete（同期利用）
        handle.Complete();

        // 結果回収（メインスレッド）
        for (int i = 0; i < count; i++)
        {
            if (isPrimeFlags[i])
            {
                primes.Add(i + 2);
            }
        }

        isPrimeFlags.Dispose();
    }

    [BurstCompile]
    private struct PrimeCheckJob : IJobParallelFor
    {
        public NativeArray<bool> Results;

        public void Execute(int index)
        {
            int n = index + 2;
            bool isPrime = true;

            int sqrt = (int)math.sqrt(n);
            for (int i = 2; i <= sqrt; i++)
            {
                if (n % i == 0)
                {
                    isPrime = false;
                    break;
                }
            }

            Results[index] = isPrime;
        }
    }
}