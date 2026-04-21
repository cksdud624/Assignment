using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Gameplay;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;

namespace InGame.Tutorial
{
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] private TutorialArrow arrow;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private List<TutorialStep> steps;

        private InGameModel _inGameModel;
        private CompositeDisposable _stepDisposable;
        private CharacterBase _player;
        private CancellationTokenSource _cts;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            _stepDisposable = new CompositeDisposable();
            _cts = new CancellationTokenSource();
            inGameModel.OnInitialized += OnInitialized;

            // 초기화 전에도 첫 화살표 미리 표시
            if (steps is { Count: > 0 } && steps[0].arrowTarget != null)
                arrow.Show(steps[0].arrowTarget);
        }

        private void OnInitialized()
        {
            _inGameModel.OnInitialized -= OnInitialized;
            _player = _inGameModel.InGameObjectModel.Player;
            if (_player == null) return;

            arrow.SetPlayer(_player.transform);
            StartStep(0);
        }

        private void StartStep(int index)
        {
            if (index >= steps.Count)
            {
                CompleteTutorial();
                return;
            }

            _stepDisposable.Clear();
            var step = steps[index];

            // 화살표 표시 (Show는 내부적으로 autoPointer를 true로 리셋하므로 반드시 먼저 호출)
            if (step.arrowTarget != null)
                arrow.Show(step.arrowTarget);
            arrow.SetAutoPointerEnabled(step.autoPointerEnabled);

            // 아이템 수 도달 시 포인터 강제 표시
            if (step.showPointerAtItemCount > 0 && _player != null)
            {
                _player.Info.MiningItemCount
                    .Where(c => c >= step.showPointerAtItemCount)
                    .First()
                    .Subscribe(_ => arrow.ShowPointer())
                    .AddTo(_stepDisposable);
            }

            SubscribeCondition(step, () => OnStepConditionMet(index + 1));
        }

        private void SubscribeCondition(TutorialStep step, System.Action onMet)
        {
            switch (step.conditionType)
            {
                case TutorialConditionType.MiningItemCountReached:
                    if (_player != null)
                        _player.Info.MiningItemCount
                            .Where(c => c >= step.conditionThreshold)
                            .First()
                            .Subscribe(_ => onMet())
                            .AddTo(_stepDisposable);
                    break;

                case TutorialConditionType.PlayerEnteredZone:
                {
                    var zone = FindComponent<InteractZone>(step.conditionTarget);
                    zone?.OnPlayerInteracted
                        .First()
                        .Subscribe(_ => onMet())
                        .AddTo(_stepDisposable);
                    break;
                }

                case TutorialConditionType.ZoneActivated:
                {
                    var zone = FindComponent<MoneySpendZone>(step.conditionTarget);
                    zone?.OnActivated
                        .First()
                        .Subscribe(_ => onMet())
                        .AddTo(_stepDisposable);
                    break;
                }
            }
        }

        private void OnStepConditionMet(int nextIndex)
        {
            _stepDisposable.Clear();

            if (nextIndex >= steps.Count)
            {
                CompleteTutorial();
                return;
            }

            var next = steps[nextIndex];
            if (next.hideArrowOnEnter || next.cameraRevealTarget != null)
                RunPreActionsAsync(nextIndex).Forget();
            else
                StartStep(nextIndex);
        }

        private async UniTaskVoid RunPreActionsAsync(int nextIndex)
        {
            var step = steps[nextIndex];
            var ct = _cts.Token;

            if (step.hideArrowOnEnter)
                arrow.Hide();

            if (step.cameraRevealTarget != null)
                await cameraController.RevealAsync(step.cameraRevealTarget, ct);

            if (ct.IsCancellationRequested) return;
            StartStep(nextIndex);
        }

        private void CompleteTutorial()
        {
            _stepDisposable.Clear();
            arrow.Hide();
        }

        private static T FindComponent<T>(Transform target) where T : Component
        {
            if (target == null) return null;
            return target.GetComponent<T>()
                ?? target.GetComponentInParent<T>()
                ?? target.GetComponentInChildren<T>();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _stepDisposable?.Dispose();
            if (_inGameModel != null)
                _inGameModel.OnInitialized -= OnInitialized;
        }
    }
}
