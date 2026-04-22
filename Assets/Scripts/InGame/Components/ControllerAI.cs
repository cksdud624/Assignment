using System;
using Common;
using Common.Template.Interface;
using InGame.Model;
using InGame.Object;
using UnityEngine;

namespace InGame.Components
{
    public class ControllerAI : ControllerBase, IUpdateable
    {
        private const float ArrivalThreshold = 0.3f;

        private Vector3? _targetPosition;
        private Action _onArrived;

        private Transform _followTarget;
        private float _followStopDistance;
        private bool _followWasMoving;

        private Transform _blockingTransform;
        private float _blockingStopDistance;

        public override void Init(InGameModel inGameModel, CharacterHub hub)
        {
            base.Init(inGameModel, hub);
            Global.Instance.BindUpdate(this);
        }

        public void MoveTo(Vector3 target, Action onArrived = null, Transform blocker = null, float blockerStopDistance = 0f)
        {
            _followTarget = null;
            _targetPosition = new Vector3(target.x, transform.position.y, target.z);
            _onArrived = onArrived;
            _blockingTransform = blocker;
            _blockingStopDistance = blockerStopDistance;
        }

        // 앞 캐릭터를 동적으로 추적 — stopDistance 이내면 정지, 멀어지면 재개
        // onFirstSettled: 처음 정착했을 때 한 번 호출
        public void FollowTransform(Transform target, float stopDistance)
        {
            _targetPosition = null;
            _onArrived = null;
            _followTarget = target;
            _followStopDistance = stopDistance;
            _followWasMoving = true;
            _blockingTransform = null;
        }

        public void StopMove()
        {
            _targetPosition = null;
            _followTarget = null;
            _onArrived = null;
            _blockingTransform = null;
            SetMoveDirection(Vector3.zero);
        }

        public void OnUpdate()
        {
            if (_followTarget != null)
            {
                var diff = _followTarget.position - transform.position;
                diff.y = 0f;
                if (diff.sqrMagnitude <= _followStopDistance * _followStopDistance)
                {
                    SetMoveDirection(Vector3.zero);
                    if (_followWasMoving)
                    {
                        _followWasMoving = false;
                    }
                }
                else
                {
                    _followWasMoving = true;
                    SetMoveDirection(diff.normalized);
                }
                return;
            }

            if (_targetPosition == null) return;

            if (_blockingTransform != null)
            {
                var toBlocker = _blockingTransform.position - transform.position;
                toBlocker.y = 0f;
                if (toBlocker.sqrMagnitude <= _blockingStopDistance * _blockingStopDistance)
                {
                    SetMoveDirection(Vector3.zero);
                    return;
                }
            }

            var toTarget = _targetPosition.Value - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= ArrivalThreshold * ArrivalThreshold)
            {
                SetMoveDirection(Vector3.zero);
                _targetPosition = null;
                _blockingTransform = null;
                var cb = _onArrived;
                _onArrived = null;
                cb?.Invoke();
                return;
            }

            SetMoveDirection(toTarget.normalized);
        }

        public override void Stop()
        {
            _targetPosition = null;
            _followTarget = null;
            _onArrived = null;
            _blockingTransform = null;
            Global.Instance?.UnBindUpdate(this);
            base.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Global.Instance?.UnBindUpdate(this);
        }
    }
}
