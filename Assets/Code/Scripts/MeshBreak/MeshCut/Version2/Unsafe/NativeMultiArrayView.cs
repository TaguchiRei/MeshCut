using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[NativeContainer]
[NativeContainerIsReadOnly]
public unsafe struct NativeMultiArrayView<T> : IDisposable where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction] private readonly T** _dataPointers;

    [NativeDisableUnsafePtrRestriction] private readonly int* _arrayOffsets;

    // NativeArrayをポインタ管理に変更
    [NativeDisableUnsafePtrRestriction] private readonly int* _indexToArrayId;

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

        // 全体サイズ計算
        int sum = 0;
        for (int i = 0; i < arrays.Length; i++) sum += arrays[i].Length;
        _totalLength = sum;

        // メモリの括確保
        int ptrSize = UnsafeUtility.SizeOf<IntPtr>() * _arrayCount; // _dataPointers
        int offsetSize = UnsafeUtility.SizeOf<int>() * _arrayCount; // _arrayOffsets
        int lutSize = UnsafeUtility.SizeOf<int>() * _totalLength; // _indexToArrayId

        byte* buffer = (byte*)UnsafeUtility.Malloc(ptrSize + offsetSize + lutSize, 16, allocator);

        _dataPointers = (T**)buffer;
        _arrayOffsets = (int*)(buffer + ptrSize);
        _indexToArrayId = (int*)(buffer + ptrSize + offsetSize);

        // データアクセス用対応配列初期化
        int currentOffset = 0;
        for (int i = 0; i < _arrayCount; i++)
        {
            _dataPointers[i] = (T*)arrays[i].GetUnsafeReadOnlyPtr();
            _arrayOffsets[i] = currentOffset;

            int len = arrays[i].Length;
            for (int j = 0; j < len; j++)
            {
                _indexToArrayId[currentOffset + j] = i;
            }

            currentOffset += len;
        }


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        _safety = AtomicSafetyHandle.Create();
#endif
    }
    
    public NativeMultiArrayView(NativeMultiArrayView<T> oldView, NativeArray<T> newArray, Allocator allocator)
    {
        _allocator = allocator;
        _arrayCount = oldView._arrayCount + 1;
        _totalLength = oldView._totalLength + newArray.Length;

        // メモリの一括確保
        int ptrSize = UnsafeUtility.SizeOf<IntPtr>() * _arrayCount;
        int offsetSize = UnsafeUtility.SizeOf<int>() * _arrayCount;
        int lutSize = UnsafeUtility.SizeOf<int>() * _totalLength;

        byte* buffer = (byte*)UnsafeUtility.Malloc(ptrSize + offsetSize + lutSize, 16, allocator);

        _dataPointers = (T**)buffer;
        _arrayOffsets = (int*)(buffer + ptrSize);
        _indexToArrayId = (int*)(buffer + ptrSize + offsetSize);

        // 既存データのコピー
        UnsafeUtility.MemCpy(_dataPointers, oldView._dataPointers,
            UnsafeUtility.SizeOf<IntPtr>() * oldView._arrayCount);
        UnsafeUtility.MemCpy(_arrayOffsets, oldView._arrayOffsets, UnsafeUtility.SizeOf<int>() * oldView._arrayCount);
        // LUTの既存部分をコピー
        UnsafeUtility.MemCpy(_indexToArrayId, oldView._indexToArrayId,
            UnsafeUtility.SizeOf<int>() * oldView._totalLength);

        // 新しい配列の追加
        int lastIdx = _arrayCount - 1;
        _dataPointers[lastIdx] = (T*)newArray.GetUnsafeReadOnlyPtr();
        _arrayOffsets[lastIdx] = oldView._totalLength; // 前のViewの合計長さを新たなオフセットとして利用

        // 新規LUT
        for (int i = 0; i < newArray.Length; i++)
        {
            _indexToArrayId[oldView._totalLength + i] = lastIdx;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        _safety = AtomicSafetyHandle.Create();
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetArrayId(int index)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(_safety);
        if ((uint)index >= (uint)_totalLength) throw new IndexOutOfRangeException();
#endif
        return _indexToArrayId[index];
    }

    public T this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(_safety);
            if ((uint)index >= (uint)_totalLength) throw new IndexOutOfRangeException();
#endif
            int arrayId = _indexToArrayId[index];
            return _dataPointers[arrayId][index - _arrayOffsets[arrayId]];
        }
    }

    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.Release(_safety);
#endif
        UnsafeUtility.Free(_dataPointers, _allocator);
    }
}