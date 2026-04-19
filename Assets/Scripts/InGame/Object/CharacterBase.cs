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
            Hub.State = ObjectState.Ready;
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
            Hub.Info = new CharacterInfo
            {
                Status = Global.Instance.TableManager.CharacterStatusRecord.GetRecord(1001)
            };

            var assetModel = InGameModel.InGameAssetModel;
            var model = Instantiate(assetModel.GetModel("player"), transform);
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
            miningRange.Init(Hub);

            var stackView = gameObject.AddComponent<MiningItemStackView>();
            stackView.Init(Hub, InGameModel);

            var handCuffCarryView = gameObject.AddComponent<HandCuffCarryView>();
            handCuffCarryView.Init(Hub, InGameModel);

            Hub.Controller = Hub.IsPlayer
                ? gameObject.AddComponent<ControllerPlayer>()
                : gameObject.AddComponent<ControllerAI>();
            Hub.Controller.Init(InGameModel, Hub);
        }
        #endregion

        public bool IsPlayer => Hub.IsPlayer;
        public ControllerBase Controller => Hub.Controller;
        public CharacterInfo Info => Hub.Info;
        public void SetCharacterState(CharacterState state) => Hub.CharacterState.Value = state;

        private new CharacterHub Hub
        {
            get => (CharacterHub)base.Hub;
            set => base.Hub = value;
        }
        
    }
}
