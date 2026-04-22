using System;
using System.Collections.Generic;
using Common;
using InGame.Model;
using InGame.Object;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public class MiningRangeTrigger : MonoBehaviour
    {
        private const float FadeDuration = 0.1f;

        private BoxCollider _triggerCollider;
        private float _miningTime;
        private float _miningTrigger;
        private CharacterHub _hub;
        private readonly List<ObjectBase> _overlappingObjects = new();
        private readonly Dictionary<ObjectBase, IDisposable> _oreSubscriptions = new();
        private readonly SerialDisposable _miningLoop = new();
        private readonly SerialDisposable _cooldownDisposable = new();
        private readonly Subject<Unit> _onMiningCompleted = new();
        private readonly Subject<Unit> _onMiningStarted = new();
        private readonly Subject<Unit> _onOreAcquiredForAI = new();
        public IObservable<Unit> OnMiningCompleted => _onMiningCompleted;
        public IObservable<Unit> OnMiningStarted => _onMiningStarted;
        public IObservable<Unit> OnOreAcquiredForAI => _onOreAcquiredForAI;
        private bool _isMining;
        private bool _onCooldown;
        private float _cycleEndTime;
        private AnimationClip _miningClip;
        private InGameModel _inGameModel;
        private InGameAssetModel _assetModel;
        private AudioSource _loopSource;
        private readonly Dictionary<ObjectBase, int> _oreHitCounts = new();
        private const int AIRequiredHits = 2;
        public Action<Vector3> OnItemMinedDirect { get; set; }

        public void Init(CharacterHub hub, InGameModel inGameModel)
        {
            _hub = hub;
            _inGameModel = inGameModel;
            _assetModel = _inGameModel.InGameAssetModel;
            var initialRecord = Global.Instance.TableManager.MiningEquipmentsRecord.GetRecordByLevel(hub.Info.MiningLevel.Value);
            _miningTime = initialRecord?.MiningTime ?? 1f;
            _miningTrigger = initialRecord?.MiningTrigger ?? 1f;
            hub.Info.MaxMiningItemCount.Value = initialRecord?.MaxMiningItemCount ?? 0;

            var triggerObject = new GameObject("MiningRangeTrigger");
            triggerObject.transform.SetParent(hub.FacingNode);
            triggerObject.transform.localPosition = Vector3.zero;
            triggerObject.transform.localRotation = Quaternion.identity;

            _triggerCollider = triggerObject.AddComponent<BoxCollider>();
            _triggerCollider.isTrigger = true;

            _triggerCollider.OnTriggerEnterAsObservable().Subscribe(OnTriggerEntered).AddTo(this);
            _triggerCollider.OnTriggerExitAsObservable().Subscribe(OnTriggerExited).AddTo(this);

            _miningClip = _assetModel?.GetAnimationClip($"{hub.Info.MiningLevel.Value}_mining");
            hub.Info.MiningLevel.Subscribe(OnMiningLevelChanged).AddTo(this);
            hub.CharacterState.Subscribe(_ => Refresh()).AddTo(this);

            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.spatialBlend = 1f;
            _loopSource.loop = true;
            _loopSource.playOnAwake = false;
            _loopSource.pitch = 1f;
            _loopSource.volume = 1f;

            hub.CharacterState
                .CombineLatest(hub.Info.MiningLevel, (state, level) => state == CharacterState.Mining && level >= 2)
                .DistinctUntilChanged()
                .Subscribe(shouldLoop =>
                {
                    if (shouldLoop)
                    {
                        var clip = _assetModel?.GetAudioClip(nameof(SoundClip.Mining2));
                        if (clip != null && _loopSource != null)
                        {
                            _loopSource.clip = clip;
                            _loopSource.Play();
                        }
                    }
                    else
                    {
                        _loopSource?.Stop();
                    }
                })
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
            _miningClip = _assetModel?.GetAnimationClip($"{level}_mining");
        }

        private void OnTriggerEntered(Collider col)
        {
            var obj = col.GetComponentInParent<ObjectBase>();
            if (obj == null || obj.Type != ObjectType.Mining) return;
            if (_oreSubscriptions.ContainsKey(obj)) return;

            _overlappingObjects.Add(obj);
            _oreSubscriptions[obj] = obj.State.Subscribe(_ => Refresh());
            Refresh();
        }

        private void OnTriggerExited(Collider col)
        {
            var obj = col.GetComponentInParent<ObjectBase>();
            if (obj == null || obj.Type != ObjectType.Mining) return;

            if (_oreSubscriptions.TryGetValue(obj, out var sub))
            {
                sub.Dispose();
                _oreSubscriptions.Remove(obj);
            }
            _overlappingObjects.Remove(obj);
            _oreHitCounts.Remove(obj);
            Refresh();
        }

        private void Refresh()
        {
            if (_hub.CharacterState.Value != CharacterState.Mining)
            {
                if (_isMining) StopLoop();
                else CancelCooldown();
                _hub.IsLowerBodyLocked.Value = false;
                _hub.IsMoving.SetValueAndForceNotify(_hub.IsMoving.Value);
                return;
            }

            if (_hub.Info.MiningLevel.Value == 1)
            {
                var hasOre = HasPlayingOre();

                if (_isMining)
                {
                    if (!hasOre) StopLoop();
                    return;
                }

                if (_onCooldown) return;

                if (hasOre) StartLoop();
            }
            else if(_hub.Info.MiningLevel.Value == 2)
            {
                if (!_isMining)
                {
                    _isMining = true;
                    _hub.AnimationPlayer.PlayUpperBodyAnimationClip(_miningClip, null, 0.1f);
                }
                foreach (var obj in _overlappingObjects)
                {
                    if (obj.State.Value == ObjectState.Playing)
                    {
                        if (_hub.Info.MiningItemCount.Value < _hub.Info.MaxMiningItemCount.Value)
                            _hub.Info.MiningItemCount.Value++;
                        else
                            _hub.Info.MiningItemCount.SetValueAndForceNotify(_hub.Info.MiningItemCount.Value);
                        obj.Disappear();
                        PlayMiningOre(obj.transform.position);
                    }
                }

                _miningLoop.Disposable = Observable
                    .Timer(TimeSpan.FromSeconds(_miningTime))
                    .Subscribe(_ => Refresh());
            }
            else
            {
                if (!_isMining)
                {
                    _isMining = true;
                    _hub.AnimationPlayer.PlayAnimationClip(_miningClip);
                    _hub.IsLowerBodyLocked.Value = true;
                }
                foreach (var obj in _overlappingObjects)
                {
                    if (obj.State.Value == ObjectState.Playing)
                    {
                        if (_hub.Info.MiningItemCount.Value < _hub.Info.MaxMiningItemCount.Value)
                            _hub.Info.MiningItemCount.Value++;
                        else
                            _hub.Info.MiningItemCount.SetValueAndForceNotify(_hub.Info.MiningItemCount.Value);
                        obj.Disappear();
                        PlayMiningOre(obj.transform.position);
                    }
                }

                _miningLoop.Disposable = Observable
                    .Timer(TimeSpan.FromSeconds(_miningTime))
                    .Subscribe(_ => Refresh());
            }
        }
        
        #region Level 1 And AI
        private void StartLoop()
        {
            _onCooldown = false;
            _isMining = true;
            _cycleEndTime = Time.time + _miningTime;
            _onMiningStarted.OnNext(Unit.Default);
            _hub.AnimationPlayer.PlayUpperBodyAnimationClip(_miningClip, _miningTime, crossFadeDuration: 0.1f);

            _miningLoop.Disposable = Observable
                .Timer(TimeSpan.FromSeconds(_miningTime * _miningTrigger))
                .Subscribe(_ => OnMineEvent());
        }
        
        private void StopLoop()
        {
            if (!_isMining) return;
            _isMining = false;
            _miningLoop.Disposable = Disposable.Empty;
            _hub.AnimationPlayer.StopUpperBodyAnimation(FadeDuration);

            var remaining = Mathf.Max(0f, _cycleEndTime - Time.time);
            if (remaining > 0f)
            {
                _onCooldown = true;
                _cooldownDisposable.Disposable = Observable
                    .Timer(TimeSpan.FromSeconds(remaining))
                    .Subscribe(_ =>
                    {
                        _onCooldown = false;
                        if (_hub.CharacterState.Value != CharacterState.Mining) return;
                        if (!HasPlayingOre())
                            _onMiningCompleted.OnNext(Unit.Default);
                        else
                            Refresh();
                    });
            }
            else
            {
                if (_hub.CharacterState.Value != CharacterState.Mining) return;
                if (!HasPlayingOre())
                    _onMiningCompleted.OnNext(Unit.Default);
            }
        }
        
        private void OnMineEvent()
        {
            var closest = GetClosest();
            if (closest != null)
            {
                _inGameModel.SoundPlayer.PlayOnce(_assetModel.GetAudioClip(nameof(SoundClip.Mining1)), transform.position);

                if (!_hub.IsPlayer)
                {
                    _oreHitCounts.TryGetValue(closest, out var hits);
                    hits++;
                    if (hits < AIRequiredHits)
                    {
                        _oreHitCounts[closest] = hits;
                        // 아직 2번째 히트 미달 — 사운드만 재생하고 채굴 계속
                    }
                    else
                    {
                        _oreHitCounts.Remove(closest);
                        if (OnItemMinedDirect != null)
                            OnItemMinedDirect.Invoke(closest.transform.position);
                        else if (_hub.Info.MiningItemCount.Value < _hub.Info.MaxMiningItemCount.Value)
                            _hub.Info.MiningItemCount.Value++;
                        closest.Disappear();

                        _miningLoop.Disposable = Disposable.Empty;
                        _isMining = false;
                        _hub.AnimationPlayer.StopUpperBodyAnimation(FadeDuration);
                        _onOreAcquiredForAI.OnNext(Unit.Default);
                        return;
                    }
                }
                else
                {
                    if (OnItemMinedDirect != null)
                        OnItemMinedDirect.Invoke(closest.transform.position);
                    else if (_hub.Info.MiningItemCount.Value < _hub.Info.MaxMiningItemCount.Value)
                        _hub.Info.MiningItemCount.Value++;
                    else
                        _hub.Info.MiningItemCount.SetValueAndForceNotify(_hub.Info.MiningItemCount.Value);
                    closest.Disappear();
                }
            }

            if (!_isMining) return;

            _miningLoop.Disposable = Observable
                .Timer(TimeSpan.FromSeconds(_miningTime * (1f - _miningTrigger)))
                .Subscribe(_ =>
                {
                    if (HasPlayingOre())
                        StartLoop();
                    else
                        StopLoop();
                });
        }
        
        private ObjectBase GetClosest()
        {
            ObjectBase closest = null;
            var minDist = float.MaxValue;
            var pos = transform.position;
            foreach (var obj in _overlappingObjects)
            {
                if (obj == null || obj.State.Value != ObjectState.Playing) continue;
                var dist = (obj.transform.position - pos).sqrMagnitude;
                if (dist >= minDist) continue;
                minDist = dist;
                closest = obj;
            }
            return closest;
        }

        private bool HasPlayingOre()
        {
            foreach (var obj in _overlappingObjects)
            {
                if (obj != null && obj.State.Value == ObjectState.Playing)
                    return true;
            }
            return false;
        }
        #endregion
        
        private void PlayMiningOre(Vector3 position)
        {
            var clip = _assetModel?.GetAudioClip(nameof(SoundClip.MiningOre));
            if (clip != null)
                _inGameModel.SoundPlayer.PlayOnce(clip, position, volume: 0.7f);
        }

        private void CancelCooldown()
        {
            if (!_onCooldown) return;
            _onCooldown = false;
            _cooldownDisposable.Disposable = Disposable.Empty;
        }

        public void Stop()
        {
            _loopSource?.Stop();
            _miningLoop.Disposable = Disposable.Empty;
            _cooldownDisposable.Disposable = Disposable.Empty;
            foreach (var sub in _oreSubscriptions.Values)
                sub.Dispose();
            _oreSubscriptions.Clear();
            _overlappingObjects.Clear();
            _oreHitCounts.Clear();
            _isMining = false;
            _onCooldown = false;
            if (_triggerCollider != null)
                _triggerCollider.enabled = false;
        }

        private void OnDestroy()
        {
            foreach (var sub in _oreSubscriptions.Values)
                sub.Dispose();
            _oreSubscriptions.Clear();
            _miningLoop.Dispose();
            _cooldownDisposable.Dispose();
            _onMiningCompleted.Dispose();
            _onMiningStarted.Dispose();
            _onOreAcquiredForAI.Dispose();
        }
    }
}
