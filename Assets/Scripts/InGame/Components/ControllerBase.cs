using Common;
using Common.Template.Interface;
using InGame.Model;
using InGame.Object;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public abstract class ControllerBase : MonoBehaviour, IFixedUpdateable
    {

        protected InGameModel InGameModel { get; private set; }
        protected CharacterHub Hub { get; private set; }

        private Vector3 _moveDirection;

        public virtual void Init(InGameModel inGameModel, CharacterHub hub)
        {
            InGameModel = inGameModel;
            Hub = hub;
            Global.Instance.BindFixedUpdate(this);
        }

        protected void SetMoveDirection(Vector3 direction)
        {
            _moveDirection = direction;
            Hub.IsMoving.Value = direction.sqrMagnitude > 0.01f;
        }

        public void OnFixedUpdate()
        {
            var speed = Hub.Info?.Status?.MoveSpeed ?? MoveSpeed;
            Hub.Rigidbody.velocity = _moveDirection * speed;

            if (Hub.IsMoving.Value)
            {
                var targetRot = Quaternion.LookRotation(_moveDirection);
                Hub.FacingNode.rotation = Quaternion.Slerp(
                    Hub.FacingNode.rotation, targetRot, Time.fixedDeltaTime * RotateSpeed);
            }
        }

        public virtual void Stop()
        {
            SetMoveDirection(Vector3.zero);
            Global.Instance?.UnBindFixedUpdate(this);
        }

        protected virtual void OnDestroy()
        {
            Global.Instance?.UnBindFixedUpdate(this);
        }
    }
}
