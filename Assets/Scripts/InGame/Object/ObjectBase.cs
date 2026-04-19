using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using InGame.Components;
using InGame.Model;
using UnityEngine;
using static Common.GameDefine;
using ObjectState = Common.GameDefine.ObjectState;

namespace InGame.Object
{
    public class ObjectBase : MonoBehaviour
    {
        #region Object Management

        protected InGameModel InGameModel { get; set; }

        public void Init(InGameModel model, ObjectType type ,bool isPlayer = false)
        {
            Hub = new ();
            Hub.IsPlayer = isPlayer;
            Hub.Type = type;
            InGameModel = model;
            AddObject();
            AddParts();
            Hub.State = ObjectState.Ready;
        }

        protected virtual void AddObject()
        {
            InGameModel.InGameObjectModel.AddObject(this);
        }

        protected virtual void OnDestroy()
        {
            InGameModel.InGameObjectModel.RemoveObject(this);
        }
        #endregion

        #region Components
        protected virtual void AddParts()
        {
            var assetModel = InGameModel.InGameAssetModel;
            var model = Instantiate(assetModel.GetObjectModel("ore"), transform);
            Hub.Model = model;

            var clips = new Dictionary<InGameObjectAnimation, AnimationClip>();
            foreach (InGameObjectAnimation type in Enum.GetValues(typeof(InGameObjectAnimation)))
            {
                var clip = assetModel.GetAnimationClip($"default_{type}");
                if (clip != null)
                    clips.Add(type, clip);
            }

            Hub.AnimationPlayer = gameObject.AddComponent<AnimationPlayer>();
            Hub.AnimationPlayer.Init(model, clips);

            Hub.Collider = model.GetComponent<Collider>();

            var miningTrigger = model.AddComponent<BoxCollider>();
            miningTrigger.isTrigger = true;
            if (Hub.Collider is BoxCollider physicsBox)
            {
                miningTrigger.center = physicsBox.center;
                miningTrigger.size = physicsBox.size;
            }
            else if (Hub.Collider != null)
            {
                var bounds = Hub.Collider.bounds;
                miningTrigger.center = model.transform.InverseTransformPoint(bounds.center);
                miningTrigger.size = bounds.size;
            }
            Hub.MiningTrigger = miningTrigger;
        }
        #endregion

        protected ObjectHub Hub { get; set; }
        public ObjectType Type => Hub.Type;
        public ObjectState State => Hub.State;
        public void SetState(ObjectState state) => Hub.State = state;

        public void Disappear()
        {
            Hub.State = ObjectState.Sleep;
            Hub.AnimationPlayer.PlayAnimation(InGameObjectAnimation.Disappear);
            RespawnAsync().Forget();
        }

        private async UniTaskVoid RespawnAsync()
        {
            if (Hub.Type != ObjectType.Mining) return;
            await UniTask.Delay(TimeSpan.FromSeconds(4f), cancellationToken: this.GetCancellationTokenOnDestroy());
            Hub.State = ObjectState.Playing;
            Hub.AnimationPlayer.PlayAnimation(InGameObjectAnimation.Appear);
        }
        public Collider Collider => Hub.Collider;
        public Collider MiningTrigger => Hub.MiningTrigger;
    }
}
