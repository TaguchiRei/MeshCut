public class MeshCounterData
{
    public NativeMeshData MeshData;
    public int Counter;
    public int UseCounter;

    public MeshCounterData(NativeMeshData meshData)
    {
        MeshData = meshData;
        Counter = 1;
        UseCounter = 0;
    }
}