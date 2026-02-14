using Unity.Mathematics;

public struct NewTriangleDataL3
{
    public VertexRef V0, V1, V2;

    public int Submesh;
    public int ObjectId;
    public int IsFrontSide;

    public NewTriangleDataL3(
        VertexRef v0, VertexRef v1, VertexRef v2,
        int submesh, int objectId, int isFrontSide)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
        Submesh = submesh;
        ObjectId = objectId;
        IsFrontSide = isFrontSide;
    }
}

public struct VertexRef
{
    private readonly int _rawData;

    public VertexRef(int index, bool isNew)
    {
        // 新規頂点なら負数（-1, -2...）、既存なら正数（0, 1...）に変換
        _rawData = isNew ? -(index + 1) : index;
    }

    public bool IsNew => _rawData < 0;

    public int Index => _rawData < 0 ? math.abs(_rawData) - 1 : _rawData;

    // Burstで使いやすいように明示的なキャストやヘルパーを用意
    public static VertexRef Existing(int index) => new VertexRef(index, false);
    public static VertexRef New(int index) => new VertexRef(index, true);
}