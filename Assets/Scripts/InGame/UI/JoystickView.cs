using System;
using UnityEngine;

namespace InGame.UI
{
    public class JoystickView : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        public event Action<Vector2> OnPerformed;
        public event Action OnCanceled;

        private RectTransform _rectTransform;
        private Vector2 _handleOrigin;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _handleOrigin = handle.anchoredPosition;
        }

        public void Activate(Vector2 screenPos, Camera pressCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform.parent as RectTransform,
                screenPos, pressCamera, out var localPoint);

            _rectTransform.anchoredPosition = localPoint;
            handle.anchoredPosition = _handleOrigin;
        }

        public void UpdateHandle(Vector2 screenPos, Camera pressCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, screenPos, pressCamera, out var localPoint);

            var radius  = background.rect.width * 0.5f;
            var clamped = Vector2.ClampMagnitude(localPoint, radius);
            handle.anchoredPosition = clamped;

            var direction = clamped / radius;
            if (direction.magnitude > 0.1f)
                OnPerformed?.Invoke(direction);
            else
                OnCanceled?.Invoke();
        }

        public void Deactivate()
        {
            handle.anchoredPosition = _handleOrigin;
            OnCanceled?.Invoke();
        }

        private void OnDestroy()
        {
            OnPerformed = null;
            OnCanceled  = null;
        }
    }
}
