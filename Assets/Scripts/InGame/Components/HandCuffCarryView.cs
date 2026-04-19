using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public class HandCuffCarryView : MonoBehaviour
    {
        private GameObject _itemPrefab;
        private Transform _stackRoot;
        private readonly List<GameObject> _stackedItems = new();

        public void Init(CharacterHub hub, InGameModel inGameModel)
        {
            _itemPrefab = inGameModel.InGameAssetModel.GetView("handcuff_stack");

            var stackObject = new GameObject("HandCuffStacks");
            stackObject.transform.SetParent(hub.Model.transform);
            stackObject.transform.localPosition = Vector3.zero;
            stackObject.transform.localRotation = Quaternion.identity;
            _stackRoot = stackObject.transform;

            hub.Info.HandCuffCount
                .Subscribe(OnCountChanged)
                .AddTo(this);
        }

        private void OnCountChanged(int count)
        {
            while (_stackedItems.Count < count) SpawnItem();
            while (_stackedItems.Count > count) DespawnItem();
        }

        private void SpawnItem()
        {
            var index = _stackedItems.Count;
            var item = Instantiate(_itemPrefab, _stackRoot);
            item.transform.localPosition = new Vector3(0f, HandCuffCarryHeightOffset + HandCuffCarryStackHeight * index, HandCuffCarryFrontOffset);
            item.transform.localRotation = Quaternion.identity;
            _stackedItems.Add(item);
            TweenUtility.PopScaleAsync(item.transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void DespawnItem()
        {
            var last = _stackedItems[^1];
            _stackedItems.RemoveAt(_stackedItems.Count - 1);
            Destroy(last);
        }

        private void OnDestroy()
        {
            foreach (var item in _stackedItems)
                if (item != null) Destroy(item);
        }
    }
}
