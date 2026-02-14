using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

[Serializable]
public class WorkManagement
{
    public List<Work> Works = new();

    public void AddWork(Work work)
    {
        Works.Add(work);
    }

    public bool TryGetLastWork(string workName, out Work lastWork)
    {
        for (int i = Works.Count - 1; i >= 0; i--)
        {
            if (Works[i].WorkName == workName)
            {
                lastWork = Works[i];
                return true;
            }
        }

        lastWork = null;
        return false;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var work in Works)
        {
            sb.AppendLine(work.ToString());
        }

        return sb.ToString();
    }

    public void FromString(string data)
    {
        var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var workData = line.Split('#');
            Works.Add(new Work(workData[0],
                DateTime.ParseExact(workData[1], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture),
                DateTime.ParseExact(workData[2], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture),
                bool.Parse(workData[3])
            ));
        }
    }
}