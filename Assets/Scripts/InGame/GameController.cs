using System.Collections.Generic;
using InGame.Components;
using InGame.Gameplay;
using InGame.Model;
using InGame.Tutorial;
using InGame.UI;
using UniRx;
using UnityEngine;

namespace InGame
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private ObjectSpawner objectSpawner;
        [SerializeField] private AssetLoadSignal assetLoadSignal;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private JoystickInputArea joystickInputArea;
        [SerializeField] private MiningZone miningZone;
        [SerializeField] private HandCuffMachineZone handCuffMachineZone;
        [SerializeField] private HandCuffSellZone handCuffSellZone;
        [SerializeField] private int prisonGoalCount = 20;

        [SerializeField] private List<MoneySpendZoneEntry> moneySpendZones;
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private MaxLabelController maxLabelController;
        [SerializeField] private MoneySpendZone prisonUnlockZone;
        [SerializeField] private PlayerMoneyView playerMoneyView;
        [SerializeField] private SoundPlayer soundPlayer;

        [SerializeField] private GameClearView gameClear;

        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            inGameModel.MaxLabelController = maxLabelController;
            inGameModel.PrisonGoalCount = prisonGoalCount;
            inGameModel.OnInitialized += OnInitialized;
            inGameModel.OnGameClear += OnGameClear;
            objectSpawner.Init(_inGameModel);
            joystickInputArea.Init(_inGameModel);
            cameraController.Init(_inGameModel);
            miningZone.Init(_inGameModel);
            handCuffMachineZone.Init(_inGameModel);
            prisonUnlockZone?.Init(_inGameModel);
            handCuffSellZone.Init(_inGameModel, prisonUnlockZone);
            playerMoneyView?.Init(_inGameModel);
            foreach (var entry in moneySpendZones)
                entry.zone.Init(_inGameModel);
            tutorialController.Init(_inGameModel);
            soundPlayer.Init(_inGameModel);
            gameClear.Init(_inGameModel);
            assetLoadSignal.Init(_inGameModel);
        }

        #region Events

        private void OnInitialized()
        {
            _inGameModel.SoundPlayer = soundPlayer;
            _inGameModel.InvokeOnSpawnPlayer(Vector3.zero);
            var player = _inGameModel.InGameObjectModel.Player;
            if (player != null)
            {
                _inGameModel.InvokeOnRequestAttachCamera(player.transform);
                _inGameModel.InvokeOnPlayerChanged(player);
                if (player.Controller is ControllerPlayer cp)
                    cp.SetJoystick(joystickInputArea.JoystickView);

                foreach (var entry in moneySpendZones)
                {
                    if (entry.zone.PurchaseAction == MoneySpendPurchaseAction.MiningLevelUp) continue;
                    var captured = entry;
                    captured.zone.OnPurchaseCompleted
                        .First()
                        .Subscribe(_ => OnPurchaseCompleted(captured))
                        .AddTo(this);
                }
            }
            miningZone.Place();
            _inGameModel.InGameObjectModel.ActivateAll();
        }

        private void OnPurchaseCompleted(MoneySpendZoneEntry entry)
        {
            switch (entry.zone.PurchaseAction)
            {
                case MoneySpendPurchaseAction.SpawnMinerAI:
                    SpawnMinerAI(entry.minerSpawnPoints);
                    break;
                case MoneySpendPurchaseAction.SpawnGenericAI:
                    SpawnGenericAI(entry.genericAISpawnPoint, entry.genericAISteps);
                    break;
            }
        }

        private void SpawnMinerAI(List<Transform> spawnPoints)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                var spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
                _inGameModel.InvokeOnSpawnAI(spawnPos, Quaternion.identity, ai =>
                {
                    _inGameModel.InGameObjectModel.ActivateAll();
                    ai.gameObject.AddComponent<MinerAIBrain>().Init(ai, miningZone, handCuffMachineZone);
                });
            }
        }

        private void SpawnGenericAI(Transform spawnPoint, List<AIBehaviourStep> steps)
        {
            var spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            _inGameModel.InvokeOnSpawnAI(spawnPos, Quaternion.identity, ai =>
            {
                _inGameModel.InGameObjectModel.ActivateAll();
                ai.gameObject.AddComponent<GenericAIBrain>().Init(ai, steps);
            });
        }

        private void OnGameClear()
        {
            _inGameModel.OnGameClear -= OnGameClear;
            _inGameModel.InvokeOnSetPlayerInputEnabled(false);

            foreach (var character in _inGameModel.InGameObjectModel.Characters)
            {
                character.Controller.Stop();
                character.GetComponent<MinerAIBrain>()?.Stop();
                character.GetComponent<GenericAIBrain>()?.Stop();
                character.GetComponent<MiningRangeTrigger>()?.Stop();
            }

            handCuffMachineZone.Stop();
            handCuffSellZone.Stop();
            prisonUnlockZone?.Stop();
            foreach (var entry in moneySpendZones)
                entry.zone.Stop();
            
            gameClear.gameObject.SetActive(true);
        }

        #endregion
    }
}
