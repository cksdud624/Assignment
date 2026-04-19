using Common.Scene.Parameter;
using Generated.Table;
using InGame.Components;
using InGame.Gameplay;
using InGame.Model;
using InGame.UI;
using UnityEngine;

namespace InGame
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private ObjectSpawner objectSpawner;
        [SerializeField] private AssetLoadSignal assetLoadSignal;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private JoystickView joystickView;
        [SerializeField] private MiningZone miningZone;
        [SerializeField] private HandCuffMachineZone handCuffMachineZone;
        [SerializeField] private HandCuffSellZone handCuffSellZone;

        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            inGameModel.OnInitialized += OnInitialized;
            objectSpawner.Init(_inGameModel);
            cameraController.Init(_inGameModel);
            miningZone.Init(_inGameModel);
            handCuffMachineZone.Init(_inGameModel);
            handCuffSellZone.Init(_inGameModel);
            assetLoadSignal.Init(_inGameModel);
            //플레이어 스폰
        }
        
        #region Events

        private void OnInitialized()
        {
            _inGameModel.InvokeOnSpawnPlayer(Vector3.zero);
            var player = _inGameModel.InGameObjectModel.Player;
            if (player != null)
            {
                _inGameModel.InvokeOnRequestAttachCamera(player.transform);
                _inGameModel.InvokeOnPlayerChanged(player);
                if (player.Controller is ControllerPlayer cp)
                    cp.SetJoystick(joystickView);
            }
            miningZone.Place();
            _inGameModel.InGameObjectModel.ActivateAll();
        }
        #endregion

    }
}
