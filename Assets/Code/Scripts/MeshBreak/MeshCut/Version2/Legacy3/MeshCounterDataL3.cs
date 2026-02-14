public class MeshCounterDataL3
{
    public NativeMeshDataL3 MeshDataL3;
    public int Counter;
    public int UseCounter;

    public MeshCounterDataL3(NativeMeshDataL3 meshDataL3)
    {
        MeshDataL3 = meshDataL3;
        Counter = 1;
        UseCounter = 0;
    }
}