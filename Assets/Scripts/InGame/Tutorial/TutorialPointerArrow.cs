using Common;
using Common.Template.Interface;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Tutorial
{
    public class TutorialPointerArrow : MonoBehaviour, IUpdateable
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private float radius = 120f;
        [SerializeField] private Vector2 centerOffset = Vector2.zero;

        private Transform _tutorialArrow;
        private Transform _player;
        private bool _isBound;
        private Canvas _canvas;
        private RectTransform _canvasRect;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
                _canvasRect = _canvas.GetComponent<RectTransform>();
        }

        public void Show(Transform tutorialArrow, Transform player)
        {
            _tutorialArrow = tutorialArrow;
            _player = player;
            gameObject.SetActive(true);
            OnUpdate();
            if (!_isBound)
            {
                Global.Instance.BindUpdate(this);
                _isBound = true;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _tutorialArrow = null;
            _player = null;
            if (_isBound)
            {
                Global.Instance.UnBindUpdate(this);
                _isBound = false;
            }
        }

        public void OnUpdate()
        {
            if (_tutorialArrow == null || _player == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            if (_canvas == null || _canvasRect == null) return;

            var canvasCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            var playerScreen = cam.WorldToScreenPoint(_player.position);
            var arrowScreen = cam.WorldToScreenPoint(_tutorialArrow.position);

            // 카메라 뒤쪽이면 방향 반전
            if (arrowScreen.z < 0f)
            {
                arrowScreen.x = Screen.width - arrowScreen.x;
                arrowScreen.y = Screen.height - arrowScreen.y;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, playerScreen, canvasCam, out var playerLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, arrowScreen, canvasCam, out var arrowLocal);

            var dir = arrowLocal - playerLocal;
            if (dir == Vector2.zero) return;
            dir.Normalize();

            var localPos = playerLocal + centerOffset + dir * radius;
            rectTransform.position = _canvasRect.TransformPoint(localPos);

            // 화살표 스프라이트가 위(+Y)를 향한다고 가정, -90도 보정
            rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        private void OnDestroy()
        {
            if (_isBound)
                Global.Instance?.UnBindUpdate(this);
        }
    }
}
