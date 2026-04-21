using System.Collections.Generic;
using UnityEngine;

namespace InGame.Model
{
    public class InGameAssetModel
    {
        private readonly Dictionary<string, GameObject> _modelAssets = new();
        public void AddModel(string key, GameObject model) => _modelAssets.Add(key, model);
        public void RemoveModel(string key) => _modelAssets.Remove(key);
        public GameObject GetModel(string key) => _modelAssets.GetValueOrDefault(key);
        public Dictionary<string, GameObject> GetModels() => _modelAssets;

        private readonly Dictionary<string, GameObject> _objectModelAssets = new();
        public void AddObjectModel(string key, GameObject model) => _objectModelAssets.Add(key, model);
        public void RemoveObjectModel(string key) => _objectModelAssets.Remove(key);
        public GameObject GetObjectModel(string key) => _objectModelAssets.GetValueOrDefault(key);
        public Dictionary<string, GameObject> GetObjectModels() => _objectModelAssets;

        private readonly Dictionary<string, GameObject> _miningEquipmentAssets = new();
        public void AddMiningEquipment(string key, GameObject prefab) => _miningEquipmentAssets.Add(key, prefab);
        public void RemoveMiningEquipment(string key) => _miningEquipmentAssets.Remove(key);
        public GameObject GetMiningEquipment(string key) => _miningEquipmentAssets.GetValueOrDefault(key);
        public Dictionary<string, GameObject> GetMiningEquipments() => _miningEquipmentAssets;

        private readonly Dictionary<string, GameObject> _viewAssets = new();
        public void AddView(string key, GameObject prefab) => _viewAssets.Add(key, prefab);
        public void RemoveView(string key) => _viewAssets.Remove(key);
        public GameObject GetView(string key) => _viewAssets.GetValueOrDefault(key);
        public Dictionary<string, GameObject> GetViews() => _viewAssets;

        private readonly Dictionary<string, AnimationClip> _animationClips = new();
        public void AddAnimationClip(string key, AnimationClip clip) => _animationClips.Add(key, clip);
        public void RemoveAnimationClip(string key) => _animationClips.Remove(key);
        public AnimationClip GetAnimationClip(string key) => _animationClips.GetValueOrDefault(key);
        public Dictionary<string, AnimationClip> GetAnimationClips() => _animationClips;
        
        private readonly Dictionary<string, AudioClip> _audioClips = new();
        public void AddSoundClip(string key, AudioClip clip) => _audioClips.Add(key, clip);
        public void RemoveSoundClip(string key) => _audioClips.Remove(key);
        public AudioClip GetAudioClip(string key) => _audioClips.GetValueOrDefault(key);
    }
}
