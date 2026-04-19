using System;
using System.Collections.Generic;
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
    public class HandCuffSellZone : MonoBehaviour
    {
        [SerializeField] private InteractZone interactZone;
        [SerializeField] private Transform handCuffSellPlace;
        [SerializeField] private Transform aiSpawnPoint;
        [SerializeField] private Transform tableWaitPoint;
        [SerializeField] private Transform tableNextPoint;

        public ReactiveProperty<int> HandCuffCount { get; } = new(0);

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private SerialDisposable _playerCountDisposable;
        private SerialDisposable _spawnWatcherDisposable;
        private StackView _stackView;
        private GameObject _itemPrefab;
        private CharacterBase _currentPlayer;
        private bool _aiIsWaiting;
        private Transform _lastDepartedTransform;
        private CharacterBase _depositor;

        private readonly List<(CharacterBase character, ControllerAI controller)> _aiQueue = new();

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            _transferDisposable = new SerialDisposable().AddTo(this);
            _playerCountDisposable = new SerialDisposable().AddTo(this);
            _spawnWatcherDisposable = new SerialDisposable().AddTo(this);

            _stackView = handCuffSellPlace.gameObject.AddComponent<StackView>();
            _stackView.Init(new StackView.Config
            {
                Columns = 1,
                StackHeight = HandCuffSellStackHeight,
                HeightOffset = HandCuffSellHeightOffset,
                ColumnOffset = HandCuffSellColumnOffset,
                WobbleDuration = HandCuffSellWobbleDuration,
                WobbleFrequency = HandCuffSellWobbleFrequency,
                WobbleAmplitude = HandCuffSellWobbleAmplitude,
            });

            HandCuffCount
                .Where(count => count >= 3 && _aiIsWaiting)
                .Subscribe(_ => ConsumeAndMoveAI())
                .AddTo(this);

            inGameModel.OnInitialized += OnInitialized;
            interactZone.OnPlayerInteracted.Subscribe(OnPlayerInteracted).AddTo(this);
            interactZone.OnPlayerExited.Subscribe(OnPlayerExited).AddTo(this);
        }

        private void OnInitialized()
        {
            _inGameModel.OnInitialized -= OnInitialized;
            _itemPrefab = _inGameModel.InGameAssetModel.GetView("handcuff_stack");
            SpawnAI();
        }

        #region Events

        private void OnPlayerInteracted(CharacterBase player)
        {
            _currentPlayer = player;
            StartTransferInterval(player);
            _playerCountDisposable.Disposable = player.Info.HandCuffCount
                .Select(c => c > 0)
                .DistinctUntilChanged()
                .Where(hasItems => hasItems)
                .Subscribe(_ => StartTransferInterval(_currentPlayer));
        }

        private void OnPlayerExited(CharacterBase player)
        {
            _currentPlayer = null;
            _transferDisposable.Disposable = Disposable.Empty;
            _playerCountDisposable.Disposable = Disposable.Empty;
        }

        #endregion

        private void StartTransferInterval(CharacterBase player)
        {
            _transferDisposable.Disposable = Observable
                .Interval(TimeSpan.FromSeconds(0.05))
                .TakeWhile(_ => player.Info.HandCuffCount.Value > 0)
                .Subscribe(_ => TransferFromPlayer(player));
        }

        private void TransferFromPlayer(CharacterBase player)
        {
            player.Info.HandCuffCount.Value--;
            _depositor = player;
            FlyToSellPlaceAsync(player.transform.position).Forget();
        }

        private async UniTaskVoid FlyToSellPlaceAsync(Vector3 playerPosition)
        {
            var item = Instantiate(_itemPrefab);
            var from = playerPosition + Vector3.up * HandCuffPlayerItemOffset;
            item.transform.position = from;

            var worldDest = _stackView.GetNextWorldPosition();
            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, worldDest, ct);

            if (item == null) return;
            _stackView.AddItem(item);
            HandCuffCount.Value++;
        }

        private void SpawnAI()
        {
            _inGameModel.InvokeOnSpawnAI(aiSpawnPoint.position, aiSpawnPoint.rotation, ai =>
            {
                var controller = ai.Controller as ControllerAI;
                _aiQueue.Add((ai, controller));

                if (_aiQueue.Count == 1)
                    controller?.MoveTo(tableWaitPoint.position, OnFrontAiArrived);
                else
                {
                    var ahead = _aiQueue[_aiQueue.Count - 2];
                    controller?.FollowTransform(ahead.character.transform, HandCuffAIQueueStopDistance);
                }

                StartSpawnWatcher(ai.transform);
            });
        }

        private void StartSpawnWatcher(Transform aiTransform)
        {
            var spawnPos = aiSpawnPoint.position;
            var stopDistSq = HandCuffAIQueueStopDistance * HandCuffAIQueueStopDistance;

            _spawnWatcherDisposable.Disposable = Observable.EveryUpdate()
                .Where(_ => aiTransform != null &&
                            (aiTransform.position - spawnPos).sqrMagnitude > stopDistSq)
                .First()
                .Subscribe(_ => SpawnAI());
        }

        private void OnFrontAiArrived()
        {
            _aiIsWaiting = true;
            if (HandCuffCount.Value >= 3)
                ConsumeAndMoveAI();
        }

        private void ConsumeAndMoveAI()
        {
            _aiIsWaiting = false;

            for (var i = 0; i < 3; i++)
            {
                HandCuffCount.Value--;
                _stackView.RemoveItem();
                if (_depositor != null)
                    _depositor.Info.Money.Value += HandCuffSellMoneyPerHandCuff;
            }

            if (_aiQueue.Count == 0) return;

            var departingAi = _aiQueue[0];
            _aiQueue.RemoveAt(0);

            if (_aiQueue.Count > 0)
                _aiQueue[0].controller?.MoveTo(tableWaitPoint.position, OnFrontAiArrived);

            var prevDeparted = _lastDepartedTransform;
            _lastDepartedTransform = departingAi.character.transform;

            if (prevDeparted == null)
                departingAi.controller?.MoveTo(tableNextPoint.position, SpawnAI);
            else
                departingAi.controller?.FollowTransform(prevDeparted, HandCuffAIQueueStopDistance, SpawnAI);
        }
    }
}
