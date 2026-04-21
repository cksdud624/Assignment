using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace InGame.UI
{
    public class HandCuffRequirementUI : MonoBehaviour
    {
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private float fillDuration = 0.3f;
        [SerializeField] private Vector2 canvasOffset = new Vector2(60f, 60f);

        private float _originalFillHeight;
        private CancellationTokenSource _cts;
        private Transform _worldTarget;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
                _canvasRect = _canvas.GetComponent<RectTransform>();

            if (fillRect != null)
            {
                _originalFillHeight = fillRect.sizeDelta.y;
                var size = fillRect.sizeDelta;
                fillRect.sizeDelta = new Vector2(size.x, 0f);
            }
            gameObject.SetActive(false);
        }

        public void Show(Transform worldTarget, int required)
        {
            _worldTarget = worldTarget;
            SetFillImmediate(0f);
            if (countText != null)
                countText.text = required.ToString();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _worldTarget = null;
            gameObject.SetActive(false);
        }

        public void UpdateFill(int received, int required)
        {
            var remaining = required - received;
            if (countText != null)
                countText.text = remaining.ToString();

            var targetFill = required > 0 ? (float)received / required : 0f;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            AnimateFillAsync(targetFill, _cts.Token).Forget();
        }

        private void LateUpdate()
        {
            if (_worldTarget == null || _canvas == null || _canvasRect == null || _rectTransform == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            var screenPos = cam.WorldToScreenPoint(_worldTarget.position);
            var canvasCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, canvasCam, out var localPos))
                _rectTransform.position = _canvasRect.TransformPoint(localPos + canvasOffset);
        }

        private void SetFillImmediate(float ratio)
        {
            if (fillRect == null) return;
            var size = fillRect.sizeDelta;
            fillRect.sizeDelta = new Vector2(size.x, _originalFillHeight * ratio);
        }

        private async UniTaskVoid AnimateFillAsync(float targetRatio, CancellationToken ct)
        {
            if (fillRect == null) return;

            var startHeight = fillRect.sizeDelta.y;
            var targetHeight = _originalFillHeight * targetRatio;
            var elapsed = 0f;

            while (elapsed < fillDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fillDuration);
                t = t * t * (3f - 2f * t);
                var size = fillRect.sizeDelta;
                fillRect.sizeDelta = new Vector2(size.x, Mathf.Lerp(startHeight, targetHeight, t));

                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow())
                    return;
            }

            var finalSize = fillRect.sizeDelta;
            fillRect.sizeDelta = new Vector2(finalSize.x, targetHeight);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
