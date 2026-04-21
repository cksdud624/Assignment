using System;
using System.Collections.Generic;
using System.Threading;
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
    public class HandCuffOutputZone : MonoBehaviour, IAIPickupSource
    {
        [SerializeField] private InteractZone interactZone;
        [SerializeField] private Transform output;
        [SerializeField] private Transform handCuffPlace;
        [SerializeField] private Transform handCuffs;

        public ReactiveProperty<int> HandCuffCount { get; } = new(0);
        public bool HasItems => HandCuffCount.Value > 0;
        public int ItemCount => HandCuffCount.Value;
        public bool IsAIInZone(CharacterBase ai) => _aiInZone.Contains(ai);
        public InteractZone InteractZone => interactZone;

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private StackView _stackView;
        private GameObject _itemPrefab;
        private CharacterBase _currentPlayer;
        private readonly HashSet<CharacterBase> _aiInZone = new();
        private int _pitchCount;

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
            interactZone.OnAIInteracted.Subscribe(ai => _aiInZone.Add(ai)).AddTo(this);
            interactZone.OnAIExited.Subscribe(ai => _aiInZone.Remove(ai)).AddTo(this);

            HandCuffCount
                .Select(c => c > 0)
                .DistinctUntilChanged()
                .Where(hasItems => hasItems && _currentPlayer != null)
                .Subscribe(_ => StartTransferInterval(_currentPlayer))
                .AddTo(this);

            HandCuffCount
                .Subscribe(count =>
                {
                    var max = interactZone.MaxCount;
                    interactZone.SetMaxReached(max > 0 && count >= max);
                })
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

        public async UniTask PickupForAI(CharacterBase ai, int threshold, CancellationToken ct)
        {
            if (_itemPrefab == null) return;

            await UniTask.WaitUntil(() => _aiInZone.Contains(ai), cancellationToken: ct);

            var pickedUp = 0;
            while (pickedUp < threshold && !ct.IsCancellationRequested)
            {
                if (HandCuffCount.Value <= 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    continue;
                }
                HandCuffCount.Value--;
                _stackView.RemoveItem();
                FlyToAIAsync(ai, ct).Forget();
                pickedUp++;
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: ct);
            }
        }

        private async UniTaskVoid FlyToAIAsync(CharacterBase ai, CancellationToken ct)
        {
            var item = Instantiate(_itemPrefab);
            item.transform.position = output.position;
            var to = ai.transform.position + Vector3.up * HandCuffPlayerItemOffset;
            try
            {
                await TweenUtility.MoveArcAsync(item.transform, item.transform.position, to, ct);
            }
            finally
            {
                if (item != null) Destroy(item);
            }
            ai.Info.HandCuffCount.Value++;
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
            _pitchCount = 0;
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

            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.ItemCharge));
            _inGameModel.SoundPlayer.PlayOnce(clip, interactZone.transform.position, CalcPitch());
            _pitchCount = Mathf.Min(_pitchCount + 1, 10);
        }

        private float CalcPitch() => Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(_pitchCount / 9f));

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
