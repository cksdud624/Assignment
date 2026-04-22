using System.Collections.Generic;
using InGame.Model;
using UnityEngine;

namespace InGame.Components
{
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField] private int poolSize = 16;

        private InGameModel _inGameModel;
        private readonly List<AudioSource> _pool = new();
        private int _poolIndex;

        public void Init(InGameModel inGameModel)
        {
            _inGameModel = inGameModel;

            for (var i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"SoundSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.pitch = 1f;
                src.volume = 1f;
                _pool.Add(src);
            }
        }

        public void PlayOnce(AudioClip clip, Vector3 position, float pitch = 1f, float volume = 1f)
        {
            if (clip == null) return;

            var src = GetNextSource();
            src.transform.position = position;
            src.clip = clip;
            src.pitch = pitch;
            src.volume = volume;
            src.Play();
        }

        private AudioSource GetNextSource()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                var idx = (_poolIndex + i) % _pool.Count;
                if (!_pool[idx].isPlaying)
                {
                    _poolIndex = (idx + 1) % _pool.Count;
                    return _pool[idx];
                }
            }

            // all busy — evict oldest (round-robin)
            var evict = _pool[_poolIndex];
            evict.Stop();
            evict.pitch = 1f;
            evict.volume = 1f;
            _poolIndex = (_poolIndex + 1) % _pool.Count;
            return evict;
        }
    }
}
