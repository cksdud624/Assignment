using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Components;
using InGame.Object;
using UnityEngine;

namespace InGame.Gameplay
{
    public class GenericAIBrain : MonoBehaviour
    {
        private CharacterBase _ai;
        private ControllerAI _controller;
        private IReadOnlyList<AIBehaviourStep> _steps;
        private CancellationTokenSource _cts;

        public void Init(CharacterBase ai, IReadOnlyList<AIBehaviourStep> steps)
        {
            _ai = ai;
            _controller = (ControllerAI)ai.Controller;
            _steps = steps;
            _cts = new CancellationTokenSource();
            RunLoop(_cts.Token).Forget();
        }

        private async UniTaskVoid RunLoop(CancellationToken ct)
        {
            if (_steps == null || _steps.Count == 0) return;

            while (!ct.IsCancellationRequested)
            {
                foreach (var step in _steps)
                {
                    if (ct.IsCancellationRequested) return;
                    await ExecuteStep(step, ct);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private async UniTask ExecuteStep(AIBehaviourStep step, CancellationToken ct)
        {
            var source = step.actionTarget != null ? step.actionTarget : step.location;

            switch (step.action)
            {
                case AIStepAction.WaitForItems:
                {
                    var moveDest = step.location ?? source;
                    if (moveDest != null)
                        _controller.MoveTo(moveDest.position, null);
                    var pickup = GetInterface<IAIPickupSource>(source);
                    if (pickup != null)
                    {
                        await UniTask.WaitUntil(() => pickup.IsAIInZone(_ai), cancellationToken: ct);
                        _controller.StopMove();
                        await UniTask.WaitUntil(() => pickup.ItemCount >= step.threshold, cancellationToken: ct);
                    }
                    else
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    break;
                }
                case AIStepAction.PickUp:
                {
                    var pickup = GetInterface<IAIPickupSource>(source);
                    if (pickup != null)
                        await pickup.PickupForAI(_ai, step.threshold, ct);
                    break;
                }
                case AIStepAction.Deliver:
                {
                    await MoveToAsync(step.location.position, ct);
                    var deliver = GetInterface<IAIDeliverTarget>(source);
                    if (deliver != null)
                        await deliver.ReceiveFromAI(_ai, ct);
                    break;
                }
                case AIStepAction.Activate:
                {
                    await MoveToAsync(step.location.position, ct);
                    var activatable = GetInterface<IAIActivatable>(source);
                    if (activatable != null)
                        await activatable.ActivateForAI(_ai, ct);
                    break;
                }
            }
        }

        private static T GetInterface<T>(Transform target) where T : class
        {
            if (target == null) return null;
            return target.GetComponentInParent<T>() ?? target.GetComponentInChildren<T>();
        }

        private async UniTask MoveToAsync(Vector3 target, CancellationToken ct)
        {
            var arrived = false;
            _controller.MoveTo(target, () => arrived = true);
            await UniTask.WaitUntil(() => arrived, cancellationToken: ct);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
