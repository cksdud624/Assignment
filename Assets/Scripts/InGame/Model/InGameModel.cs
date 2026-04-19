using System;
using Common.Scene.Parameter;
using InGame.Object;
using UnityEngine;

namespace InGame.Model
{
    public class InGameModel
    {
        public InGameModel(SceneParameterMain sceneParameterMain)
        {
            InGameObjectModel = new(sceneParameterMain);
            InGameAssetModel = new ();
        }

        #region Events
        public event Action<Vector3> OnSpawnPlayer;
        public void InvokeOnSpawnPlayer(Vector3 pos) => OnSpawnPlayer?.Invoke(pos);
        public event Action<Vector3, Quaternion, Action<CharacterBase>> OnSpawnAI;
        public void InvokeOnSpawnAI(Vector3 pos, Quaternion rot, Action<CharacterBase> onSpawned) => OnSpawnAI?.Invoke(pos, rot, onSpawned);
        public event Action OnInitialized;
        public void InvokeOnInitialized() => OnInitialized?.Invoke();
        public event Action<Transform> OnRequestAttachCamera;
        public void InvokeOnRequestAttachCamera(Transform target) => OnRequestAttachCamera?.Invoke(target);
        public event Action<CharacterBase> OnPlayerChanged;
        public void InvokeOnPlayerChanged(CharacterBase player) => OnPlayerChanged?.Invoke(player);
        #endregion
        
        #region Models
        public InGameObjectModel InGameObjectModel { get; private set; }
        public InGameAssetModel InGameAssetModel { get; private set; }
        #endregion

        public void Release()
        {
            OnSpawnPlayer = null;
            OnSpawnAI = null;
            OnInitialized = null;
            OnRequestAttachCamera = null;
            OnPlayerChanged = null;
        }
    }
}
