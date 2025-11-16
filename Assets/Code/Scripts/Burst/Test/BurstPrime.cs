using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Attribute;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BurstPrime : MonoBehaviour
{
    [SerializeField] private int _checkNum;

    [MethodExecutor]
    private void Test()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        int maxNumber = 50; // 例: 1~50までの素数判定
        NativeList<int> primes = new NativeList<int>(Allocator.Temp);

        // Burst Direct Call
        PrimeUtility.GetPrimesUpTo(maxNumber, ref primes);

        List<int> a = new List<int>(primes.Length);
        for (int i = 0; i < primes.Length; i++)
        {
            a.Add(primes[i]);
        }

        primes.Dispose();

        stopwatch.Stop();
        Debug.Log($"{stopwatch.ElapsedMilliseconds} ms");
    }
}


[BurstCompile]
public static class PrimeUtility
{
    [BurstCompile]
    public static void GetPrimesUpTo(int maxNumber, ref NativeList<int> primes)
    {
        primes.Clear();
        if (maxNumber < 2) return;

        for (int n = 2; n <= maxNumber; n++)
        {
            bool isPrime = true;
            int sqrtN = (int)math.sqrt(n);
            for (int i = 2; i <= sqrtN; i++)
            {
                if (n % i == 0)
                {
                    isPrime = false;
                    break;
                }
            }

            if (isPrime)
            {
                primes.Add(n);
            }
        }
    }
}