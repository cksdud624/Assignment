using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Object;
using static Common.GameDefine;
using UniRx;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Components
{
    public class AnimationPlayer : MonoBehaviour
    {
        private Animator _animator;
        private readonly Dictionary<int, AnimationClipPlayable> _animationClips = new();

        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _layerMixer;

        // Layer 0 - full body
        private AnimationMixerPlayable _fullBodyMixer;
        private AnimationClipPlayable _currentClip;
        private AnimationClipPlayable _targetClip;
        private int _currentSlot;
        private CancellationTokenSource _fadeCts;

        // Layer 1 - upper body
        private AnimationMixerPlayable _upperBodyMixer;
        private AnimationClipPlayable _upperCurrentClip;
        private AnimationClipPlayable _upperTargetClip;
        private int _upperCurrentSlot;
        private CancellationTokenSource _upperFadeCts;
        private CancellationTokenSource _layerFadeCts;
        
        private bool _isLowerBodyLocked = false;

        public void Init<TAnimation>(GameObject model, Dictionary<TAnimation, AnimationClip> clips) where TAnimation : Enum
        {
            _graph = PlayableGraph.Create("AnimationGraph");
            var modelAnimator = model.GetComponent<Animator>();
            _animator = modelAnimator == null ? model.AddComponent<Animator>() : modelAnimator;
            _animator.applyRootMotion = false;

            foreach (var clip in clips)
                _animationClips.Add(Convert.ToInt32(clip.Key), AnimationClipPlayable.Create(_graph, clip.Value));

            var output = AnimationPlayableOutput.Create(_graph, "AnimationPlayer", _animator);
            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);

            _fullBodyMixer = AnimationMixerPlayable.Create(_graph, 2);
            _layerMixer.ConnectInput(0, _fullBodyMixer, 0, 1f);

            _upperBodyMixer = AnimationMixerPlayable.Create(_graph, 2);
            _layerMixer.ConnectInput(1, _upperBodyMixer, 0, 0f);
            _layerMixer.SetLayerMaskFromAvatarMask(1, CreateUpperBodyMask());

            output.SetSourcePlayable(_layerMixer);
            _graph.Play();
        }

        public void Init<TAnimation>(GameObject model, Dictionary<TAnimation, AnimationClip> clips, CharacterHub hub) where TAnimation : Enum
        {
            Init(model, clips);
            hub.IsMoving.Subscribe(OnIsMovingChanged).AddTo(this);
            hub.IsLowerBodyLocked.Subscribe(isLocked =>  _isLowerBodyLocked = isLocked);
        }

        public Transform GetBoneTransform(HumanBodyBones bone)
        {
            if (_animator == null)
            {
                Debug.LogError("[AnimationPlayer] Animator is null");
                return null;
            }
            if (!_animator.isHuman)
            {
                Debug.LogError("[AnimationPlayer] Animator is not Humanoid");
                return null;
            }
            return _animator.GetBoneTransform(bone);
        }

        public void PlayAnimation<TAnimation>(TAnimation anim, Action onAnimationEnd = null) where TAnimation : Enum
        {
            if (_animationClips.TryGetValue(Convert.ToInt32(anim), out var clip))
                CrossFade(clip, onAnimationEnd: onAnimationEnd).Forget();
        }

        public void PlayAnimationClip(AnimationClip clip)
        {
            var playable = AnimationClipPlayable.Create(_graph, clip);
            CrossFade(playable).Forget();
        }

        public void PlayUpperBodyAnimation<TAnimation>(TAnimation anim, float? desiredDuration = null, float crossFadeDuration = 0.3f) where TAnimation : Enum
        {
            _layerFadeCts?.Cancel();
            _layerFadeCts?.Dispose();
            _layerFadeCts = null;
            _layerMixer.SetInputWeight(1, 1f);
            if (_animationClips.TryGetValue(Convert.ToInt32(anim), out var clip))
                CrossFadeUpperBody(clip, crossFadeDuration, desiredDuration: desiredDuration).Forget();
        }

        public void PlayUpperBodyAnimationClip(AnimationClip clip, float? desiredDuration = null, float crossFadeDuration = 0.3f)
        {
            if (clip == null) return;
            _layerFadeCts?.Cancel();
            _layerFadeCts?.Dispose();
            _layerFadeCts = null;
            _layerMixer.SetInputWeight(1, 1f);
            var playable = AnimationClipPlayable.Create(_graph, clip);
            CrossFadeUpperBody(playable, crossFadeDuration, desiredDuration: desiredDuration).Forget();
        }

        public void StopUpperBodyAnimation(float duration = 0.3f)
        {
            _upperFadeCts?.Cancel();
            _upperFadeCts?.Dispose();
            _upperFadeCts = null;
            FadeOutUpperBodyAsync(duration).Forget();
        }

        private async UniTaskVoid FadeOutUpperBodyAsync(float duration)
        {
            var completed = await FadeLayerWeight(1, 0f, duration);
            if (!completed) return;
            if (_upperBodyMixer.GetInput(0).IsValid()) _upperBodyMixer.DisconnectInput(0);
            if (_upperBodyMixer.GetInput(1).IsValid()) _upperBodyMixer.DisconnectInput(1);
            _upperBodyMixer.SetInputWeight(0, 0f);
            _upperBodyMixer.SetInputWeight(1, 0f);
            _upperCurrentClip = default;
        }

        private void OnIsMovingChanged(bool isMoving)
        {
            if (!_isLowerBodyLocked)
            {
                var key = isMoving
                    ? Convert.ToInt32(InGameCommonAnimation.Walk)
                    : Convert.ToInt32(InGameCommonAnimation.Idle);
                if (_animationClips.TryGetValue(key, out var clip))
                    CrossFade(clip).Forget();
            }
        }

        private async UniTask CrossFade(AnimationClipPlayable nextClip, float duration = 0.3f, Action onAnimationEnd = null)
        {
            if (_targetClip.IsValid() && _targetClip.GetHandle() == nextClip.GetHandle()) return;

            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = new CancellationTokenSource();
            var token = _fadeCts.Token;

            _targetClip = nextClip;
            var nextSlot = 1 - _currentSlot;

            if (_fullBodyMixer.GetInput(nextSlot).IsValid())
                _fullBodyMixer.DisconnectInput(nextSlot);

            if (_currentClip.IsValid() && _currentClip.GetHandle() == nextClip.GetHandle())
            {
                _fullBodyMixer.SetInputWeight(_currentSlot, 1f);
                return;
            }

            nextClip.SetTime(0);
            nextClip.SetDone(false);
            nextClip.Play();
            _fullBodyMixer.ConnectInput(nextSlot, nextClip, 0);

            if (!_currentClip.IsValid())
            {
                _fullBodyMixer.SetInputWeight(nextSlot, 1f);
                _currentClip = nextClip;
                _currentSlot = nextSlot;
                return;
            }

            _fullBodyMixer.SetInputWeight(_currentSlot, 1f);
            _fullBodyMixer.SetInputWeight(nextSlot, 0f);

            try
            {
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    _fullBodyMixer.SetInputWeight(nextSlot, t);
                    _fullBodyMixer.SetInputWeight(_currentSlot, 1f - t);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _fullBodyMixer.SetInputWeight(nextSlot, 1f);
            _fullBodyMixer.SetInputWeight(_currentSlot, 0f);
            _fullBodyMixer.DisconnectInput(_currentSlot);

            _currentClip = nextClip;
            _currentSlot = nextSlot;
            
            onAnimationEnd?.Invoke();
        }

        private async UniTask CrossFadeUpperBody(AnimationClipPlayable nextClip, float duration = 0.3f, float? desiredDuration = null)
        {
            _upperFadeCts?.Cancel();
            _upperFadeCts?.Dispose();
            _upperFadeCts = new CancellationTokenSource();
            var token = _upperFadeCts.Token;

            _upperTargetClip = nextClip;
            var nextSlot = 1 - _upperCurrentSlot;

            if (_upperBodyMixer.GetInput(nextSlot).IsValid())
                _upperBodyMixer.DisconnectInput(nextSlot);

            if (_upperCurrentClip.IsValid() && _upperCurrentClip.GetHandle() == nextClip.GetHandle())
            {
                nextClip.SetSpeed(desiredDuration.HasValue ? nextClip.GetAnimationClip().length / desiredDuration.Value : 1.0);
                nextClip.SetTime(0);
                nextClip.SetDone(false);
                nextClip.Play();
                _upperBodyMixer.SetInputWeight(_upperCurrentSlot, 1f);
                return;
            }

            nextClip.SetSpeed(desiredDuration.HasValue ? nextClip.GetAnimationClip().length / desiredDuration.Value : 1.0);
            nextClip.SetTime(0);
            nextClip.SetDone(false);
            nextClip.Play();
            _upperBodyMixer.ConnectInput(nextSlot, nextClip, 0);

            // Immediately clear old clip so it doesn't bleed through during fade-in
            if (_upperCurrentClip.IsValid())
            {
                _upperBodyMixer.DisconnectInput(_upperCurrentSlot);
                _upperCurrentClip = default;
            }

            _upperBodyMixer.SetInputWeight(nextSlot, 0f);

            try
            {
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    _upperBodyMixer.SetInputWeight(nextSlot, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _upperBodyMixer.SetInputWeight(nextSlot, 1f);
            _upperCurrentClip = nextClip;
            _upperCurrentSlot = nextSlot;
        }

        private async UniTask<bool> FadeLayerWeight(int layerIndex, float targetWeight, float duration)
        {
            _layerFadeCts?.Cancel();
            _layerFadeCts?.Dispose();
            _layerFadeCts = new CancellationTokenSource();
            var token = _layerFadeCts.Token;

            var startWeight = _layerMixer.GetInputWeight(layerIndex);
            var elapsed = 0f;
            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    _layerMixer.SetInputWeight(layerIndex, Mathf.Lerp(startWeight, targetWeight, t));
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            _layerMixer.SetInputWeight(layerIndex, targetWeight);
            return true;
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            var mask = new AvatarMask();
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);
            return mask;
        }

        private void OnDestroy()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _upperFadeCts?.Cancel();
            _upperFadeCts?.Dispose();
            _layerFadeCts?.Cancel();
            _layerFadeCts?.Dispose();
            if (_graph.IsValid())
                _graph.Destroy();
        }
    }
}
