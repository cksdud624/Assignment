using System;
using InGame.Components;
using InGame.Object;
using UniRx;
using UnityEngine;

namespace InGame.Gameplay
{
    public class MinerAIBrain : MonoBehaviour
    {
        private CharacterBase _ai;
        private ControllerAI _controller;
        private MiningZone _miningZone;

        private CompositeDisposable _disposable;
        private SerialDisposable _retryDisposable;

        public void Init(CharacterBase ai, MiningZone miningZone, HandCuffMachineZone handCuffMachineZone)
        {
            _ai = ai;
            _controller = (ControllerAI)ai.Controller;
            _miningZone = miningZone;
            ai.Info.MiningLevel.Value = 1;

            _disposable = new CompositeDisposable();
            _retryDisposable = new SerialDisposable().AddTo(_disposable);

            var trigger = ai.GetComponent<MiningRangeTrigger>();
            trigger.OnItemMinedDirect = handCuffMachineZone.ReceiveAIItem;
            trigger.OnMiningCompleted
                .Subscribe(_ => SeekNextOre())
                .AddTo(_disposable);
            trigger.OnMiningStarted
                .Subscribe(_ => _controller.StopMove())
                .AddTo(_disposable);

            SeekNextOre();
        }

        private void SeekNextOre()
        {
            _retryDisposable.Disposable = Disposable.Empty;
            _controller.StopMove();

            var ore = _miningZone.GetClosestPlayingOre(_ai.transform.position);

            if (ore == null)
            {
                _retryDisposable.Disposable = Observable
                    .Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => SeekNextOre());
                return;
            }

            _controller.FollowTransform(ore.transform, 0.5f);
        }

        public void Stop() => _disposable.Dispose();

        private void OnDestroy()
        {
            _disposable.Dispose();
        }
    }
}
