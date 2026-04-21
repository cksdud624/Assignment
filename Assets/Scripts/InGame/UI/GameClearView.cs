using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Model;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.UI
{
    public class GameClearView : MonoBehaviour
    {
        [SerializeField] private GameObject gameClear;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMin = 0.8f;
        [SerializeField] private float pulseMax = 1.2f;

        private InGameModel _inGameModel;
        private CancellationTokenSource _cts;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
        }

        private void OnEnable()
        {
            var soundPlayer = _inGameModel?.SoundPlayer;
            var clip = _inGameModel?.InGameAssetModel.GetAudioClip(nameof(SoundClip.GameClear));
            if (soundPlayer != null && clip != null)
                soundPlayer.PlayOnce(clip, transform.position);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            PulseAsync(_cts.Token).Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            if (gameClear != null)
                gameClear.transform.localScale = Vector3.one;
        }

        private async UniTaskVoid PulseAsync(CancellationToken ct)
        {
            if (gameClear == null) return;

            var mid = (pulseMax + pulseMin) * 0.5f;
            var amp = (pulseMax - pulseMin) * 0.5f;
            var elapsed = 0f;

            while (!ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                var scale = mid + amp * Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f);
                gameClear.transform.localScale = Vector3.one * scale;
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow())
                    break;
            }
        }
    }
}
