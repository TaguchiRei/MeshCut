using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UsefllAttribute;
using Debug = UnityEngine.Debug;

public class Prime : MonoBehaviour
{
    [SerializeField] private int checkNum;

    [MethodExecutor]
    private void Test()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        FindPrimeNumber(checkNum);
        stopwatch.Stop();
        Debug.Log($"{stopwatch.ElapsedMilliseconds} ms");
    }

    public int[] FindPrimeNumber(int n)
    {
        List<int> list = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (IsPrime(i))
            {
                list.Add(i);
            }
        }

        return list.ToArray();
    }

    private bool IsPrime(int number)
    {
        if (number == 2) return true;
        if (number < 2 ||
            number % 2 == 0)
        {
            return false;
        }

        for (int i = 3; i * i <= number; i += 2)
        {
            if (number % i == 0)
            {
                return false;
            }
        }

        return true;
    }
}