using System;
using Common;
using Cysharp.Threading.Tasks;
using InGame.Components;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Gameplay
{
    public class HandCuffOutputZone : MonoBehaviour
    {
        [SerializeField] private InteractZone interactZone;
        [SerializeField] private Transform output;
        [SerializeField] private Transform handCuffPlace;
        [SerializeField] private Transform handCuffs;

        public ReactiveProperty<int> HandCuffCount { get; } = new(0);

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private StackView _stackView;
        private GameObject _itemPrefab;
        private CharacterBase _currentPlayer;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            handCuffs.position = handCuffPlace.position;
            _stackView = handCuffs.gameObject.AddComponent<StackView>();
            _stackView.Init(new StackView.Config
            {
                Columns = 1,
                StackHeight = HandCuffOutputStackHeight,
                HeightOffset = HandCuffOutputHeightOffset,
                ColumnOffset = HandCuffOutputColumnOffset,
                WobbleDuration = HandCuffOutputWobbleDuration,
                WobbleFrequency = HandCuffOutputWobbleFrequency,
                WobbleAmplitude = HandCuffOutputWobbleAmplitude,
            });

            _transferDisposable = new SerialDisposable().AddTo(this);
            inGameModel.OnInitialized += OnInitialized;
            interactZone.OnPlayerInteracted.Subscribe(OnPlayerInteracted).AddTo(this);
            interactZone.OnPlayerExited.Subscribe(OnPlayerExited).AddTo(this);

            HandCuffCount
                .Select(c => c > 0)
                .DistinctUntilChanged()
                .Where(hasItems => hasItems && _currentPlayer != null)
                .Subscribe(_ => StartTransferInterval(_currentPlayer))
                .AddTo(this);
        }

        private void OnInitialized()
        {
            _inGameModel.OnInitialized -= OnInitialized;
            _itemPrefab = _inGameModel.InGameAssetModel.GetView("handcuff_stack");
        }

        public void AddHandCuff()
        {
            HandCuffCount.Value++;

            var ct = this.GetCancellationTokenOnDestroy();
            TweenUtility.PopScaleAsync(output, ct).Forget();

            var item = Instantiate(_itemPrefab);
            item.transform.position = _stackView.GetNextWorldPosition();
            _stackView.AddItem(item);
        }

        #region Events

        private void OnPlayerInteracted(CharacterBase player)
        {
            _currentPlayer = player;
            StartTransferInterval(player);
        }

        private void OnPlayerExited(CharacterBase player)
        {
            _currentPlayer = null;
            _transferDisposable.Disposable = Disposable.Empty;
        }

        #endregion

        private void StartTransferInterval(CharacterBase player)
        {
            _transferDisposable.Disposable = Observable
                .Interval(TimeSpan.FromSeconds(0.05))
                .TakeWhile(_ => HandCuffCount.Value > 0)
                .Subscribe(_ => TransferToPlayer(player));
        }

        private void TransferToPlayer(CharacterBase player)
        {
            HandCuffCount.Value--;
            _stackView.RemoveItem();
            FlyToPlayerAsync(player).Forget();
        }

        private async UniTaskVoid FlyToPlayerAsync(CharacterBase player)
        {
            var item = Instantiate(_itemPrefab);
            var from = output.position;
            item.transform.position = from;

            var ct = this.GetCancellationTokenOnDestroy();
            var to = player.transform.position + Vector3.up * HandCuffPlayerItemOffset;
            await TweenUtility.MoveArcAsync(item.transform, from, to, ct);

            if (item != null) Destroy(item);
            player.Info.HandCuffCount.Value++;
        }
    }
}
