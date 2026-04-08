#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;

namespace GGConsolePackage
{
    /// <summary>
    /// Keo GroupAction tren man hinh, tha ra se snap vao goc gan nhat
    /// </summary>
    public sealed class GGConsoleDrag : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform? dragTarget;
        [SerializeField] private Canvas? rootCanvas;
        [SerializeField] private float snapDuration = 0.15f;
        [SerializeField] private float edgePadding = 20f;

        private Vector2 _dragOffset;
        private RectTransform? _canvasRect;
        private Coroutine? _snapCoroutine;

        private void Awake()
        {
            if (rootCanvas != null)
                _canvasRect = rootCanvas.transform as RectTransform;
        }

        private void Start()
        {
            StartCoroutine(SetDefaultPosition());
        }

        private System.Collections.IEnumerator SetDefaultPosition()
        {
            yield return new WaitForEndOfFrame();

            if (dragTarget == null || _canvasRect == null) yield break;

            var corners = GetCorners();
            dragTarget.localPosition = corners[1]; // top-right
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragTarget == null || _canvasRect == null) return;

            if (_snapCoroutine != null)
            {
                StopCoroutine(_snapCoroutine);
                _snapCoroutine = null;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out var pointerLocal);

            _dragOffset = (Vector2)dragTarget.localPosition - pointerLocal;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragTarget == null || _canvasRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out var pointerLocal);

            dragTarget.localPosition = pointerLocal + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragTarget == null || _canvasRect == null) return;

            var targetPos = FindNearestCorner((Vector2)dragTarget.localPosition);
            _snapCoroutine = StartCoroutine(SnapTo(targetPos));
        }

        /// <summary>
        /// 0=top-left, 1=top-right, 2=bot-left, 3=bot-right
        /// </summary>
        private Vector2[] GetCorners()
        {
            var canvasSize = _canvasRect!.rect.size;
            var halfCanvas = canvasSize * 0.5f;
            var targetSize = dragTarget!.rect.size;
            var pivot = dragTarget.pivot;
            var offsetX = targetSize.x * pivot.x;
            var offsetY = targetSize.y * pivot.y;

            var left = -halfCanvas.x + edgePadding + offsetX;
            var right = halfCanvas.x - edgePadding - targetSize.x + offsetX;
            var bottom = -halfCanvas.y + edgePadding + offsetY;
            var top = halfCanvas.y - edgePadding - targetSize.y + offsetY;

            return new[]
            {
                new Vector2(left, top),
                new Vector2(right, top),
                new Vector2(left, bottom),
                new Vector2(right, bottom)
            };
        }

        private Vector2 FindNearestCorner(Vector2 currentPos)
        {
            var corners = GetCorners();
            var nearest = corners[0];
            var minDist = float.MaxValue;

            foreach (var corner in corners)
            {
                var dist = (currentPos - corner).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = corner;
                }
            }

            return nearest;
        }

        private System.Collections.IEnumerator SnapTo(Vector2 targetPos)
        {
            var startPos = (Vector2)dragTarget!.localPosition;
            var elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / snapDuration);
                dragTarget.localPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            dragTarget.localPosition = targetPos;
            _snapCoroutine = null;
        }
    }
}
