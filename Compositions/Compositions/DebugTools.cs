using System.Collections.Generic;
using System.Text;

namespace Compositions;

public static class DebugTools
{
    private static List<Log> _logs = new List<Log>();

    public static void Log(string message)
    {
        _logs.Add(new Log()
        {
            Message = message
        });
    }

    public static string Flush()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var log in _logs)
        {
            sb.AppendLine(log.Message);
        }

        _logs.Clear();
        return sb.ToString();
    }
}

public struct Log
{
    public int Level;
    public string Message;
}