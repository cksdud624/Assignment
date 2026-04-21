using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace InGame.Gameplay
{
    public class MoneySpendZoneUI : MonoBehaviour
    {
        [SerializeField] private TextMeshPro labelText;
        [SerializeField] private TextMeshPro costText;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private float countDuration = 0.4f;

        private float _originalFillHeight;
        private float _displayedCost;
        private float _displayedFill;
        private CancellationTokenSource _animCts;

        private void Awake()
        {
            if (fillRect != null)
            {
                _originalFillHeight = fillRect.sizeDelta.y;
                _displayedFill = 0f;
                fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, 0f);
            }
        }

        public void SetLabel(string label)
        {
            if (labelText != null)
                labelText.text = label;
        }

        public void SetRemainingCost(int remaining, int total)
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = new CancellationTokenSource();

            var targetFill = total > 0 ? Mathf.Clamp01(1f - (float)remaining / total) : 0f;
            AnimateAsync(remaining, targetFill, _animCts.Token).Forget();
        }

        private async UniTaskVoid AnimateAsync(int targetCost, float targetFill, CancellationToken ct)
        {
            var fromCost = _displayedCost;
            var fromFill = _displayedFill;
            var elapsed = 0f;

            while (elapsed < countDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / countDuration);
                var smooth = t * t * (3f - 2f * t);

                _displayedCost = Mathf.Lerp(fromCost, targetCost, smooth);
                _displayedFill = Mathf.Lerp(fromFill, targetFill, smooth);

                if (costText != null)
                    costText.text = $"${Mathf.CeilToInt(_displayedCost)}";

                if (fillRect != null)
                    fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, _originalFillHeight * _displayedFill);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _displayedCost = targetCost;
            _displayedFill = targetFill;

            if (costText != null)
                costText.text = $"${targetCost}";

            if (fillRect != null)
                fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, _originalFillHeight * targetFill);
        }

        private void OnDestroy()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
        }
    }
}
