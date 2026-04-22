using System;
using InGame.Object;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace InGame.Gameplay
{
    public class InteractZone : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material standbyMaterial;
        [SerializeField] private Material interactMaterial;
        [SerializeField] private int maxCount;
        [SerializeField] private float maxLabelHeightOffset = 1.5f;
        [SerializeField] private MaxLabelController maxLabelController;

        public int MaxCount => maxCount;

        private readonly Subject<CharacterBase> _onPlayerInteracted = new();
        private readonly Subject<CharacterBase> _onPlayerExited = new();
        private readonly Subject<CharacterBase> _onAIInteracted = new();
        private readonly Subject<CharacterBase> _onAIExited = new();
        public IObservable<CharacterBase> OnPlayerInteracted => _onPlayerInteracted;
        public IObservable<CharacterBase> OnPlayerExited => _onPlayerExited;
        public IObservable<CharacterBase> OnAIInteracted => _onAIInteracted;
        public IObservable<CharacterBase> OnAIExited => _onAIExited;

        private MaxLabel _maxLabel;

        private void Awake()
        {
            var col = meshRenderer.gameObject.GetComponent<Collider>() ?? meshRenderer.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var childGameObject = meshRenderer.gameObject;
            col.OnTriggerEnterAsObservable().Subscribe(OnTriggerEntered).AddTo(childGameObject);
            col.OnTriggerExitAsObservable().Subscribe(OnTriggerExited).AddTo(childGameObject);

            ApplyMaterial(standbyMaterial);

            if (maxCount > 0 && maxLabelController != null)
                _maxLabel = maxLabelController.CreateLabel(transform, maxLabelHeightOffset);
        }

        public void SetMaxReached(bool isMax)
        {
            _maxLabel?.SetVisible(isMax);
        }

        private void OnTriggerEntered(Collider col)
        {
            var character = col.GetComponentInParent<CharacterBase>();
            if (character == null) return;
            if (character.IsPlayer)
            {
                ApplyMaterial(interactMaterial);
                _onPlayerInteracted.OnNext(character);
            }
            else
            {
                _onAIInteracted.OnNext(character);
            }
        }

        private void OnTriggerExited(Collider col)
        {
            var character = col.GetComponentInParent<CharacterBase>();
            if (character == null) return;
            if (character.IsPlayer)
            {
                ApplyMaterial(standbyMaterial);
                _onPlayerExited.OnNext(character);
            }
            else
            {
                _onAIExited.OnNext(character);
            }
        }

        public void ApplyInteractMaterial() => ApplyMaterial(interactMaterial);
        public void ApplyStandbyMaterial() => ApplyMaterial(standbyMaterial);

        public bool IsInteractMaterial()
        {
            if(meshRenderer != null)
                return meshRenderer.material == interactMaterial;
            return false;
        }

        private void ApplyMaterial(Material material)
        {
            if (meshRenderer != null && material != null)
                meshRenderer.material = material;
        }

        private void OnDestroy()
        {
            _onPlayerInteracted.Dispose();
            _onPlayerExited.Dispose();
            _onAIInteracted.Dispose();
            _onAIExited.Dispose();
        }
    }
}
