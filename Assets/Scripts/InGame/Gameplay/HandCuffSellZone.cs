using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using InGame.Components;
using InGame.Model;
using InGame.Object;
using InGame.UI;
using UniRx;
using UnityEngine;
using static Common.GameDefine;
using Random = UnityEngine.Random;

namespace InGame.Gameplay
{
    public class HandCuffSellZone : MonoBehaviour, IAIDeliverTarget, IAIActivatable
    {
        [SerializeField] private InteractZone interactZone;
        [SerializeField] private Transform handCuffSellPlace;
        [SerializeField] private Transform aiSpawnPoint;
        [SerializeField] private Transform tableWaitPoint;
        [SerializeField] private Transform tableNextPoint;

        [SerializeField] private Transform prisonPoint;
        [SerializeField] private Collider prisonCollider;
        [SerializeField] private InteractZone moneyInteractZone;
        [SerializeField] private Transform moneyStackPlace;
        [SerializeField] private HandCuffRequirementUI requirementUI;

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private SerialDisposable _playerCountDisposable;
        private SerialDisposable _spawnWatcherDisposable;
        private StackView _stackView;
        private StackView _moneyStackView;
        private GameObject _itemPrefab;
        private GameObject _moneyItemPrefab;
        private CharacterBase _currentPlayer;
        private CharacterBase _moneyCurrentPlayer;
        private static readonly int[] RequirementCycle = { 3, 3, 4 };

        private bool _aiIsWaiting;
        private bool _isTransferringToAI;
        private int _pitchCount;
        private int _aiReceivedCount;
        private int _currentAIRequired;
        private int _departureCount;
        private Transform _lastDepartedTransform;
        private bool _guardIsReady;
        private MoneySpendZone _unlockZone;
        private int _prisonArrivalCount;
        private int _prisonGoalCount;

        private readonly List<(CharacterBase character, ControllerAI controller)> _aiQueue = new();

        public void Init(InGameModel inGameModel, MoneySpendZone unlockZone = null)
        {
            _unlockZone = unlockZone;
            _inGameModel = inGameModel;
            _prisonGoalCount = inGameModel.PrisonGoalCount;
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
            TryTransferToAI();
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
            _pitchCount = 0;
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
            var max = interactZone.MaxCount;
            if (max > 0 && _stackView.Count >= max) return;

            player.Info.HandCuffCount.Value--;
            FlyToSellPlaceAsync(player.transform.position).Forget();

            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.ItemCharge));
            _inGameModel.SoundPlayer.PlayOnce(clip, interactZone.transform.position, CalcPitch());
            _pitchCount = Mathf.Min(_pitchCount + 1, 10);
        }

        private float CalcPitch() => Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(_pitchCount / 9f));

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
            UpdateMaxDisplay();

            TryTransferToAI();
        }

        #endregion

        #region AI Transfer

        public async UniTask ActivateForAI(CharacterBase ai, CancellationToken ct)
        {
            _guardIsReady = true;
            interactZone.ApplyInteractMaterial();
            TryTransferToAI();
            await UniTask.WaitUntil(() => _stackView.Count == 0, cancellationToken: ct);
            _guardIsReady = false;
            interactZone.ApplyStandbyMaterial();
        }

        public async UniTask ReceiveFromAI(CharacterBase ai, CancellationToken ct)
        {
            while (ai.Info.HandCuffCount.Value > 0 && !ct.IsCancellationRequested)
            {
                var max = interactZone.MaxCount;
                if (max > 0 && _stackView.Count >= max) break;

                ai.Info.HandCuffCount.Value--;

                var item = Instantiate(_itemPrefab);
                var from = ai.transform.position + Vector3.up * HandCuffPlayerItemOffset;
                item.transform.position = from;
                var worldDest = _stackView.ReserveNextWorldPosition();

                try
                {
                    await TweenUtility.MoveArcAsync(item.transform, from, worldDest, ct);
                }
                catch (OperationCanceledException)
                {
                    if (item != null) Destroy(item);
                    return;
                }

                if (item == null) return;
                _stackView.AddItem(item);
                UpdateMaxDisplay();
                TryTransferToAI();

                await UniTask.Delay(TimeSpan.FromSeconds(0.05f), cancellationToken: ct);
            }
        }

        private void TryTransferToAI()
        {
            if (_currentPlayer == null && !_guardIsReady) return;
            if (!_aiIsWaiting || _isTransferringToAI || _stackView.Count <= 0 || _aiQueue.Count == 0) return;
            if (_aiReceivedCount >= _currentAIRequired) return;

            var item = _stackView.TakeItem();
            if (item == null) return;

            UpdateMaxDisplay();

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

            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.ItemCharge));
            _inGameModel.SoundPlayer.PlayOnce(clip, destination);

            _aiReceivedCount++;
            requirementUI?.UpdateFill(_aiReceivedCount, _currentAIRequired);

            if (_aiReceivedCount >= _currentAIRequired)
            {
                _aiReceivedCount = 0;
                HideRequirementLabel();
                var purchasedClip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.PurchasedMoney));
                _inGameModel.SoundPlayer.PlayOnce(purchasedClip, destination);
                MoveAI();
                return;
            }

            TryTransferToAI();
        }

        #endregion

        private void UpdateMaxDisplay()
        {
            var max = interactZone.MaxCount;
            interactZone.SetMaxReached(max > 0 && _stackView.Count >= max);
        }

        #region Money Interact

        private void OnMoneyPlayerInteracted(CharacterBase player)
        {
            _moneyCurrentPlayer = player;
            TransferAllMoneyToPlayer(player);
        }

        private void OnMoneyPlayerExited(CharacterBase player)
        {
            _moneyCurrentPlayer = null;
        }

        private void TransferAllMoneyToPlayer(CharacterBase player)
        {
            if (_moneyStackView.Count <= 0) return;

            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.GetMoney));
            _inGameModel.SoundPlayer.PlayOnce(clip, moneyStackPlace.position);

            while (_moneyStackView.Count > 0)
            {
                var item = _moneyStackView.TakeItem();
                if (item == null) break;
                FlyMoneyToPlayerAsync(item, player).Forget();
            }
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
            _currentAIRequired = RequirementCycle[_departureCount % RequirementCycle.Length];
            _aiIsWaiting = true;

            if (_aiQueue.Count > 0)
                ShowRequirementLabel(_aiQueue[0].character);

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

            var isFirstDeparture = _lastDepartedTransform == null;
            _lastDepartedTransform = departingAi.character.transform;

            var captured = departingAi;
            departingAi.controller?.MoveTo(tableNextPoint.position, () =>
            {
                if (isFirstDeparture) SpawnAI();
                captured.controller?.MoveTo(GetRandomPrisonPosition(), OnAIReachedPrison);
            });

            _departureCount++;
            SpawnMoneyItems();
        }

        private void ShowRequirementLabel(CharacterBase ai)
        {
            requirementUI?.Show(tableWaitPoint, _currentAIRequired);
        }

        private void HideRequirementLabel()
        {
            requirementUI?.Hide();
        }

        public void Stop()
        {
            _transferDisposable.Disposable = Disposable.Empty;
            _playerCountDisposable.Disposable = Disposable.Empty;
            _spawnWatcherDisposable.Disposable = Disposable.Empty;
            HideRequirementLabel();
        }

        private void OnAIReachedPrison()
        {
            _prisonArrivalCount++;
            if (_prisonArrivalCount < _prisonGoalCount) return;

            if (_unlockZone != null)
            {
                _unlockZone.ForceActivate();
                _inGameModel.InvokeOnRevealTarget(_unlockZone.transform);
            }
        }

        private Vector3 GetRandomPrisonPosition()
        {
            if (prisonCollider != null)
            {
                var bounds = prisonCollider.bounds;
                return new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );
            }
            return prisonPoint != null ? prisonPoint.position : Vector3.zero;
        }

        private void SpawnMoneyItems()
        {
            var itemCount = _currentAIRequired * HandCuffSellMoneyPerHandCuff / MoneyStackValue;
            var from = handCuffSellPlace.position + Vector3.up * HandCuffPlayerItemOffset;
            for (var i = 0; i < itemCount; i++)
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
                TransferAllMoneyToPlayer(_moneyCurrentPlayer);
        }

        #endregion
    }
}
