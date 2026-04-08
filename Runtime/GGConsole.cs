#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GGConsolePackage
{
    /// <summary>
    /// In-game debug console - hien thi Unity log runtime tren man hinh
    /// </summary>
    public sealed class GGConsole : MonoBehaviour
    {
        [SerializeField] private Canvas? canvasConsole;
        [SerializeField] private ScrollRect? scrollRect;
        [SerializeField] private Transform? textLogContainer;
        [SerializeField] private Text? textRecordStatus;
        [SerializeField] private GameObject? prefabTextLog;
        [SerializeField] private Transform? poolParent;
        [SerializeField] private InputField? inputSearch;
        [SerializeField] private Toggle? toggleFilterLog;
        [SerializeField] private Toggle? toggleFilterError;
        [SerializeField] private Text? textResize;
        [SerializeField] private RectTransform? consoleRect;
        [SerializeField] private GameObject? buttonUp;
        [SerializeField] private GameObject? buttonDown;

        private const int MAX_ENTRIES = 500;
        private const int TRIM_COUNT = 100;

        private static readonly float[] SIZE_RATIOS = { 1f, 0.5f, 0.33f };
        private static readonly string[] SIZE_LABELS = { "Full", "1/2", "1/3" };

        private GameObject? _ownedEventSystem;
        private GGLogPool? _pool;
        private readonly List<GGLogEntry> _allEntries = new();
        private bool _recordLogOn = true;
        private bool _filterLog = true;
        private bool _filterError = true;
        private string _searchKeyword = "";
        private int _sizeMode = 1;
        private bool _anchorTop = true;
        private int _entryIndex;

        private void Start()
        {
            EnsureEventSystem();
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            _pool = new GGLogPool(prefabTextLog!, textLogContainer!, poolParent!);
            _pool.Prewarm();

            // Dong bo state tu Toggle prefab
            if (toggleFilterLog != null)
            {
                _filterLog = toggleFilterLog.isOn;
                toggleFilterLog.onValueChanged.AddListener(OnFilterLogChanged);
            }
            if (toggleFilterError != null)
            {
                _filterError = toggleFilterError.isOn;
                toggleFilterError.onValueChanged.AddListener(OnFilterErrorChanged);
            }
            if (inputSearch != null)
                inputSearch.onValueChanged.AddListener(OnSearchChanged);

            CheckRecordStatus();
            ApplyResize();
            prefabTextLog!.SetActive(false);
        }

        public static void ShowScreenConsole()
        {
            var existing = FindAnyObjectByType<GGConsole>();
            if (existing != null) return;

            var prefab = Resources.Load<GameObject>("GGConsole");
            if (prefab == null)
                throw new InvalidOperationException("GGConsole prefab not found in Resources");

            var goConsole = Instantiate(prefab);
            goConsole.name = "GGConsole";
            DontDestroyOnLoad(goConsole);
        }

        public static void CloseScreenConsole()
        {
            var existing = FindAnyObjectByType<GGConsole>();
            if (existing != null) Destroy(existing.gameObject);
        }

        private void OnEnable() => Application.logMessageReceived += HandleLog;

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #region Button Handlers

        public void ButtonStopContinueClick()
        {
            _recordLogOn = !_recordLogOn;
            CheckRecordStatus();
        }

        public void ButtonClearClick()
        {
            _allEntries.Clear();
            _pool?.RecycleAll();
        }

        public void ButtonCloseClick() => CloseScreenConsole();

        public void OnFilterLogChanged(bool isOn)
        {
            _filterLog = isOn;
            RebuildVisibleCells();
        }

        public void OnFilterErrorChanged(bool isOn)
        {
            _filterError = isOn;
            RebuildVisibleCells();
        }

        public void ButtonCopyClick()
        {
            if (_pool == null || _pool.ActiveCount == 0) return;

            var sb = new System.Text.StringBuilder();
            foreach (var cell in _pool.ActiveCells)
            {
                if (cell.Entry != null)
                    sb.AppendLine(cell.Entry.FullText);
            }

            GUIUtility.systemCopyBuffer = sb.ToString();

            if (textRecordStatus != null)
                StartCoroutine(ShowCopiedFeedback());
        }

        public void ButtonResizeClick()
        {
            _sizeMode = (_sizeMode + 1) % SIZE_RATIOS.Length;
            ApplyResize();
        }

        public void ButtonUpClick()
        {
            _anchorTop = true;
            ApplyResize();
        }

        public void ButtonDownClick()
        {
            _anchorTop = false;
            ApplyResize();
        }

        public void OnSearchChanged(string keyword)
        {
            _searchKeyword = keyword ?? "";
            RebuildVisibleCells();
        }

        #endregion

        #region Core Logic

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!_recordLogOn) return;
            if (type != LogType.Log && type != LogType.Warning
                && type != LogType.Error && type != LogType.Exception) return;

            var entry = new GGLogEntry(logString, stackTrace, type, _entryIndex++);
            _allEntries.Add(entry);

            if (_allEntries.Count > MAX_ENTRIES)
            {
                _allEntries.RemoveRange(0, TRIM_COUNT);
                RebuildVisibleCells();
                return;
            }

            if (!PassesFilter(entry)) return;

            var cell = _pool!.Get();
            cell.Bind(entry);

            StartCoroutine(DelayMoveToBottom());
        }

        private void RebuildVisibleCells()
        {
            _pool?.RecycleAll();

            foreach (var entry in _allEntries)
            {
                if (!PassesFilter(entry)) continue;

                var cell = _pool!.Get();
                cell.Bind(entry);
            }

            StartCoroutine(DelayMoveToBottom());
        }

        private bool PassesFilter(GGLogEntry entry)
        {
            var typePass = entry.LogType switch
            {
                LogType.Log => _filterLog,
                LogType.Warning => _filterLog,
                LogType.Error => _filterError,
                LogType.Exception => _filterError,
                _ => false
            };

            if (!typePass) return false;

            if (!string.IsNullOrEmpty(_searchKeyword)
                && entry.Message.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }

        #endregion

        #region Helpers

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                // Scene da co EventSystem, xoa cai minh tao truoc do (neu co)
                if (_ownedEventSystem != null)
                {
                    Destroy(_ownedEventSystem);
                    _ownedEventSystem = null;
                }
                return;
            }

            // Chua co EventSystem nao, tao moi va giu DontDestroyOnLoad
            if (_ownedEventSystem == null)
            {
                _ownedEventSystem = new GameObject("EventSystem (GGConsole)");
                _ownedEventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                _ownedEventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                _ownedEventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                DontDestroyOnLoad(_ownedEventSystem);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureEventSystem();

        private void CheckRecordStatus() =>
            textRecordStatus!.text = _recordLogOn ? "Stop" : "Cont";

        private void ApplyResize()
        {
            if (consoleRect == null) return;

            var ratio = SIZE_RATIOS[_sizeMode];

            if (_anchorTop)
            {
                consoleRect.anchorMin = new Vector2(0f, 1f - ratio);
                consoleRect.anchorMax = Vector2.one;
            }
            else
            {
                consoleRect.anchorMin = Vector2.zero;
                consoleRect.anchorMax = new Vector2(1f, ratio);
            }

            consoleRect.offsetMin = Vector2.zero;
            consoleRect.offsetMax = Vector2.zero;

            if (textResize != null)
                textResize.text = SIZE_LABELS[_sizeMode];

            var isFullScreen = _sizeMode == 0;
            if (buttonUp != null)
                buttonUp.SetActive(!isFullScreen && !_anchorTop);
            if (buttonDown != null)
                buttonDown.SetActive(!isFullScreen && _anchorTop);
        }

        private IEnumerator DelayMoveToBottom()
        {
            yield return null;
            scrollRect!.normalizedPosition = new Vector2(0, 0f);
        }

        private IEnumerator ShowCopiedFeedback()
        {
            var originalText = textRecordStatus!.text;
            textRecordStatus.text = "Copied!";
            yield return new WaitForSeconds(1f);
            textRecordStatus.text = originalText;
        }

        #endregion
    }
}
