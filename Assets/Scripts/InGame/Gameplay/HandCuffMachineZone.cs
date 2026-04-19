using System;
using Common;
using Cysharp.Threading.Tasks;
using InGame.Components;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using static Common.GameDefine;

namespace InGame.Gameplay
{
    public class HandCuffMachineZone : MonoBehaviour
    {
        [SerializeField] private InteractZone interactZone;
        [SerializeField] private Transform machine;
        [SerializeField] private HandCuffOutputZone handCuffOutputZone;

        public ReactiveProperty<int> MiningItemCount { get; } = new(0);

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private SerialDisposable _consumeDisposable;
        private GameObject _itemPrefab;
        private StackView _stackView;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            _transferDisposable = new SerialDisposable().AddTo(this);
            _consumeDisposable = new SerialDisposable().AddTo(this);
            _stackView = interactZone.gameObject.AddComponent<StackView>();
            _stackView.Init(new StackView.Config
            {
                Columns = 2,
                StackHeight = HandCuffStackHeight,
                HeightOffset = HandCuffHeightOffset,
                ColumnOffset = HandCuffColumnOffset,
                WobbleDuration = HandCuffWobbleDuration,
                WobbleFrequency = HandCuffWobbleFrequency,
                WobbleAmplitude = HandCuffWobbleAmplitude,
            });
            handCuffOutputZone.Init(inGameModel);

            inGameModel.OnInitialized += OnInitialized;
            interactZone.OnPlayerInteracted.Subscribe(OnPlayerInteracted).AddTo(this);
            interactZone.OnPlayerExited.Subscribe(OnPlayerExited).AddTo(this);

            MiningItemCount
                .Where(count => count >= 1)
                .Subscribe(OnMiningItemCountChanged)
                .AddTo(this);
        }

        private void OnInitialized()
        {
            _inGameModel.OnInitialized -= OnInitialized;
            _itemPrefab = _inGameModel.InGameAssetModel.GetView("ore_stack");
        }

        private void OnPlayerInteracted(CharacterBase player)
        {
            _transferDisposable.Disposable = Observable
                .Interval(TimeSpan.FromSeconds(1.0 / 20.0))
                .TakeWhile(_ => player.Info.MiningItemCount.Value > 0)
                .Subscribe(_ => TransferItem(player));
        }

        private void OnPlayerExited(CharacterBase player)
        {
            _transferDisposable.Disposable = Disposable.Empty;
        }

        #region Events

        private void OnMiningItemCountChanged(int count)
        {
            _consumeDisposable.Disposable = Observable
                .Interval(TimeSpan.FromSeconds(0.7))
                .TakeWhile(_ => MiningItemCount.Value > 0)
                .Subscribe(OnConsumeItem);
        }

        private void OnConsumeItem(long _)
        {
            MiningItemCount.Value--;
            _stackView.RemoveItem();

            var ct = this.GetCancellationTokenOnDestroy();
            TweenUtility.PopScaleAsync(machine, ct).Forget();
            PopOutputAsync(ct).Forget();
        }

        #endregion

        private void TransferItem(CharacterBase player)
        {
            player.Info.MiningItemCount.Value--;
            MiningItemCount.Value++;

            var from = player.transform.position + Vector3.up * HandCuffPlayerItemOffset;
            var worldDest = _stackView.GetNextWorldPosition();

            FlyItemAsync(from, worldDest).Forget();
        }

        private async UniTaskVoid PopOutputAsync(System.Threading.CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: ct);
            handCuffOutputZone.AddHandCuff();
        }

        private async UniTaskVoid FlyItemAsync(Vector3 from, Vector3 worldDest)
        {
            var item = Instantiate(_itemPrefab);
            item.transform.position = from;

            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, worldDest, ct);

            if (item == null) return;
            _stackView.AddItem(item);
        }
    }
}
