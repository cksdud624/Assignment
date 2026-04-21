using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Common.Template.Interface;
using Cysharp.Threading.Tasks;
using InGame.Gameplay;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public class MiningItemStackView : MonoBehaviour, IUpdateable
    {
        private CharacterHub _hub;
        private GameObject _itemPrefab;
        private Transform _oreStacksRoot;
        private Transform _maxLabelAnchor;

        private readonly List<GameObject> _stackedItems = new();
        private float _moveFactor;
        private float _wobbleElapsed = float.MaxValue;
        private bool _wasMoving;

        private MaxLabelController _maxLabelController;
        private CancellationTokenSource _maxLoopCts;

        public void Init(CharacterHub hub, InGameModel inGameModel)
        {
            _hub = hub;
            _itemPrefab = inGameModel.InGameAssetModel.GetView("ore_stack");

            var oreStacksObject = new GameObject("OreStacks");
            oreStacksObject.transform.SetParent(_hub.Model.transform);
            oreStacksObject.transform.localPosition = Vector3.zero;
            oreStacksObject.transform.localRotation = Quaternion.identity;
            _oreStacksRoot = oreStacksObject.transform;

            var anchorObject = new GameObject("MaxLabelAnchor");
            anchorObject.transform.SetParent(_oreStacksRoot);
            anchorObject.transform.localPosition = new Vector3(0f, 0f, -MiningBackOffset);
            _maxLabelAnchor = anchorObject.transform;

            hub.Info.MiningItemCount
                .Subscribe(OnCountChanged)
                .AddTo(this);

            if (hub.IsPlayer && inGameModel.MaxLabelController != null)
            {
                _maxLabelController = inGameModel.MaxLabelController;

                hub.Info.MiningItemCount
                    .Select(count => hub.Info.MaxMiningItemCount.Value > 0 && count >= hub.Info.MaxMiningItemCount.Value)
                    .DistinctUntilChanged()
                    .Subscribe(isAtMax =>
                    {
                        _maxLoopCts?.Cancel();
                        _maxLoopCts?.Dispose();
                        _maxLoopCts = null;

                        if (isAtMax)
                        {
                            _maxLoopCts = new CancellationTokenSource();
                            RunMaxLabelLoop(_maxLoopCts.Token).Forget();
                        }
                    })
                    .AddTo(this);
            }

            Global.Instance.BindUpdate(this);
        }

        private async UniTaskVoid RunMaxLabelLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var max = _hub.Info.MaxMiningItemCount.Value;
                _maxLabelController.ShowFloatingMax(_maxLabelAnchor, MiningHeightOffset);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_maxLabelController.FloatingRiseDuration),
                    cancellationToken: ct).SuppressCancellationThrow();
            }
        }

        public void OnUpdate()
        {
            if (_oreStacksRoot == null || _stackedItems.Count == 0) return;

            var velocity = _hub.Rigidbody.velocity;
            velocity.y = 0f;

            var isMoving = velocity.sqrMagnitude > 0.01f;
            if (_wasMoving && !isMoving)
                _wobbleElapsed = 0f;
            _wasMoving = isMoving;

            var targetMoveFactor = isMoving ? 1f : 0f;
            _moveFactor = Mathf.Lerp(_moveFactor, targetMoveFactor, Time.deltaTime * MiningTiltSpeed);

            var wobbleArc = 0f;
            if (_wobbleElapsed < MiningWobbleDuration)
            {
                _wobbleElapsed += Time.deltaTime;
                var decay = 1f - Mathf.Clamp01(_wobbleElapsed / MiningWobbleDuration);
                wobbleArc = Mathf.Sin(_wobbleElapsed * MiningWobbleFrequency * Mathf.PI * 2f) * MiningWobbleAmplitude * decay;
            }

            var localVelocity = _hub.Model.transform.InverseTransformDirection(velocity);
            var tiltX = Mathf.Clamp(-localVelocity.z * MiningTiltFactor, -MiningMaxTilt, MiningMaxTilt);
            var tiltZ = Mathf.Clamp(localVelocity.x * MiningTiltFactor, -MiningMaxTilt, MiningMaxTilt);

            var maxMining = Mathf.Max(1, _hub.Info.MaxMiningItemCount.Value);
            var maxIndex = Mathf.Max(1, _stackedItems.Count - 1);
            for (var i = 0; i < _stackedItems.Count; i++)
            {
                var curveRatio = (float)i / maxMining;
                var targetZ = -MiningBackOffset - MiningCurveDepth * (curveRatio * curveRatio) * (_moveFactor + wobbleArc);
                var pos = _stackedItems[i].transform.localPosition;
                pos.z = Mathf.Lerp(pos.z, targetZ, Time.deltaTime * MiningTiltSpeed);
                _stackedItems[i].transform.localPosition = pos;

                var tiltRatio = Mathf.Pow((float)i / maxIndex, 2f);
                var targetRotation = Quaternion.Euler(tiltX * tiltRatio, 0f, tiltZ * tiltRatio);
                _stackedItems[i].transform.localRotation = Quaternion.Lerp(
                    _stackedItems[i].transform.localRotation, targetRotation, Time.deltaTime * MiningTiltSpeed);
            }
        }

        private void OnCountChanged(int count)
        {
            while (_stackedItems.Count < count)
                SpawnItem();
            while (_stackedItems.Count > count)
                DespawnItem();
        }

        private void SpawnItem()
        {
            var index = _stackedItems.Count;
            var item = Instantiate(_itemPrefab, _oreStacksRoot);
            item.transform.localPosition = new Vector3(0f, MiningStackHeight * index + MiningHeightOffset, -MiningBackOffset);
            item.transform.localRotation = Quaternion.identity;
            _stackedItems.Add(item);
            TweenUtility.PopScaleAsync(item.transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void DespawnItem()
        {
            var last = _stackedItems[^1];
            _stackedItems.RemoveAt(_stackedItems.Count - 1);
            Destroy(last);
        }

        private void OnDestroy()
        {
            _maxLoopCts?.Cancel();
            _maxLoopCts?.Dispose();
            Global.Instance?.UnBindUpdate(this);
            foreach (var item in _stackedItems)
                if (item != null) Destroy(item);
        }
    }
}
