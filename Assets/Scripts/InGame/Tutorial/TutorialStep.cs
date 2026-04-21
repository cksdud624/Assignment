using System;
using InGame.Gameplay;
using UnityEngine;

namespace InGame.Tutorial
{
    public enum TutorialConditionType
    {
        // player.Info.MiningItemCount >= conditionThreshold 될 때 진행
        MiningItemCountReached,

        // conditionTarget의 InteractZone에 플레이어가 진입할 때 진행
        PlayerEnteredZone,

        // conditionTarget의 MoneySpendZone이 활성화될 때 진행
        ZoneActivated,
    }

    [Serializable]
    public class TutorialStep
    {
        [Header("Arrow")]
        [Tooltip("화살표가 가리킬 Transform")]
        public Transform arrowTarget;

        [Tooltip("false = 오프스크린 포인터 자동 표시 비활성화 (ShowPointerAtItemCount로 수동 제어)")]
        public bool autoPointerEnabled = true;

        [Tooltip("MiningItemCount가 이 값 이상이 되면 포인터 강제 표시. 0 = 사용 안 함")]
        public int showPointerAtItemCount;

        [Header("Advance Condition")]
        public TutorialConditionType conditionType;

        [Tooltip("PlayerEnteredZone: InteractZone Transform / ZoneActivated: MoneySpendZone Transform")]
        public Transform conditionTarget;

        [Tooltip("MiningItemCountReached 전용 임계값")]
        public int conditionThreshold = 1;

        [Header("Pre-Step Actions (이 스텝 시작 전 실행)")]
        [Tooltip("이 스텝 시작 전 화살표를 잠시 숨김")]
        public bool hideArrowOnEnter;

        [Tooltip("이 스텝 시작 전 카메라로 해당 Transform을 reveal. 비어있으면 스킵")]
        public Transform cameraRevealTarget;
    }
}
