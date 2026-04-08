#nullable enable

using UnityEngine;

namespace GGConsolePackage
{
    /// <summary>
    /// Data model cho 1 dong log
    /// </summary>
    public sealed class GGLogEntry
    {
        public string Message { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }
        public int Index { get; }

        public GGLogEntry(string message, string stackTrace, LogType logType, int index)
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
