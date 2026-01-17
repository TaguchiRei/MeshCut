using UnityEngine;

public struct NativeTriangle
{
    public int Vertex1;
    public int Vertex2;
    public int Vertex3;
    
    public int SubmeshId;

    public NativeTriangle(int vertex1, int vertex2, int vertex3,  int submeshId)
    {
        Vertex1 = vertex1;
        Vertex2 = vertex2;
        Vertex3 = vertex3;
        SubmeshId = submeshId;
    }
}