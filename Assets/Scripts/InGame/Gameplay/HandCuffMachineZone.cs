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
        public InteractZone InteractZone => interactZone;
        public HandCuffOutputZone HandCuffOutputZone => handCuffOutputZone;

        private InGameModel _inGameModel;
        private SerialDisposable _transferDisposable;
        private SerialDisposable _consumeDisposable;
        private GameObject _itemPrefab;
        private StackView _stackView;
        private int _pitchCount;

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
                .Select(c => c > 0)
                .DistinctUntilChanged()
                .Where(hasItems => hasItems)
                .Subscribe(_ => StartConsumeLoop())
                .AddTo(this);

            MiningItemCount
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
            _pitchCount = 0;
        }

        #region Events

        private void StartConsumeLoop()
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

            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.MachineActivate));
            _inGameModel.SoundPlayer.PlayOnce(clip, machine.position);
        }

        #endregion

        private float CalcPitch() => Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(_pitchCount / 9f));

        public void Stop()
        {
            _transferDisposable.Disposable = Disposable.Empty;
            _consumeDisposable.Disposable = Disposable.Empty;
        }

        public void ReceiveAIItem(Vector3 from)
        {
            ReceiveAIItemAsync(from).Forget();
        }

        private async UniTaskVoid ReceiveAIItemAsync(Vector3 from)
        {
            if (_itemPrefab == null) return;
            var max = interactZone.MaxCount;
            if (max > 0 && MiningItemCount.Value >= max) return;
            var worldDest = _stackView.GetNextWorldPosition();
            var item = Instantiate(_itemPrefab);
            item.transform.position = from;
            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, worldDest, ct);
            if (item == null) return;
            _stackView.AddItem(item);
            MiningItemCount.Value++;
        }

        private void TransferItem(CharacterBase player)
        {
            var max = interactZone.MaxCount;
            if (max > 0 && MiningItemCount.Value >= max) return;

            player.Info.MiningItemCount.Value--;
            MiningItemCount.Value++;
            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.ItemCharge));
            _inGameModel.SoundPlayer.PlayOnce(clip, interactZone.transform.position, CalcPitch());
            _pitchCount = Mathf.Min(_pitchCount + 1, 10);

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
