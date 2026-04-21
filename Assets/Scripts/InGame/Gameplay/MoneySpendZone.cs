using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;
using InGame;

namespace InGame.Gameplay
{
    public class MoneySpendZone : MonoBehaviour
    {
        [SerializeField] private InteractZone interactZone;
        [SerializeField] private MoneySpendPurchaseAction purchaseAction;
        private const int CostPerTick = 5;
        [SerializeField] private int fixedCost = 0;
        [SerializeField] private List<ActivateCondition> activateConditions;
        [SerializeField] private bool manualOnly;
        [SerializeField] private MoneySpendZoneUI zoneUI;
        [SerializeField] private string itemLabel;

        public MoneySpendPurchaseAction PurchaseAction => purchaseAction;

        private readonly Subject<Unit> _onPurchaseCompleted = new();
        public IObservable<Unit> OnPurchaseCompleted => _onPurchaseCompleted;

        private readonly Subject<Unit> _onActivated = new();
        public IObservable<Unit> OnActivated => _onActivated;

        private SerialDisposable _spendDisposable;
        private int _spentAmount;
        private int _pitchCount;
        private bool _isPurchased;
        private bool _waitingForReEnter;
        private InGameModel _inGameModel;
        private GameObject _moneyItemPrefab;
        private CharacterBase _cachedPlayer;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            _spendDisposable = new SerialDisposable().AddTo(this);
            interactZone.gameObject.SetActive(false);
            inGameModel.OnInitialized += OnInitialized;
            interactZone.OnPlayerInteracted.Subscribe(OnPlayerEntered).AddTo(this);
            interactZone.OnPlayerExited.Subscribe(OnPlayerLeft).AddTo(this);

            if (purchaseAction != MoneySpendPurchaseAction.MiningLevelUp)
                zoneUI?.SetLabel(itemLabel);
        }

        private void OnInitialized()
        {
            _inGameModel.OnInitialized -= OnInitialized;
            _moneyItemPrefab = _inGameModel.InGameAssetModel.GetView("money_stack");

            var player = _inGameModel.InGameObjectModel.Player;
            if (player == null) return;

            _cachedPlayer = player;

            if (manualOnly) return;

            if (activateConditions == null || activateConditions.Count == 0)
            {
                Activate();
                return;
            }

            var streams = activateConditions
                .Select(c => GetConditionStream(c, player))
                .ToList();

            Observable.CombineLatest(streams)
                .Where(results => results.All(r => r))
                .First()
                .Subscribe(_ => Activate())
                .AddTo(this);

            // 레벨업 조건 시 비용이 레벨에 따라 바뀌므로 레벨 변화 구독
            if (purchaseAction == MoneySpendPurchaseAction.MiningLevelUp)
                player.Info.MiningLevel.Subscribe(level =>
                {
                    RefreshCostUI();
                    RefreshLabelUI(level);
                }).AddTo(this);
            else
                RefreshCostUI();
        }

        private IObservable<bool> GetConditionStream(ActivateCondition cond, CharacterBase player)
        {
            return cond.type switch
            {
                ActivateConditionType.MoneyCarryCountReached =>
                    player.Info.MoneyCarryCount.Select(c => c >= cond.threshold),
                ActivateConditionType.MiningLevelReached =>
                    player.Info.MiningLevel.Select(l => l >= cond.threshold),
                ActivateConditionType.MiningItemCountReached =>
                    player.Info.MiningItemCount.Select(c => c >= cond.threshold),
                ActivateConditionType.HandCuffCountReached =>
                    player.Info.HandCuffCount.Select(c => c >= cond.threshold),
                ActivateConditionType.MoneyReached =>
                    player.Info.Money.Select(m => m >= cond.threshold),
                ActivateConditionType.ZonePurchased =>
                    cond.targetZone != null
                        ? cond.targetZone.OnPurchaseCompleted.Select(_ => true).StartWith(false)
                        : Observable.Return(false),
                _ => Observable.Return(false)
            };
        }

        private void Activate()
        {
            interactZone.gameObject.SetActive(true);
            _onActivated.OnNext(Unit.Default);
            RefreshCostUI();
            TweenUtility.PopScaleAsync(transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void RefreshCostUI()
        {
            var total = GetRequiredCost();
            zoneUI?.SetRemainingCost(total - _spentAmount, total);
        }

        private void RefreshLabelUI(int currentLevel)
        {
            var nextRecord = Global.Instance.TableManager.MiningEquipmentsRecord
                .GetRecordByLevel(currentLevel + 1);
            if (nextRecord != null)
                zoneUI?.SetLabel(nextRecord.Name);
        }

        private int GetRequiredCost()
        {
            if (fixedCost > 0) return fixedCost;
            if (_cachedPlayer == null) return 0;
            var record = Global.Instance.TableManager.MiningEquipmentsRecord
                .GetRecordByLevel(_cachedPlayer.Info.MiningLevel.Value);
            return record?.RequiredLevelUp ?? 0;
        }

        private void OnPlayerEntered(CharacterBase player)
        {
            if (_isPurchased) return;
            if (GetRequiredCost() <= 0) return;

            _spendDisposable.Disposable = Observable
                .Interval(TimeSpan.FromSeconds(0.2))
                .TakeWhile(_ => !_isPurchased && player.Info.MoneyCarryCount.Value > 0)
                .Subscribe(_ => SpendMoney(player));
        }

        private void OnPlayerLeft(CharacterBase player)
        {
            _spendDisposable.Disposable = Disposable.Empty;
            _pitchCount = 0;
            if (_waitingForReEnter)
            {
                _isPurchased = false;
                _waitingForReEnter = false;
            }
        }

        private void SpendMoney(CharacterBase player)
        {
            player.Info.MoneyCarryCount.Value--;
            _spentAmount += CostPerTick;
            FlyMoneyToZoneAsync(player.transform.position).Forget();

            var clip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.SpendMoney));
            _inGameModel.SoundPlayer.PlayOnce(clip, interactZone.transform.position, CalcPitch());
            _pitchCount = Mathf.Min(_pitchCount + 1, 10);
            RefreshCostUI();

            if (_spentAmount < GetRequiredCost()) return;

            _spentAmount = 0;
            _spendDisposable.Disposable = Disposable.Empty;

            var purchaseClip = _inGameModel.InGameAssetModel.GetAudioClip(nameof(SoundClip.PurchasedMoney));
            _inGameModel.SoundPlayer.PlayOnce(purchaseClip, interactZone.transform.position);

            if (purchaseAction == MoneySpendPurchaseAction.MiningLevelUp)
            {
                if (fixedCost <= 0)
                    player.Info.MiningLevel.Value++;

                var nextCost = GetRequiredCost();
                if (nextCost <= 0)
                    interactZone.gameObject.SetActive(false);
                else
                {
                    _isPurchased = true;
                    _waitingForReEnter = true;
                    RefreshCostUI();
                }
            }
            else
            {
                interactZone.gameObject.SetActive(false);
                _isPurchased = true;

                if (purchaseAction == MoneySpendPurchaseAction.GameClear)
                    _inGameModel.InvokeOnGameClear();
            }

            _onPurchaseCompleted.OnNext(Unit.Default);
        }

        private float CalcPitch() => Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(_pitchCount / 9f));

        private async UniTaskVoid FlyMoneyToZoneAsync(Vector3 playerPosition)
        {
            if (_moneyItemPrefab == null) return;

            var item = Instantiate(_moneyItemPrefab);
            var from = playerPosition + Vector3.up * HandCuffPlayerItemOffset;
            var to = interactZone.transform.position + Vector3.up * HandCuffPlayerItemOffset;
            item.transform.position = from;

            var ct = this.GetCancellationTokenOnDestroy();
            await TweenUtility.MoveArcAsync(item.transform, from, to, ct, duration: 0.23f);

            if (item != null) Destroy(item);
        }

        public void ForceActivate() => Activate();

        public void Stop()
        {
            _spendDisposable.Disposable = Disposable.Empty;
        }

        private void OnDestroy()
        {
            _onPurchaseCompleted.Dispose();
            _onActivated.Dispose();
        }
    }
}
