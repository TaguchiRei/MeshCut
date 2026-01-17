using System;
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
        int ptrSize = UnsafeUtility.SizeOf<IntPtr>() * _arrayCount;   // _dataPointers
        int offsetSize = UnsafeUtility.SizeOf<int>() * _arrayCount;  // _arrayOffsets
        int lutSize = UnsafeUtility.SizeOf<int>() * _totalLength;    // _indexToArrayId
        
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