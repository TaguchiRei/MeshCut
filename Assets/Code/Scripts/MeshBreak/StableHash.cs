using System;
using System.Text;

public static class StableHash
{
    public static int GetHash(string s)
    {
        unchecked
        {
            uint hash = 2166136261u; // FNV-1a 初期値
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619;
            }

            return (int)hash;
        }
    }
}