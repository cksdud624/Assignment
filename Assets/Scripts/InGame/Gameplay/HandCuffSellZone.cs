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

        [SerializeField] private InteractZone moneyInteractZone;
        [SerializeField] private Transform moneyStackPlace;

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private SerialDisposable _playerCountDisposable;
        private SerialDisposable _spawnWatcherDisposable;
        private SerialDisposable _moneyTransferDisposable;
        private StackView _stackView;
        private StackView _moneyStackView;
        private GameObject _itemPrefab;
        private GameObject _moneyItemPrefab;
        private CharacterBase _currentPlayer;
        private CharacterBase _moneyCurrentPlayer;
        private bool _aiIsWaiting;
        private bool _isTransferringToAI;
        private int _aiReceivedCount;
        private Transform _lastDepartedTransform;

        private readonly List<(CharacterBase character, ControllerAI controller)> _aiQueue = new();

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            _transferDisposable = new SerialDisposable().AddTo(this);
            _playerCountDisposable = new SerialDisposable().AddTo(this);
            _spawnWatcherDisposable = new SerialDisposable().AddTo(this);
            _moneyTransferDisposable = new SerialDisposable().AddTo(this);

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

            _moneyStackView = moneyStackPlace.gameObject.AddComponent<StackView>();
            _moneyStackView.Init(new StackView.Config
            {
                Columns = MoneyStackColumns,
                Rows = MoneyStackRows,
                StackHeight = MoneyStackHeight,
                HeightOffset = MoneyStackHeightOffset,
                ColumnOffset = MoneyStackColumnOffset,
                RowOffset = MoneyStackRowOffset,
                WobbleDuration = MoneyStackWobbleDuration,
                WobbleFrequency = MoneyStackWobbleFrequency,
                WobbleAmplitude = MoneyStackWobbleAmplitude,
            });

            inGameModel.OnInitialized += OnInitialized;
            interactZone.OnPlayerInteracted.Subscribe(OnPlayerInteracted).AddTo(this);
            interactZone.OnPlayerExited.Subscribe(OnPlayerExited).AddTo(this);
            moneyInteractZone.OnPlayerInteracted.Subscribe(OnMoneyPlayerInteracted).AddTo(this);
            moneyInteractZone.OnPlayerExited.Subscribe(OnMoneyPlayerExited).AddTo(this);
        }

        private void OnInitialized()
        {
            _inGameModel.OnInitialized -= OnInitialized;
            _itemPrefab = _inGameModel.InGameAssetModel.GetView("handcuff_stack");
            _moneyItemPrefab = _inGameModel.InGameAssetModel.GetView("money_stack");
            SpawnAI();
        }

        #region HandCuff Interact

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
            FlyToSellPlaceAsync(player.transform.position).Forget();
        }

        private async UniTaskVoid FlyToSellPlaceAsync(Vector3 playerPosition)
        {
            var item = Instantiate(_itemPrefab);
            var from = playerPosition + Vector3.up * HandCuffPlayerItemOffset;
            item.transform.position = from;

            var worldDest = _stackView.ReserveNextWorldPosition();
            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, worldDest, ct);

            if (item == null) return;
            _stackView.AddItem(item);

            TryTransferToAI();
        }

        #endregion

        #region AI Transfer

        private void TryTransferToAI()
        {
            if (!_aiIsWaiting || _isTransferringToAI || _stackView.Count <= 0 || _aiQueue.Count == 0) return;
            if (_aiReceivedCount >= 3) return;

            var item = _stackView.TakeItem();
            if (item == null) return;

            _isTransferringToAI = true;
            var aiPosition = _aiQueue[0].character.transform.position;
            FlyToAIAsync(item, aiPosition).Forget();
        }

        private async UniTaskVoid FlyToAIAsync(GameObject item, Vector3 destination)
        {
            var from = item.transform.position;
            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, destination, ct);

            _isTransferringToAI = false;

            if (item != null) Destroy(item);

            _aiReceivedCount++;

            if (_aiReceivedCount >= 3)
            {
                _aiReceivedCount = 0;
                MoveAI();
                return;
            }

            TryTransferToAI();
        }

        #endregion

        #region Money Interact

        private void OnMoneyPlayerInteracted(CharacterBase player)
        {
            _moneyCurrentPlayer = player;
            StartMoneyTransferInterval(player);
        }

        private void OnMoneyPlayerExited(CharacterBase player)
        {
            _moneyCurrentPlayer = null;
            _moneyTransferDisposable.Disposable = Disposable.Empty;
        }

        private void StartMoneyTransferInterval(CharacterBase player)
        {
            _moneyTransferDisposable.Disposable = Observable
                .Interval(TimeSpan.FromSeconds(0.05))
                .TakeWhile(_ => _moneyStackView.Count > 0)
                .Subscribe(_ => TransferMoneyToPlayer(player));
        }

        private void TransferMoneyToPlayer(CharacterBase player)
        {
            var item = _moneyStackView.TakeItem();
            if (item == null) return;
            FlyMoneyToPlayerAsync(item, player).Forget();
        }

        private async UniTaskVoid FlyMoneyToPlayerAsync(GameObject item, CharacterBase player)
        {
            var from = item.transform.position;
            var to = player.transform.position + Vector3.up * HandCuffPlayerItemOffset;
            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, to, ct);

            if (item != null) Destroy(item);
            player.Info.MoneyCarryCount.Value++;
        }

        #endregion

        #region AI Queue

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
            TryTransferToAI();
        }

        private void MoveAI()
        {
            _aiIsWaiting = false;

            if (_aiQueue.Count == 0) return;

            var departingAi = _aiQueue[0];
            _aiQueue.RemoveAt(0);

            if (_aiQueue.Count > 0)
                _aiQueue[0].controller?.MoveTo(tableWaitPoint.position, OnFrontAiArrived);

            var prevDeparted = _lastDepartedTransform;
            _lastDepartedTransform = departingAi.character.transform;

            if (prevDeparted == null)
                departingAi.controller?.MoveTo(tableNextPoint.position, OnAIDeparted);
            else
                departingAi.controller?.FollowTransform(prevDeparted, HandCuffAIQueueStopDistance, OnAIDeparted);

            SpawnMoneyItems();
        }

        private void OnAIDeparted()
        {
            SpawnAI();
        }

        private void SpawnMoneyItems()
        {
            var from = handCuffSellPlace.position + Vector3.up * HandCuffPlayerItemOffset;
            for (var i = 0; i < 3; i++)
                FlyMoneyToStackAsync(from).Forget();
        }

        private async UniTaskVoid FlyMoneyToStackAsync(Vector3 from)
        {
            var item = Instantiate(_moneyItemPrefab);
            item.transform.position = from;

            var worldDest = _moneyStackView.ReserveNextWorldPosition();
            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, worldDest, ct);

            if (item == null) return;
            _moneyStackView.AddItem(item);

            if (_moneyCurrentPlayer != null)
                StartMoneyTransferInterval(_moneyCurrentPlayer);
        }

        #endregion
    }
}
