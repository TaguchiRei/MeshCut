using System;
using Unity.Collections;
using Unity.Mathematics;

public struct NativeBreakMeshData
{
    
}
public class BaseMeshData
{
    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeArray<float2> UVs;
}

public struct NativeNestedList : IDisposable
{
    /// <summary>
    /// 各リストを識別するIDをキー、中身を値とする。
    /// DictionaryのBurst対応、一つのキーに複数Valueを付与できるバージョン
    /// </summary>
    private NativeParallelMultiHashMap<int, int> dataMap;

    public NativeNestedList(int initialCapacity, Allocator allocator)
    {
        dataMap = new NativeParallelMultiHashMap<int, int>(initialCapacity, allocator);
    }

    /// <summary>
    /// 特定のリスト（listIndex）に値を追加します
    /// </summary>
    public void AddElement(int listIndex, int value)
    {
        dataMap.Add(listIndex, value);
    }

    /// <summary>
    /// 指定したリストの要素数を取得します
    /// </summary>
    public int GetCount(int listIndex)
    {
        return dataMap.CountValuesForKey(listIndex);
    }

    /// <summary>
    /// 書き込み専用のParallelWriterを取得します。Jobで並列書き込みする場合に必要です。
    /// </summary>
    public NativeParallelMultiHashMap<int, int>.ParallelWriter AsParallelWriter()
    {
        return dataMap.AsParallelWriter();
    }

    public void Dispose()
    {
        if (dataMap.IsCreated)
        {
            dataMap.Dispose();
        }
    }

    // 列挙用の列挙子を返すメソッドも定義可能
    public Enumerator GetEnumerator(int listIndex)
    {
        return new Enumerator(dataMap, listIndex);
    }
    
    public struct Enumerator
    {
        private NativeParallelMultiHashMap<int, int> _map;
        private int _key;
        private NativeParallelMultiHashMapIterator<int> _it;
        private bool _isFirst;

        public int Current { get; private set; }

        public Enumerator(NativeParallelMultiHashMap<int, int> map, int key)
        {
            _map = map;
            _key = key;
            _it = default;
            _isFirst = true;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_isFirst)
            {
                _isFirst = false;
                return _map.TryGetFirstValue(_key, out int value, out _it);
            }
            return _map.TryGetNextValue(out int val, ref _it);
        }
    }
}