using InGame.Components;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Object
{
    public class ObjectHub
    {
        public ObjectState State { get; set; }
        public ObjectType Type { get; set; }
        public bool IsPlayer { get; set; }
        public GameObject Model { get; set; }
        public Rigidbody Rigidbody { get; set; }
        public Collider Collider { get; set; }
        public Collider MiningTrigger { get; set; }
        public AnimationPlayer AnimationPlayer { get; set; }
        public ReactiveProperty<bool> IsMoving { get; } = new(false);
    }
}
