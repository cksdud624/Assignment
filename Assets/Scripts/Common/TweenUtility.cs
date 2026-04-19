using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Common
{
    public static class TweenUtility
    {
        public static async UniTask MoveArcAsync(
            Transform target,
            Vector3 from,
            Vector3 to,
            CancellationToken cancellationToken,
            float duration = 0.35f,
            float arcHeight = 1.5f)
        {
            var elapsed = 0f;
            var mid = (from + to) * 0.5f + Vector3.up * arcHeight;
            while (elapsed < duration)
            {
                if (target == null) return;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var a = Vector3.Lerp(from, mid, t);
                var b = Vector3.Lerp(mid, to, t);
                target.position = Vector3.Lerp(a, b, t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            if (target != null)
                target.position = to;
        }

        public static async UniTask ShrinkScaleAsync(
            Transform target,
            CancellationToken cancellationToken,
            float duration = 0.2f)
        {
            var elapsed = 0f;
            var startScale = target != null ? target.localScale : Vector3.one;
            while (elapsed < duration)
            {
                if (target == null) return;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            if (target != null)
                target.localScale = Vector3.zero;
        }

        public static async UniTask PopScaleAsync(
            Transform target,
            CancellationToken cancellationToken,
            float popScale = 1.2f,
            float duration = 0.2f)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) return;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            if (target != null)
                target.localScale = Vector3.one;
        }
    }
}
