using System;
using System.Collections.Generic;
using Common;
using InGame.Object;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public class MiningRangeTrigger : MonoBehaviour
    {
        private BoxCollider _triggerCollider;
        private float _miningTime;
        private float _miningTrigger;
        private CharacterHub _hub;
        private readonly List<ObjectBase> _overlappingObjects = new();

        private readonly SerialDisposable _timerDisposable = new();
        private readonly Subject<ObjectBase> _onMineCompleted = new();
        public IObservable<ObjectBase> OnMineCompleted => _onMineCompleted;

        public void Init(CharacterHub hub)
        {
            _hub = hub;
            var initialRecord = Global.Instance.TableManager.MiningEquipmentsRecord.GetRecordByLevel(hub.Info.MiningLevel.Value);
            _miningTime = initialRecord?.MiningTime ?? 1f;
            _miningTrigger = initialRecord?.MiningTrigger ?? 1f;
            hub.Info.MaxMiningItemCount.Value = initialRecord?.MaxMiningItemCount ?? 0;

            var triggerObject = new GameObject("MiningRangeTrigger");
            triggerObject.transform.SetParent(hub.Model.transform);
            triggerObject.transform.localPosition = Vector3.zero;
            triggerObject.transform.localRotation = Quaternion.identity;

            _triggerCollider = triggerObject.AddComponent<BoxCollider>();
            _triggerCollider.isTrigger = true;

            _triggerCollider.OnTriggerEnterAsObservable()
                .Subscribe(OnTriggerEntered)
                .AddTo(this);

            _triggerCollider.OnTriggerExitAsObservable()
                .Subscribe(OnTriggerExited)
                .AddTo(this);

            hub.Info.MiningLevel
                .Subscribe(OnMiningLevelChanged)
                .AddTo(this);

            hub.CharacterState
                .Subscribe(OnCharacterStateChanged)
                .AddTo(this);
        }

        private void OnMiningLevelChanged(int level)
        {
            var record = Global.Instance.TableManager.MiningEquipmentsRecord.GetRecordByLevel(level);
            if (record == null)
            {
                Debug.LogWarning($"[MiningRangeTrigger] No record for level {level}");
                return;
            }
            _triggerCollider.center = record.Center;
            _triggerCollider.size = record.Range;
            _miningTime = record.MiningTime;
            _miningTrigger = record.MiningTrigger;
            _hub.Info.MaxMiningItemCount.Value = record.MaxMiningItemCount;
        }

        private void OnCharacterStateChanged(CharacterState state)
        {
            var isMining = state == CharacterState.Mining;
            _triggerCollider.enabled = isMining;
            if (isMining)
            {
                StartMiningTimer();
            }
            else
            {
                _overlappingObjects.Clear();
                _timerDisposable.Disposable = Disposable.Empty;
            }
        }

        private void OnTriggerEntered(Collider col)
        {
            var obj = col.GetComponentInParent<ObjectBase>();
            if (obj == null || obj.Type != ObjectType.Mining) return;
            _overlappingObjects.Add(obj);
        }

        private void OnTriggerExited(Collider col)
        {
            var obj = col.GetComponentInParent<ObjectBase>();
            if (obj == null || obj.Type != ObjectType.Mining) return;
            _overlappingObjects.Remove(obj);
            if (_overlappingObjects.Count == 0)
                _timerDisposable.Disposable = Disposable.Empty;
        }

        private void StartMiningTimer()
        {
            _timerDisposable.Disposable = Observable
                .Timer(TimeSpan.FromSeconds(_miningTime * _miningTrigger))
                .Subscribe(_ =>
                {
                    var closest = GetClosest();
                    if (closest != null)
                    {
                        if (_hub.Info.MiningItemCount.Value < _hub.Info.MaxMiningItemCount.Value)
                            _hub.Info.MiningItemCount.Value++;
                        else
                            Debug.Log($"[MiningRangeTrigger] MiningItemCount is at max ({_hub.Info.MaxMiningItemCount.Value})");
                        _onMineCompleted.OnNext(closest);
                        closest.Disappear();
                    }
                    _timerDisposable.Disposable = Observable
                        .Timer(TimeSpan.FromSeconds(_miningTime * (1f - _miningTrigger)))
                        .Subscribe(__ => StartMiningTimer());
                });
        }

        private ObjectBase GetClosest()
        {
            ObjectBase closest = null;
            var minDist = float.MaxValue;
            var pos = transform.position;
            foreach (var obj in _overlappingObjects)
            {
                if (obj == null || obj.State != ObjectState.Playing) continue;
                var dist = (obj.transform.position - pos).sqrMagnitude;
                if (dist >= minDist) continue;
                minDist = dist;
                closest = obj;
            }
            return closest;
        }

        private void OnDestroy()
        {
            _timerDisposable.Dispose();
            _onMineCompleted.Dispose();
        }
    }
}
