using Common;
using Common.Template.Interface;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Tutorial
{
    public class TutorialArrow : MonoBehaviour, IUpdateable
    {
        [SerializeField] private TutorialPointerArrow pointerArrow;

        private Transform _target;
        private Transform _player;
        private float _bobTime;
        private bool _isBound;
        private bool _pointerVisible;
        private bool _autoPointerEnabled = true;
        private bool _pointerUnlocked;

        public void SetPlayer(Transform player) => _player = player;

        public void SetAutoPointerEnabled(bool enabled)
        {
            _autoPointerEnabled = enabled;
            if (!enabled && _pointerVisible)
            {
                pointerArrow.Hide();
                _pointerVisible = false;
            }
        }

        public void ShowPointer()
        {
            if (_player == null) return;
            _pointerUnlocked = true;
            if (_pointerVisible) return;
            pointerArrow.Show(_target, _player);
            _pointerVisible = true;
        }

        public void Show(Transform target)
        {
            _target = target;
            _bobTime = 0f;
            _pointerVisible = false;
            _autoPointerEnabled = true;
            _pointerUnlocked = false;
            pointerArrow.Hide();
            gameObject.SetActive(true);
            if (!_isBound)
            {
                Global.Instance.BindUpdate(this);
                _isBound = true;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _target = null;
            _pointerVisible = false;
            _autoPointerEnabled = true;
            _pointerUnlocked = false;
            pointerArrow.Hide();
            if (_isBound)
            {
                Global.Instance.UnBindUpdate(this);
                _isBound = false;
            }
        }

        public void OnUpdate()
        {
            if (_target == null) return;
            UpdateBob();
            UpdateOffScreenPointer();
        }

        private void UpdateBob()
        {
            var bob = Mathf.Sin(_bobTime * TutorialArrowBobSpeed) * TutorialArrowBobAmount;
            transform.position = _target.position + Vector3.up * (TutorialArrowHeightOffset + bob);
            _bobTime += Time.deltaTime;
        }

        private void UpdateOffScreenPointer()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var vp = cam.WorldToViewportPoint(_target.position);
            var isOnScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            if (isOnScreen)
            {
                if (_pointerVisible)
                {
                    pointerArrow.Hide();
                    _pointerVisible = false;
                }
                return;
            }

            if (!_autoPointerEnabled && !_pointerUnlocked) return;

            if (!_pointerVisible && _player != null)
            {
                pointerArrow.Show(_target, _player);
                _pointerVisible = true;
            }
        }

        private void OnDestroy()
        {
            if (_isBound)
                Global.Instance?.UnBindUpdate(this);
        }
    }
}
