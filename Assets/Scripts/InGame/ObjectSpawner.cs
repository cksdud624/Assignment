using System;
using Common;
using InGame.Model;
using InGame.Object;
using UnityEngine;
using static Common.GameDefine;

namespace InGame
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private CharacterBase characterPrefab;
        [SerializeField] private ObjectBase objectPrefab;

        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            inGameModel.OnSpawnPlayer += OnSpawnPlayer;
            inGameModel.OnSpawnAI += OnSpawnAI;
        }

        #region Events
        private void OnSpawnPlayer(Vector3 position)
        {
            if (_inGameModel.InGameObjectModel.Player == null)
            {
                var player = Instantiate(characterPrefab, position, Quaternion.identity);
                player.Init(_inGameModel, ObjectType.Character, true);
            }
        }

        private void OnSpawnAI(Vector3 position, Quaternion rotation, Action<CharacterBase> onSpawned)
        {
            var ai = Instantiate(characterPrefab, position, rotation);
            ai.Init(_inGameModel, ObjectType.Character);
            onSpawned?.Invoke(ai);
        }
        #endregion

    }
}
