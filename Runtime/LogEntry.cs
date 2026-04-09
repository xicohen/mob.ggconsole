#nullable enable

using UnityEngine;

namespace Mob404.Console
{
    /// <summary>
    /// Data model cho 1 dong log
    /// </summary>
    public sealed class LogEntry
    {
        public string Message { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }
        public int Index { get; }

        public LogEntry(string message, string stackTrace, LogType logType, int index)
        {
            Message = message;
            StackTrace = stackTrace;
            LogType = logType;
            Index = index;
        }

        public string FullText => string.IsNullOrEmpty(StackTrace)
            ? Message
            : $"{Message}\n{StackTrace}";
    }
}
