using Common;
using Common.Template.Interface;
using InGame.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Common.GameDefine;

namespace InGame.UI
{
    public class JoystickInputArea : MonoBehaviour, IUpdateable
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup joystickViewGroup;
        [SerializeField] private JoystickView joystickView;
        [SerializeField] private GameObject inputStandby;

        public JoystickView JoystickView => joystickView;

        private float _lastInteractTime;
        private bool _isActive;
        private bool _standbyEnabled = true;
        private bool _playerInputEnabled = true;
        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            inGameModel.OnSetStandbyEnabled += SetStandbyEnabled;
            inGameModel.OnSetPlayerInputEnabled += SetPlayerInputEnabled;
        }

        private void Start()
        {
            backgroundImage.OnPointerDownAsObservable()
                .Subscribe(OnPointerDownBackgroundImage)
                .AddTo(this);

            backgroundImage.OnDragAsObservable()
                .Subscribe(OnDragBackgroundImage)
                .AddTo(this);

            backgroundImage.OnPointerUpAsObservable()
                .Subscribe(OnPointerUpBackgroundImage)
                .AddTo(this);

            joystickViewGroup.alpha = 0;
            inputStandby.SetActive(true);

            _lastInteractTime = Time.time;
            Global.Instance.BindUpdate(this);
        }

        private void OnDestroy()
        {
            Global.Instance?.UnBindUpdate(this);
            if (_inGameModel != null)
            {
                _inGameModel.OnSetStandbyEnabled -= SetStandbyEnabled;
                _inGameModel.OnSetPlayerInputEnabled -= SetPlayerInputEnabled;
            }
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            _playerInputEnabled = enabled;
            backgroundImage.raycastTarget = enabled;
            if (!enabled)
            {
                joystickViewGroup.alpha = 0;
                _isActive = false;
                joystickView.Deactivate();
            }
        }

        public void SetStandbyEnabled(bool enabled)
        {
            _standbyEnabled = enabled;
            if (!enabled && inputStandby.activeSelf)
                inputStandby.SetActive(false);
        }

        public void OnUpdate()
        {
            if (!_standbyEnabled) return;
            if (_isActive) return;
            if (inputStandby.activeSelf) return;
            if (Time.time - _lastInteractTime >= JoystickStandbyTime)
                inputStandby.SetActive(true);
        }

        #region Events

        private void OnPointerDownBackgroundImage(PointerEventData eventData)
        {
            if (!_playerInputEnabled) return;
            _isActive = true;
            _lastInteractTime = Time.time;
            inputStandby.SetActive(false);
            joystickViewGroup.alpha = 1;
            joystickView.Activate(eventData.position, eventData.pressEventCamera);
        }

        private void OnDragBackgroundImage(PointerEventData eventData)
        {
            if (!_playerInputEnabled) return;
            _lastInteractTime = Time.time;
            joystickView.UpdateHandle(eventData.position, eventData.pressEventCamera);
        }

        private void OnPointerUpBackgroundImage(PointerEventData eventData)
        {
            _isActive = false;
            _lastInteractTime = Time.time;
            joystickView.Deactivate();
            joystickViewGroup.alpha = 0;
        }

        #endregion
    }
}
