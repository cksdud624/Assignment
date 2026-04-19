using System;
using InGame.Model;
using UnityEngine;

namespace InGame
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        
        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            inGameModel.OnRequestAttachCamera += AttachCamera;
        }

        private void AttachCamera(Transform target)
        {
            transform.parent = target;
        }

    }
}
