using InGame.Components;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Gameplay
{
    public class MinerAIBrain : MonoBehaviour
    {
        private CharacterBase _ai;
        private ControllerAI _controller;
        private MiningZone _miningZone;
        private CompositeDisposable _disposable;

        public void Init(CharacterBase ai, MiningZone miningZone, HandCuffMachineZone handCuffMachineZone)
        {
            _ai = ai;
            _controller = (ControllerAI)ai.Controller;
            _miningZone = miningZone;
            ai.Info.MiningLevel.Value = 1;
            ai.SetCharacterState(CharacterState.Mining);

            _disposable = new CompositeDisposable();

            var trigger = ai.GetComponent<MiningRangeTrigger>();
            trigger.OnItemMinedDirect = handCuffMachineZone.ReceiveAIItem;

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateTarget())
                .AddTo(_disposable);
        }

        private void UpdateTarget()
        {
            var ore = _miningZone.GetClosestPlayingOre(_ai.transform.position);
            if (ore != null)
                _controller.FollowTransform(ore.transform, 0.5f);
            else
                _controller.StopMove();
        }

        public void Stop() => _disposable.Dispose();

        private void OnDestroy() => _disposable.Dispose();
    }
}
