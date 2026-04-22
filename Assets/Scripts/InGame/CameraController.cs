using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Model;
using UnityEngine;

namespace InGame
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float revealMoveDuration = 0.8f;
        [SerializeField] private float revealHoldDuration = 2f;

        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            inGameModel.OnRequestAttachCamera += AttachCamera;
            inGameModel.OnRevealTarget += OnRevealTarget;
        }

        private void OnRevealTarget(Transform target)
        {
            _inGameModel.OnRevealTarget -= OnRevealTarget;
            RevealAsync(target, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void AttachCamera(Transform target)
        {
            transform.SetParent(target);
        }

        public async UniTask RevealAsync(Transform target, CancellationToken ct, bool restoreInput = true)
        {
            var originalParent = transform.parent;
            if (originalParent == null) return;

            var originalLocalPos = transform.localPosition;
            var originalLocalRot = transform.localRotation;

            // 플레이어 기준 카메라 오프셋을 그대로 target에 적용
            var worldOffset = transform.position - originalParent.position;
            var revealPos = target.position + worldOffset;

            transform.SetParent(null);
            _inGameModel.InvokeOnSetStandbyEnabled(false);
            _inGameModel.InvokeOnSetPlayerInputEnabled(false);

            await MoveToAsync(revealPos, transform.rotation, revealMoveDuration, ct);
            await UniTask.Delay(TimeSpan.FromSeconds(revealHoldDuration), cancellationToken: ct);
            await MoveToAsync(
                originalParent.TransformPoint(originalLocalPos),
                originalParent.rotation * originalLocalRot,
                revealMoveDuration, ct);

            transform.SetParent(originalParent);
            transform.localPosition = originalLocalPos;
            transform.localRotation = originalLocalRot;

            if (restoreInput)
            {
                _inGameModel.InvokeOnSetStandbyEnabled(true);
                _inGameModel.InvokeOnSetPlayerInputEnabled(true);
            }
        }

        private async UniTask MoveToAsync(Vector3 to, Quaternion toRot, float duration, CancellationToken ct)
        {
            var from = transform.position;
            var fromRot = transform.rotation;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(from, to, t);
                transform.rotation = Quaternion.Lerp(fromRot, toRot, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            transform.position = to;
            transform.rotation = toRot;
        }
    }
}
