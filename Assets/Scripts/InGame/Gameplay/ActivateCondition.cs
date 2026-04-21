using System;
using UnityEngine;

namespace InGame.Gameplay
{
    public enum ActivateConditionType
    {
        MoneyCarryCountReached,   // 플레이어가 들고 있는 돈 >= threshold
        MiningLevelReached,       // 채굴 레벨 >= threshold
        MiningItemCountReached,   // 현재 들고 있는 채굴 아이템 >= threshold
        HandCuffCountReached,     // 들고 있는 수갑 >= threshold
        MoneyReached,             // 총 돈(Money) >= threshold
        ZonePurchased,            // 특정 MoneySpendZone이 구매 완료되면
    }

    [Serializable]
    public class ActivateCondition
    {
        public ActivateConditionType type;
        [Tooltip("이 값 이상이 되면 조건 충족 (ZonePurchased에서는 미사용)")]
        public int threshold = 1;
        [Tooltip("ZonePurchased 전용: 구매 완료를 감지할 MoneySpendZone")]
        public MoneySpendZone targetZone;
    }
}
