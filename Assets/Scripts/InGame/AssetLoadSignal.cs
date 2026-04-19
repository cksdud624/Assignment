using System;
using Common;
using Cysharp.Threading.Tasks;
using InGame.Model;
using UnityEngine;
using static Common.AssetKeys;
using static Common.GameDefine;

namespace InGame
{
    public class AssetLoadSignal : MonoBehaviour
    {
        private InGameModel _inGameModel;
        
        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            LoadInitAssetsAsync().Forget();
        }

        private async UniTask LoadInitAssetsAsync()
        {
            var assetManager = Global.Instance.AssetManager;
            var assetModel = _inGameModel.InGameAssetModel;

            var playerModel = await assetManager.LoadAssetAsync<GameObject>(LoadTarget.Model, "player");
            assetModel.AddModel("player", playerModel);

            var oreModel = await assetManager.LoadAssetAsync<GameObject>(LoadTarget.ObjectModel, "ore");
            assetModel.AddObjectModel("ore", oreModel);

            foreach (InGameCommonAnimation type in Enum.GetValues(typeof(InGameCommonAnimation)))
            {
                var key = $"default_{type}";
                var clip = await assetManager.LoadAssetAsync<AnimationClip>(LoadTarget.AnimationClip, key);
                if (clip == null) { Debug.LogWarning($"{key} is empty"); continue; }
                assetModel.AddAnimationClip(key, clip);
            }

            foreach (InGameObjectAnimation type in Enum.GetValues(typeof(InGameObjectAnimation)))
            {
                var key = $"default_{type}";
                var clip = await assetManager.LoadAssetAsync<AnimationClip>(LoadTarget.AnimationClip, key);
                if (clip == null) { Debug.LogWarning($"{key} is empty"); continue; }
                assetModel.AddAnimationClip(key, clip);
            }

            var oreStack = await assetManager.LoadAssetAsync<GameObject>(LoadTarget.View, "ore_stack");
            if (oreStack == null) Debug.LogWarning("ore_stack is empty");
            else assetModel.AddView("ore_stack", oreStack);

            var handcuffStack = await assetManager.LoadAssetAsync<GameObject>(LoadTarget.View, "handcuff_stack");
            if (handcuffStack == null) Debug.LogWarning("handcuff_stack is empty");
            else assetModel.AddView("handcuff_stack", handcuffStack);

            var equipmentRecords = Global.Instance.TableManager.MiningEquipmentsRecord.GetAllRecord();
            foreach (var record in equipmentRecords)
            {
                var key = record.Id.ToString();
                var equipment = await assetManager.LoadAssetAsync<GameObject>(LoadTarget.MiningEquipment, key);
                if (equipment == null) { Debug.LogWarning($"MiningEquipment {key} is empty"); continue; }
                assetModel.AddMiningEquipment(key, equipment);
            }

            _inGameModel.InvokeOnInitialized();
        }
        
        public void Dispose()
        {
            var assetManager = Global.Instance.AssetManager;
            var assetModel = _inGameModel.InGameAssetModel;
            foreach (var key in assetModel.GetModels().Keys)
            {
                assetModel.RemoveModel(key);
                assetManager.ReleaseAsset<GameObject>(LoadTarget.Model, key);
            }
            foreach (var key in assetModel.GetObjectModels().Keys)
            {
                assetModel.RemoveObjectModel(key);
                assetManager.ReleaseAsset<GameObject>(LoadTarget.ObjectModel, key);
            }
            foreach (var key in assetModel.GetMiningEquipments().Keys)
            {
                assetModel.RemoveMiningEquipment(key);
                assetManager.ReleaseAsset<GameObject>(LoadTarget.MiningEquipment, key);
            }
            foreach (var key in assetModel.GetViews().Keys)
            {
                assetModel.RemoveView(key);
                assetManager.ReleaseAsset<GameObject>(LoadTarget.View, key);
            }
            foreach (var key in assetModel.GetAnimationClips().Keys)
            {
                assetModel.RemoveAnimationClip(key);
                assetManager.ReleaseAsset<AnimationClip>(LoadTarget.AnimationClip, key);
            }
        }
    }
}
