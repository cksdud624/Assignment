using System;
using System.Collections.Generic;
using Common;
using InGame.Model;
using InGame.Object;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Gameplay
{
    public class MiningZone : MonoBehaviour
    {
        [SerializeField] private ObjectBase prefab;
        [SerializeField] private int countX = 1;
        [SerializeField] private int countZ = 1;
        [SerializeField] private float areaX = 1f;
        [SerializeField] private float areaZ = 1f;
        [SerializeField] [Range(0f, 1f)] private float scaleFactor = 1f;

        private readonly List<ObjectBase> _spawnedObjects = new();
        private BoxCollider _collider;
        private float _scaledY;

        private readonly Subject<ObjectBase> _onClosestObjectFound = new();
        public IObservable<ObjectBase> OnClosestObjectFound => _onClosestObjectFound;

        private CharacterBase _player;
        private InGameModel _inGameModel;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;
            _inGameModel.OnPlayerChanged += OnSetPlayer;

            _collider = gameObject.GetComponent<BoxCollider>();
            if (_collider == null)
                _collider = gameObject.AddComponent<BoxCollider>();

            _collider.OnTriggerEnterAsObservable().Subscribe(OnTriggerEntered).AddTo(this);
            _collider.OnTriggerExitAsObservable().Subscribe(OnTriggerExited).AddTo(this);
        }

        public void Place()
        {
            if (prefab == null)
            {
                Debug.LogError($"[MiningZone] prefab is null", this);
                return;
            }

            var intervalX = countX > 1 ? areaX / (countX - 1) : areaX;
            var intervalZ = countZ > 1 ? areaZ / (countZ - 1) : areaZ;
            var objectScale = Mathf.Min(intervalX, intervalZ) * scaleFactor;

            var prefabScale = prefab.transform.localScale;
            _scaledY = prefabScale.y * objectScale / prefabScale.x;

            _collider.isTrigger = true;
            _collider.size = new Vector3(areaX, _scaledY, areaZ);
            _collider.center = new Vector3(0f, _scaledY * 0.5f, 0f);

            for (var z = 0; z < countZ; z++)
            {
                for (var x = 0; x < countX; x++)
                {
                    var localPos = new Vector3(
                        x * intervalX - areaX * 0.5f,
                        _scaledY,
                        z * intervalZ - areaZ * 0.5f);

                    var obj = Instantiate(prefab, transform.TransformPoint(localPos), Quaternion.identity, transform);
                    obj.transform.localScale = new Vector3(objectScale, _scaledY, objectScale);
                    _spawnedObjects.Add(obj);
                    obj.Init(_inGameModel, ObjectType.Mining);
                }
            }
        }

        #region Events

        private void OnSetPlayer(CharacterBase player) => _player = player;

        private void OnTriggerEntered(Collider col)
        {
            if (_player == null || col != _player.Collider) return;
            _player.SetCharacterState(CharacterState.Mining);
        }

        private void OnTriggerExited(Collider col)
        {
            if (_player == null || col != _player.Collider) return;
            _player.SetCharacterState(CharacterState.Idle);
        }

        private void OnDestroy()
        {
            if (_inGameModel != null)
                _inGameModel.OnPlayerChanged -= OnSetPlayer;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var intervalX = countX > 1 ? areaX / (countX - 1) : 0f;
            var intervalZ = countZ > 1 ? areaZ / (countZ - 1) : 0f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(areaX, 0.1f, areaZ));

            for (var z = 0; z < countZ; z++)
            {
                for (var x = 0; x < countX; x++)
                {
                    var localPos = new Vector3(
                        x * intervalX - areaX * 0.5f,
                        0f,
                        z * intervalZ - areaZ * 0.5f);

                    Gizmos.DrawWireSphere(transform.TransformPoint(localPos), 0.3f);
                }
            }
        }
#endif
    }
}
