using TMPro;
using UnityEngine;

namespace InGame.Gameplay
{
    public class MaxLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        private Transform _worldTarget;
        private float _heightOffset;
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _rectTransform;

        public void Init(Transform worldTarget, float heightOffset, RectTransform canvasRect, Camera camera)
        {
            _worldTarget = worldTarget;
            _heightOffset = heightOffset;
            _canvasRect = canvasRect;
            _canvas = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
            _camera = camera;
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void LateUpdate()
        {
            if (_worldTarget == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;
            if (_canvasRect == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                _canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
            }
            if (_canvasRect == null) return;

            var worldPos = _worldTarget.position + Vector3.up * _heightOffset;
            var screenPos = _camera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                gameObject.SetActive(false);
                return;
            }

            // Screen Space Overlay는 camera null, 나머지는 canvas worldCamera
            var canvasCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, canvasCam, out var localPoint);

            // canvasRect 기준 local → UI world position으로 변환 후 position 세팅
            // (MaxLabel이 Canvas 직속 자식이 아니어도 정확하게 위치함)
            _rectTransform.position = _canvasRect.TransformPoint(localPoint);
        }
    }
}
