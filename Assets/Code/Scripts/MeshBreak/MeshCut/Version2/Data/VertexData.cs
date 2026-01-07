using Unity.Mathematics;

public struct VertexData
{
    public int ObjectId;
    public int SpaceId;
    public int SubmeshId;
    public int Position;
    public int Normal;
    public int Uv;
}

public struct TriangleData
{
    public VertexData Vertex1;
    public VertexData Vertex2;
    public VertexData Vertex3;

    public TriangleData(VertexData v1, VertexData v2, VertexData v3)
    {
        Vertex1 = v1;
        Vertex2 = v2;
        Vertex3 = v3;
    }
}