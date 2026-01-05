using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// データを渡す、あるいは受け取るときのフォーマット
/// </summary>
public struct NativeMeshData
{
    public NativeParallelHashMap<int, float3> Vertices;
    public NativeParallelHashMap<int, float3> Normals;
    public NativeParallelHashMap<int, float2> Uvs;
    public NativeList<SubmeshTriangleData> SubMesh;

    public int SubMeshCount;


    public NativeMeshData(
        NativeParallelHashMap<int, float3> vertices,
        NativeParallelHashMap<int, float3> normals,
        NativeParallelHashMap<int, float2> uvs,
        NativeList<SubmeshTriangleData> subMesh, int subMeshCount)
    {
        Vertices = vertices;
        Normals = normals;
        Uvs = uvs;
        SubMesh = subMesh;
        SubMeshCount = subMeshCount;
    }

    /// <summary>
    /// マルチスレッドからの書き込みを要するときに使用する
    /// </summary>
    public struct ParallelWriter
    {
        public NativeParallelHashMap<int, float3>.ParallelWriter Vertices;
        public NativeParallelHashMap<int, float3>.ParallelWriter Normals;
        public NativeParallelHashMap<int, float2>.ParallelWriter Uvs;
        public NativeList<SubmeshTriangleData>.ParallelWriter SubMesh;

        public ParallelWriter(
            NativeParallelHashMap<int, float3>.ParallelWriter vertices,
            NativeParallelHashMap<int, float3>.ParallelWriter normals,
            NativeParallelHashMap<int, float2>.ParallelWriter uvs,
            NativeList<SubmeshTriangleData>.ParallelWriter subMesh)
        {
            Vertices = vertices;
            Normals = normals;
            Uvs = uvs;
            SubMesh = subMesh;
        }

        /// <summary>
        /// 三角形データを追加する。浮動小数点誤差を丸め込んだハッシュ値により重複防止を行う
        /// </summary>
        /// <param name="v1">頂点１</param>
        /// <param name="v2">頂点２</param>
        /// <param name="v3">頂点３</param>
        /// <param name="submesh">サブメッシュ番号</param>
        /// <param name="quantizationPrecision">浮動小数点誤差丸め込みの精度。量子化グリッドの量子化量</param>
        public void AddTriangle(NativeVertexData v1, NativeVertexData v2, NativeVertexData v3, int submesh,
            int quantizationPrecision)
        {
            int k1 = NativeVertexData.GenerateKey(v1, submesh, quantizationPrecision);
            int k2 = NativeVertexData.GenerateKey(v2, submesh, quantizationPrecision);
            int k3 = NativeVertexData.GenerateKey(v3, submesh, quantizationPrecision);

            if (Vertices.TryAdd(k1, v1.Vertex))
            {
                Normals.TryAdd(k1, v1.Normal);
                Uvs.TryAdd(k1, v1.Uv);
            }

            if (Vertices.TryAdd(k2, v2.Vertex))
            {
                Normals.TryAdd(k2, v2.Normal);
                Uvs.TryAdd(k2, v2.Uv);
            }

            if (Vertices.TryAdd(k3, v3.Vertex))
            {
                Normals.TryAdd(k3, v3.Normal);
                Uvs.TryAdd(k3, v3.Uv);
            }

            // 3. 三角形データの登録
            SubMesh.AddNoResize(new SubmeshTriangleData
            {
                Index0 = k1,
                Index1 = k2,
                Index2 = k3,
                SubmeshId = submesh
            });
        }
    }
}

public struct GridVertexData
{
    public float3 Position;
    public int OriginalIndex;
}

/// <summary>
/// 三角形メッシュのインデックス番号を三角形ごとにまとめて保持する
/// </summary>
public struct SubmeshTriangleData
{
    public int Index0;
    public int Index1;
    public int Index2;

    public int SubmeshId;
}

/// <summary>
/// ユニークなハッシュ値を生成するための構造体
/// </summary>
public struct NativeVertexData
{
    public float3 Vertex;
    public float3 Normal;
    public float2 Uv;

    public static int GenerateKey(NativeVertexData vertexData, int submesh, int quantizationPrecision)
    {
        // 座標の丸め込み
        int3 qPos = (int3)math.round(vertexData.Vertex * quantizationPrecision);

        // 2. 各要素をハッシュ化して混ぜ合わせる
        // math.hash は float2, float3, int3 などを直接引数に取れます
        uint h1 = math.hash(qPos);
        uint h2 = math.hash(vertexData.Normal);
        uint h3 = math.hash(vertexData.Uv);

        // 3. 最終的な1つのハッシュ値にまとめる
        return (int)math.hash(new uint4(h1, h2, h3, (uint)submesh));
    }
}

public struct NativeTriangleDetailData
{
    public NativeVertexData V0;
    public NativeVertexData V1;
    public NativeVertexData V2;
    public int Submesh;
    public int SoloVertex;

    public NativeTriangleDetailData(NativeVertexData v0, NativeVertexData v1, NativeVertexData v2, 
        int submesh, int soloVertex)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
        Submesh = submesh;
        SoloVertex = soloVertex;
    }
}

public struct NativePlane
{
    public float3 Position;
    public float3 Normal;

    public NativePlane(float3 position, float3 normal)
    {
        Position = position;
        Normal = normal;
    }

    /// <summary>
    /// 面の法線方向にあれば1、法線の反対または面上にあれば0を返す
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public readonly int GetSide(float3 position)
    {
        float d = math.dot(position - Position, Normal);
        return d > 0.0f ? 1 : 0;
        // return math.select(0, 1, d > 0);
    }
}