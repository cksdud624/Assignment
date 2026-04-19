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

        private readonly Subject<CharacterBase> _onPlayerInteracted = new();
        private readonly Subject<CharacterBase> _onPlayerExited = new();
        public IObservable<CharacterBase> OnPlayerInteracted => _onPlayerInteracted;
        public IObservable<CharacterBase> OnPlayerExited => _onPlayerExited;

        private void Awake()
        {
            var col = meshRenderer.gameObject.GetComponent<Collider>() ?? meshRenderer.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var childGameObject = meshRenderer.gameObject;
            col.OnTriggerEnterAsObservable().Subscribe(OnTriggerEntered).AddTo(childGameObject);
            col.OnTriggerExitAsObservable().Subscribe(OnTriggerExited).AddTo(childGameObject);

            ApplyMaterial(standbyMaterial);
        }

        private void OnTriggerEntered(Collider col)
        {
            var character = col.GetComponentInParent<CharacterBase>();
            if (character == null || !character.IsPlayer) return;
            ApplyMaterial(interactMaterial);
            _onPlayerInteracted.OnNext(character);
        }

        private void OnTriggerExited(Collider col)
        {
            var character = col.GetComponentInParent<CharacterBase>();
            if (character == null || !character.IsPlayer) return;
            ApplyMaterial(standbyMaterial);
            _onPlayerExited.OnNext(character);
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
        }
    }
}
