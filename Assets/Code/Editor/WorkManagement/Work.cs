using System;

[Serializable]
public class Work
{
    public string WorkName;
    public DateTime Start;
    public DateTime End;
    public bool IsWorking = false;

    public Work(string workName, DateTime start, DateTime end, bool isWorking = false)
    {
        WorkName = workName;
        Start = start;
        End = end;
        IsWorking = isWorking;
    }

    public override string ToString()
    {
        return $"{WorkName}#{Start:yyyy-MM-dd HH:mm:ss.fff}#{End:yyyy-MM-dd HH:mm:ss.fff}#{IsWorking}";
    }
}