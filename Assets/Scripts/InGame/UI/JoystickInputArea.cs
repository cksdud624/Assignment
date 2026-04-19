using Common;
using Common.Template.Interface;
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
        private float _lastInteractTime;
        private bool _isActive;

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
        }

        public void OnUpdate()
        {
            if (_isActive) return;
            if (inputStandby.activeSelf) return;
            if (Time.time - _lastInteractTime >= JoystickStandbyTime)
                inputStandby.SetActive(true);
        }

        #region Events

        private void OnPointerDownBackgroundImage(PointerEventData eventData)
        {
            _isActive = true;
            _lastInteractTime = Time.time;
            inputStandby.SetActive(false);
            joystickViewGroup.alpha = 1;
            joystickView.Activate(eventData.position, eventData.pressEventCamera);
        }

        private void OnDragBackgroundImage(PointerEventData eventData)
        {
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
