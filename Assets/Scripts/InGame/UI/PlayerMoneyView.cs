using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Model;
using TMPro;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.UI
{
    public class PlayerMoneyView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private float countDuration = 0.3f;

        private float _displayed;
        private CancellationTokenSource _animCts;

        public void Init(InGameModel inGameModel)
        {
            inGameModel.OnPlayerChanged += player =>
            {
                player.Info.MoneyCarryCount
                    .Subscribe(count => AnimateTo(count * MoneyStackValue))
                    .AddTo(this);
            };
        }

        private void AnimateTo(int target)
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = new CancellationTokenSource();
            AnimateAsync(target, _animCts.Token).Forget();
        }

        private async UniTaskVoid AnimateAsync(int target, CancellationToken ct)
        {
            var from = _displayed;
            var elapsed = 0f;

            while (elapsed < countDuration)
            {
                if (ct.IsCancellationRequested) return;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / countDuration);
                var smooth = t * t * (3f - 2f * t);
                _displayed = Mathf.Lerp(from, target, smooth);
                SetText(Mathf.RoundToInt(_displayed));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _displayed = target;
            SetText(target);
        }

        private void SetText(int value)
        {
            if (moneyText != null)
                moneyText.text = $"${value}";
        }

        private void OnDestroy()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
        }
    }
}
