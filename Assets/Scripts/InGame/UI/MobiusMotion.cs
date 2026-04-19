using Common;
using Common.Template.Interface;
using UnityEngine;

namespace InGame.UI
{
    public class MobiusMotion : MonoBehaviour, IUpdateable
    {
        [SerializeField] private float radius = 100f;
        [SerializeField] private float speed  = 1f;
        [SerializeField] private Vector2 offset= Vector2.zero;

        private RectTransform _rectTransform;
        private float _t;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Global.Instance.BindUpdate(this);
        }

        private void OnDestroy()
        {
            Global.Instance?.UnBindUpdate(this);
        }

        public void OnUpdate()
        {
            _t += speed * Time.deltaTime;

            float sinT  = Mathf.Sin(_t);
            float cosT  = Mathf.Cos(_t);
            float denom = 1f + sinT * sinT;

            _rectTransform.anchoredPosition = new Vector2(
                radius * cosT / denom + offset.x,
                radius * sinT * cosT / denom + offset.y
            );
        }
    }
}
