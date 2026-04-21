using System;
using UnityEngine;
using System.Collections.Generic;
using Common;
using InGame.Components;
using InGame.Model;
using UniRx;
using static Common.GameDefine;

namespace InGame.Object
{
    public class CharacterBase : ObjectBase
    {
        #region Object Management
        public new void Init(InGameModel model, ObjectType type ,bool isPlayer = false)
        {
            Hub = new();
            Hub.IsPlayer = isPlayer;
            Hub.Type = type;
            InGameModel = model;
            AddObject();
            AddParts();
            InGameModel.InGameObjectModel.IgnoreCollisionsWithCharacters(Hub.Collider);
            Hub.State.Value = ObjectState.Ready;
        }
        protected override void AddObject()
        {
            InGameModel.InGameObjectModel.AddCharacter(this, Hub.IsPlayer);
        }

        protected override void OnDestroy()
        {
            InGameModel.InGameObjectModel.RemoveCharacter(this, Hub.IsPlayer);
        }
        #endregion
        
        #region Components
        protected override void AddParts()
        {
            //추후 다른 데이터가 들어올 수 있으면 밖으로 빼야함
            var statusId = Hub.IsPlayer ? 1001L : 1002L;
            Hub.Info = new CharacterInfo
            {
                Status = Global.Instance.TableManager.CharacterStatusRecord.GetRecord(statusId)
            };

            var facingNode = new GameObject("FacingNode");
            facingNode.transform.SetParent(transform);
            facingNode.transform.localPosition = Vector3.zero;
            facingNode.transform.localRotation = Quaternion.identity;
            Hub.FacingNode = facingNode.transform;

            var assetModel = InGameModel.InGameAssetModel;
            var model = Instantiate(assetModel.GetModel(statusId.ToString()), Hub.FacingNode);
            Hub.Model = model;
            Hub.Rigidbody = gameObject.AddComponent<Rigidbody>();
            Hub.Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            Hub.Collider = model.GetComponent<Collider>();
   
            Dictionary<InGameCommonAnimation, AnimationClip> animationClips = new ();
            
            Hub.AnimationPlayer = gameObject.AddComponent<AnimationPlayer>();
            foreach (InGameCommonAnimation type in Enum.GetValues(typeof(InGameCommonAnimation)))
            {
                var key = $"default_{type}";
                var clip = assetModel.GetAnimationClip(key);
                if (clip == null)
                {
                    Debug.LogError($"Animation clip {key} not found");
                    continue;
                }
                animationClips.Add(type, clip);
            }

            Hub.AnimationPlayer.Init(model, animationClips, Hub);

            Hub.Behaviour = gameObject.AddComponent<CharacterBehaviour>();
            Hub.Behaviour.Init(Hub, InGameModel);

            var miningRange = gameObject.AddComponent<MiningRangeTrigger>();
            miningRange.Init(Hub, InGameModel);

            var stackView = gameObject.AddComponent<MiningItemStackView>();
            stackView.Init(Hub, InGameModel);

            var handCuffCarryView = gameObject.AddComponent<HandCuffCarryView>();
            handCuffCarryView.Init(Hub, InGameModel);

            var moneyCarryView = gameObject.AddComponent<MoneyCarryView>();
            moneyCarryView.Init(Hub, InGameModel);

            Hub.Controller = Hub.IsPlayer
                ? gameObject.AddComponent<ControllerPlayer>()
                : gameObject.AddComponent<ControllerAI>();
            Hub.Controller.Init(InGameModel, Hub);
        }
        #endregion

        public bool IsPlayer => Hub.IsPlayer;
        public ControllerBase Controller => Hub.Controller;
        public CharacterInfo Info => Hub.Info;
        public Collider Collider => Hub.Collider;
        public void SetCharacterState(CharacterState state) => Hub.CharacterState.Value = state;

        private new CharacterHub Hub
        {
            get => (CharacterHub)base.Hub;
            set => base.Hub = value;
        }
        
    }
}
