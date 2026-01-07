using System;

public struct SubmeshTriangle : IEquatable<SubmeshTriangle>
{
    public int Index0, Index1, Index2;
    public int SubmeshIndex;

    public bool Equals(SubmeshTriangle other)
    {
        return Index0 == other.Index0 && Index1 == other.Index1 && Index2 == other.Index2 &&
               SubmeshIndex == other.SubmeshIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is SubmeshTriangle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index0, Index1, Index2, SubmeshIndex);
    }
}