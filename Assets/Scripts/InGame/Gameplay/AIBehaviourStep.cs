using System;
using UnityEngine;

namespace InGame.Gameplay
{
    public enum AIStepAction
    {
        WaitForItems,
        PickUp,
        Deliver,
        Activate,
    }

    public enum AIItemType
    {
        None,
        HandCuff,
        MiningItem,
        Money,
    }

    [Serializable]
    public class AIBehaviourStep
    {
        public AIStepAction action;
        public AIItemType itemType;
        [Tooltip("WaitForItems: 이 수량 이상 쌓일 때까지 대기")]
        public int threshold = 1;
        [Tooltip("이동 목적지. WaitForItems는 이동 없이 조건 대기")]
        public Transform location;
        [Tooltip("인터페이스를 찾을 대상. 비어있으면 location에서 탐색")]
        public Transform actionTarget;
    }
}
