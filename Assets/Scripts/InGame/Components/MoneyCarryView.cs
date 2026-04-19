using System.Collections.Generic;
using Common;
using Common.Template.Interface;
using Cysharp.Threading.Tasks;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public class MoneyCarryView : MonoBehaviour, IUpdateable
    {
        private CharacterHub _hub;
        private GameObject _itemPrefab;
        private Transform _stackRoot;
        private readonly List<GameObject> _stackedItems = new();

        private float _moveFactor;
        private float _wobbleElapsed = float.MaxValue;
        private bool _wasMoving;

        public void Init(CharacterHub hub, InGameModel inGameModel)
        {
            _hub = hub;
            _itemPrefab = inGameModel.InGameAssetModel.GetView("money_stack");

            var stackObject = new GameObject("MoneyStacks");
            stackObject.transform.SetParent(hub.Model.transform);
            stackObject.transform.localPosition = Vector3.zero;
            stackObject.transform.localRotation = Quaternion.identity;
            _stackRoot = stackObject.transform;

            hub.Info.MoneyCarryCount
                .Subscribe(OnCountChanged)
                .AddTo(this);

            Global.Instance.BindUpdate(this);
        }

        private float BaseZ => _hub.Info.MiningItemCount.Value > 0
            ? -(MiningBackOffset + MiningCurveDepth)
            : -MiningBackOffset;

        public void OnUpdate()
        {
            if (_stackRoot == null || _stackedItems.Count == 0) return;

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

            var baseZ = BaseZ;
            var maxIndex = Mathf.Max(1, _stackedItems.Count - 1);
            var maxCount = Mathf.Max(1, _hub.Info.MoneyCarryCount.Value);

            for (var i = 0; i < _stackedItems.Count; i++)
            {
                var curveRatio = (float)i / maxCount;
                var targetZ = baseZ - MiningCurveDepth * (curveRatio * curveRatio) * (_moveFactor + wobbleArc);
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
            while (_stackedItems.Count < count) SpawnItem();
            while (_stackedItems.Count > count) DespawnItem();
        }

        private void SpawnItem()
        {
            var item = Instantiate(_itemPrefab, _stackRoot);
            item.transform.localPosition = new Vector3(
                0f, MoneyCarryHeightOffset + MoneyCarryStackHeight * _stackedItems.Count, BaseZ);
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
            Global.Instance?.UnBindUpdate(this);
            foreach (var item in _stackedItems)
                if (item != null) Destroy(item);
        }
    }
}
