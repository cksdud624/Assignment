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
            public int Rows;        // 0이면 무한 — 기존 동작 유지
            public float StackHeight;
            public float HeightOffset;
            public float ColumnOffset;
            public float RowOffset;
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
        private int _pendingCount;

        public int Count => _stackedItems.Count;

        public void Init(Config config)
        {
            _config = config;
            Global.Instance.BindUpdate(this);
        }

        public void AddItem(GameObject item)
        {
            if (_pendingCount > 0) _pendingCount--;
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

        // 스택에서 아이템을 꺼내 반환 (Destroy 하지 않음)
        public GameObject TakeItem()
        {
            if (_stackedItems.Count == 0) return null;
            var item = _stackedItems[^1].GameObject;
            _stackedItems.RemoveAt(_stackedItems.Count - 1);
            item.transform.SetParent(null);
            return item;
        }

        private async UniTaskVoid RemoveItemAsync(GameObject item)
        {
            await TweenUtility.ShrinkScaleAsync(item.transform, this.GetCancellationTokenOnDestroy());
            if (item != null) Destroy(item);
        }

        public Vector3 ReserveNextWorldPosition()
        {
            var pos = transform.TransformPoint(GetStackPosition(_stackedItems.Count + _pendingCount));
            _pendingCount++;
            return pos;
        }

        public Vector3 GetNextWorldPosition() =>
            transform.TransformPoint(GetStackPosition(_stackedItems.Count + _pendingCount));

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
            var cols = Mathf.Max(1, _config.Columns);

            // Rows <= 0: 기존 동작 (열 채우고 Y축으로 계속 올라감)
            if (_config.Rows <= 0)
            {
                var col = index % cols;
                var row = index / cols;
                var x = cols == 1 ? 0f : col == 0 ? -_config.ColumnOffset : _config.ColumnOffset;
                return new Vector3(x, _config.HeightOffset + _config.StackHeight * row, 0f);
            }

            // Rows > 0: Columns×Rows 격자를 한 층으로, 채우면 Y로 올라감
            var rows = _config.Rows;
            var itemsPerLayer = cols * rows;
            var layer = index / itemsPerLayer;
            var indexInLayer = index % itemsPerLayer;
            var c = indexInLayer % cols;
            var r = indexInLayer / cols;

            var xGrid = cols == 1 ? 0f : c == 0 ? -_config.ColumnOffset : _config.ColumnOffset;
            var zGrid = rows == 1 ? 0f : (r - (rows - 1) * 0.5f) * _config.RowOffset;
            var yGrid = _config.HeightOffset + _config.StackHeight * layer;

            return new Vector3(xGrid, yGrid, zGrid);
        }

        private void OnDestroy()
        {
            Global.Instance?.UnBindUpdate(this);
            foreach (var entry in _stackedItems)
                if (entry.GameObject != null) Destroy(entry.GameObject);
        }
    }
}
