using System;
using System.Collections.Generic;
using InGame.Gameplay;
using UnityEngine;

namespace InGame
{
    public enum MoneySpendPurchaseAction
    {
        MiningLevelUp,
        SpawnMinerAI,
        SpawnGenericAI,
        GameClear,
    }

    [Serializable]
    public class MoneySpendZoneEntry
    {
        public MoneySpendZone zone;

        [Tooltip("SpawnMinerAI 전용: 스폰 포인트 목록")]
        public List<Transform> minerSpawnPoints;

        [Tooltip("SpawnGenericAI 전용: 스폰 포인트")]
        public Transform genericAISpawnPoint;

        [Tooltip("SpawnGenericAI 전용: AI 행동 스텝")]
        public List<AIBehaviourStep> genericAISteps;
    }
}
