using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Gameplay
{
    public class MaxLabelController : MonoBehaviour
    {
        [SerializeField] private MaxLabel maxLabelPrefab;
        [SerializeField] private float floatingRiseAmount = 80f;
        [SerializeField] private float floatingRiseDuration = 1f;

        public float FloatingRiseDuration => floatingRiseDuration;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Camera _camera;
        private bool _isFloatingActive;
        public bool IsFloatingActive => _isFloatingActive;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas.GetComponent<RectTransform>();
            _camera = Camera.main;
        }

        // 고정 MAX 라벨 — InteractZone용
        public MaxLabel CreateLabel(Transform worldTarget, float heightOffset)
        {
            if (maxLabelPrefab == null)
            {
                Debug.LogWarning("[MaxLabelController] maxLabelPrefab is not assigned.");
                return null;
            }

            var label = Instantiate(maxLabelPrefab, transform);
            label.Init(worldTarget, heightOffset, _canvasRect, _camera);
            return label;
        }

        // 플로팅 MAX 라벨 — 1초 상승 후 소멸, 진행 중이면 무시
        public void ShowFloatingMax(Transform worldTarget, float heightOffset)
        {
            if (_isFloatingActive) return;
            ShowFloatingMaxAsync(worldTarget, heightOffset, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ShowFloatingMaxAsync(Transform worldTarget, float heightOffset, System.Threading.CancellationToken ct)
        {
            _isFloatingActive = true;

            if (_camera == null) _camera = Camera.main;

            var label = Instantiate(maxLabelPrefab, transform);
            var rect = label.GetComponent<RectTransform>();
            label.gameObject.SetActive(true);

            var canvasCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            var elapsed = 0f;
            while (elapsed < floatingRiseDuration)
            {
                if (ct.IsCancellationRequested) break;
                if (_camera == null) _camera = Camera.main;
                if (_camera != null && worldTarget != null)
                {
                    var worldPos = worldTarget.position + Vector3.up * heightOffset;
                    var screenPos = _camera.WorldToScreenPoint(worldPos);
                    if (screenPos.z > 0)
                    {
                        var t = Mathf.Clamp01(elapsed / floatingRiseDuration);
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _canvasRect, screenPos, canvasCam, out var localPos);
                        localPos.y += floatingRiseAmount * t;
                        rect.position = _canvasRect.TransformPoint(localPos);
                    }
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (label != null) Destroy(label.gameObject);
            _isFloatingActive = false;
        }
    }
}
