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
            Hub.Rigidbody.velocity = _moveDirection * MoveSpeed;

            if (Hub.IsMoving.Value)
            {
                var targetRot = Quaternion.LookRotation(_moveDirection);
                Hub.Model.transform.rotation = Quaternion.Slerp(
                    Hub.Model.transform.rotation, targetRot, Time.fixedDeltaTime * RotateSpeed);
            }
        }

        protected virtual void OnDestroy()
        {
            Global.Instance?.UnBindFixedUpdate(this);
        }
    }
}
