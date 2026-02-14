using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


public unsafe struct NativeMultiArrayView<T> : IDisposable where T : unmanaged
{
    // 各配列の先頭ポインタを保持
    [NativeDisableUnsafePtrRestriction] private readonly T** _dataPointers;

    // 各配列の全体における開始インデックスを保持
    [NativeDisableUnsafePtrRestriction] private readonly int* _arrayOffsets;

    private readonly int _arrayCount;
    private readonly int _totalLength;
    private readonly Allocator _allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private AtomicSafetyHandle _safety;
#endif

    public int Length => _totalLength;

    public NativeMultiArrayView(NativeArray<T>[] arrays, Allocator allocator)
    {
        _arrayCount = arrays.Length;
        _allocator = allocator;

        int ptrSize = UnsafeUtility.SizeOf<IntPtr>() * _arrayCount;
        int offsetSize = UnsafeUtility.SizeOf<int>() * _arrayCount;
        byte* buffer = (byte*)UnsafeUtility.Malloc(ptrSize + offsetSize, 16, allocator);

        _dataPointers = (T**)buffer;
        _arrayOffsets = (int*)(buffer + ptrSize);

        int currentOffset = 0;
        for (int i = 0; i < _arrayCount; i++)
        {
            _dataPointers[i] = (T*)arrays[i].GetUnsafeReadOnlyPtr();
            _arrayOffsets[i] = currentOffset;
            currentOffset += arrays[i].Length;
        }

        _totalLength = currentOffset;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        _safety = AtomicSafetyHandle.Create();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetArrayId(int index)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if ((uint)index >= (uint)_totalLength) throw new IndexOutOfRangeException();
#endif
        // 二分探索で index が所属する配列 ID を特定
        int low = 0;
        int high = _arrayCount - 1;

        while (low <= high)
        {
            int mid = (low + high) >> 1;
            if (index < _arrayOffsets[mid])
            {
                high = mid - 1;
            }
            else if (mid + 1 < _arrayCount && index >= _arrayOffsets[mid + 1])
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }

        return 0;
    }

    public T this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(_safety);
#endif
            int arrayId = GetArrayId(index);
            return _dataPointers[arrayId][index - _arrayOffsets[arrayId]];
        }
    }

    public T this[int arrayId, int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(_safety);
#endif
            return _dataPointers[arrayId][index - _arrayOffsets[arrayId]];
        }
    }

    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.Release(_safety);
#endif
        if (_dataPointers != null)
        {
            UnsafeUtility.Free(_dataPointers, _allocator);
        }
    }
}