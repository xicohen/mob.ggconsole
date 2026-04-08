#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace GGConsolePackage
{
    /// <summary>
    /// Component tren moi cell log trong console
    /// </summary>
    public sealed class GGLogCell : MonoBehaviour
    {
        [SerializeField] private Text? textContent;

        private static readonly Color COLOR_LOG = Color.white;
        private static readonly Color COLOR_WARNING = new Color(1f, 0.92f, 0.016f);
        private static readonly Color COLOR_ERROR = Color.red;

        private GGLogEntry? _entry;

        public GGLogEntry? Entry => _entry;

        /// <summary>
        /// Gan data vao cell, set mau theo LogType
        /// </summary>
        public void Bind(GGLogEntry entry)
        {
            _entry = entry;

            if (textContent != null)
            {
                textContent.text = entry.LogType is LogType.Error or LogType.Exception
                    ? $"=================\n{entry.StackTrace}\n{entry.Message}"
                    : entry.Message;

                textContent.color = entry.LogType switch
                {
                    LogType.Warning => COLOR_WARNING,
                    LogType.Error => COLOR_ERROR,
                    LogType.Exception => COLOR_ERROR,
                    _ => COLOR_LOG
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
