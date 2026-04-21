using System.Collections.Generic;
using Common;
using InGame.Model;
using InGame.Object;
using UniRx;
using UnityEngine;
using static Common.GameDefine;

namespace InGame.Components
{
    public class CharacterBehaviour : MonoBehaviour
    {
        private CharacterHub _hub;
        private InGameModel _inGameModel;
        private readonly List<GameObject> _attachedEquipments = new();
        private Transform _modelOriginalParent;
        private bool _isBoarding;

        public void Init(CharacterHub hub, InGameModel inGameModel)
        {
            _hub = hub;
            _inGameModel = inGameModel;
            _modelOriginalParent = hub.Model.transform.parent;
            hub.CharacterState.Subscribe(OnStateChanged).AddTo(this);
            hub.Info.HandCuffCount
                .Select(c => c >= 1)
                .DistinctUntilChanged()
                .Subscribe(OnHandCuffHoldingChanged).AddTo(this);
        }

        private void OnHandCuffHoldingChanged(bool hasHandCuff)
        {
            if (_hub.CharacterState.Value == CharacterState.Mining) return;
            if (hasHandCuff)
                _hub.AnimationPlayer.PlayUpperBodyAnimation(InGameCommonAnimation.Holding);
            else
                _hub.AnimationPlayer.StopUpperBodyAnimation();
        }

        private void OnStateChanged(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.Idle:
                    if (_hub.Info.HandCuffCount.Value >= 1)
                        _hub.AnimationPlayer.PlayUpperBodyAnimation(InGameCommonAnimation.Holding);
                    else
                        _hub.AnimationPlayer.StopUpperBodyAnimation();
                    ClearEquipments();
                    break;
                case CharacterState.Mining:
                    ApplyMiningEquipment();
                    break;
            }
        }

        private void ApplyMiningEquipment()
        {
            ClearEquipments();
            var level = _hub.Info.MiningLevel.Value;
            var record = Global.Instance.TableManager.MiningEquipmentsRecord.GetRecordByLevel(level);
            if (record == null)
            {
                Debug.LogWarning($"[CharacterBehaviour] No equipment record for level {level}");
                return;
            }

            var prefab = _inGameModel.InGameAssetModel.GetMiningEquipment(record.Id.ToString());
            if (prefab == null)
            {
                Debug.LogWarning($"[CharacterBehaviour] Equipment prefab {record.Id} not found");
                return;
            }

            switch (level)
            {
                case 1:
                    AttachToHand(prefab, HumanBodyBones.RightHand);
                    break;
                case 2:
                    AttachToFront(prefab);
                    break;
                case 3:
                    BoardVehicle(prefab);
                    break;
            }
        }

        private void AttachToHand(GameObject prefab, HumanBodyBones bone)
        {
            var boneTr = _hub.AnimationPlayer.GetBoneTransform(bone);
            if (boneTr == null) return;
            var obj = Instantiate(prefab, boneTr);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            _attachedEquipments.Add(obj);
        }

        private void AttachToFront(GameObject prefab, float forwardOffset = 0.5f)
        {
            var pivot = new GameObject("FrontAttachmentPivot");
            pivot.transform.SetParent(_hub.FacingNode);
            pivot.transform.localPosition = new Vector3(0f, 0f, forwardOffset);
            pivot.transform.localRotation = Quaternion.identity;

            var instance = Instantiate(prefab, pivot.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            _attachedEquipments.Add(pivot);
        }

        private void BoardVehicle(GameObject prefab, float forwardOffset = 0.5f)
        {
            var pivot = new GameObject("FrontAttachmentPivot");
            pivot.transform.SetParent(_hub.FacingNode);
            pivot.transform.localPosition = new Vector3(0f, 0f, forwardOffset);
            pivot.transform.localRotation = Quaternion.identity;

            var instance = Instantiate(prefab, pivot.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            _attachedEquipments.Add(pivot);
        }

        private void ClearEquipments()
        {
            _hub.Model.GetComponent<IKHandTarget>()?.Clear();

            if (_isBoarding)
            {
                _hub.Model.transform.SetParent(_modelOriginalParent);
                _hub.Model.transform.localPosition = Vector3.zero;
                _hub.Model.transform.localRotation = Quaternion.identity;
                _isBoarding = false;
            }
            foreach (var obj in _attachedEquipments)
                if (obj != null) Destroy(obj);
            _attachedEquipments.Clear();
        }
    }
}
