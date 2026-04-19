using System.Collections.Generic;
using Common;
using Common.Template.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Components
{
    public class StackView : MonoBehaviour, IUpdateable
    {
        public struct Config
        {
            public int Columns;
            public float StackHeight;
            public float HeightOffset;
            public float ColumnOffset;
            public float WobbleDuration;
            public float WobbleFrequency;
            public float WobbleAmplitude;
        }

        private struct StackItem
        {
            public GameObject GameObject;
            public float WobbleElapsed;
        }

        private Config _config;
        private readonly List<StackItem> _stackedItems = new();

        public void Init(Config config)
        {
            _config = config;
            Global.Instance.BindUpdate(this);
        }

        public void AddItem(GameObject item)
        {
            item.transform.SetParent(transform);
            item.transform.localPosition = GetStackPosition(_stackedItems.Count);
            item.transform.localRotation = Quaternion.identity;
            _stackedItems.Add(new StackItem { GameObject = item, WobbleElapsed = 0f });

            TweenUtility.PopScaleAsync(item.transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void RemoveItem()
        {
            if (_stackedItems.Count == 0) return;
            var item = _stackedItems[^1].GameObject;
            _stackedItems.RemoveAt(_stackedItems.Count - 1);
            RemoveItemAsync(item).Forget();
        }

        private async UniTaskVoid RemoveItemAsync(GameObject item)
        {
            await TweenUtility.ShrinkScaleAsync(item.transform, this.GetCancellationTokenOnDestroy());
            if (item != null) Destroy(item);
        }

        public Vector3 GetNextWorldPosition() =>
            transform.TransformPoint(GetStackPosition(_stackedItems.Count));

        public void OnUpdate()
        {
            for (var i = 0; i < _stackedItems.Count; i++)
            {
                var entry = _stackedItems[i];
                if (entry.GameObject == null || entry.WobbleElapsed >= _config.WobbleDuration) continue;

                entry.WobbleElapsed += Time.deltaTime;
                var decay = 1f - Mathf.Clamp01(entry.WobbleElapsed / _config.WobbleDuration);
                var wobble = Mathf.Sin(entry.WobbleElapsed * _config.WobbleFrequency * Mathf.PI * 2f) * _config.WobbleAmplitude * decay;

                entry.GameObject.transform.localRotation = entry.WobbleElapsed >= _config.WobbleDuration
                    ? Quaternion.identity
                    : Quaternion.Euler(wobble, 0f, wobble * 0.5f);

                _stackedItems[i] = entry;
            }
        }

        private Vector3 GetStackPosition(int index)
        {
            if (_config.Columns <= 1)
                return new Vector3(0f, _config.HeightOffset + _config.StackHeight * index, 0f);

            var column = index % _config.Columns;
            var row = index / _config.Columns;
            var x = column == 0 ? -_config.ColumnOffset : _config.ColumnOffset;
            return new Vector3(x, _config.HeightOffset + _config.StackHeight * row, 0f);
        }

        private void OnDestroy()
        {
            Global.Instance?.UnBindUpdate(this);
            foreach (var entry in _stackedItems)
                if (entry.GameObject != null) Destroy(entry.GameObject);
        }
    }
}
