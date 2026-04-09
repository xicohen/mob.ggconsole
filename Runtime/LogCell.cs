#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Mob404.Console
{
    /// <summary>
    /// Component tren moi cell log trong console
    /// </summary>
    public sealed class LogCell : MonoBehaviour
    {
        [SerializeField] private Text? textContent;

        private static readonly Color LogColor = Color.white;
        private static readonly Color WarningColor = new Color(1f, 0.92f, 0.016f);
        private static readonly Color ErrorColor = Color.red;

        private LogEntry? _entry;

        public LogEntry? Entry => _entry;

        /// <summary>
        /// Gan data vao cell, set mau theo LogType
        /// </summary>
        public void Bind(LogEntry entry)
        {
            _entry = entry;

            if (textContent != null)
            {
                textContent.text = entry.LogType is LogType.Error or LogType.Exception
                    ? $"=================\n{entry.StackTrace}\n{entry.Message}"
                    : entry.Message;

                textContent.color = entry.LogType switch
                {
                    LogType.Warning => WarningColor,
                    LogType.Error => ErrorColor,
                    LogType.Exception => ErrorColor,
                    _ => LogColor
                };
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Reset cell khi tra ve pool
        /// </summary>
        public void ResetCell()
        {
            _entry = null;

            if (textContent != null)
                textContent.text = string.Empty;

            gameObject.SetActive(false);
        }
    }
}
