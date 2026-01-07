using Unity.Mathematics;

public struct VertexData
{
    public int ObjectId;
    public int SpaceId;
    public int SubmeshId;
    public int VertexId;
}

public struct TriangleData
{
    public VertexData Vertex0;
    public VertexData Vertex1;
    public VertexData Vertex2;

    public TriangleData(VertexData v1, VertexData v2, VertexData v3)
    {
        Vertex0 = v1;
        Vertex1 = v2;
        Vertex2 = v3;
    }
}