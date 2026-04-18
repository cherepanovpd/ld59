// Path: Assets/_Project/Scripts/Audio/AudioManager.cs
using System.Collections.Generic;

using Core;

using Project.Core;

using UnityEngine;
using UnityEngine.Audio;

namespace Project.Audio
{
    /// <summary>
    /// Configuration for a set of audio clips with randomization parameters.
    /// </summary>
    [System.Serializable]
    public class AudioClipSet
    {
        public string key;
        public List<AudioClip> clips = new List<AudioClip>();
        [Range(0f, 1f)] public float volumeMin = 1f;
        [Range(0f, 1f)] public float volumeMax = 1f;
        [Range(0.1f, 3f)] public float pitchMin = 1f;
        [Range(0.1f, 3f)] public float pitchMax = 1f;
    }

    /// <summary>
    /// Manages sound effects and music playback with string keys, randomization, and pooling.
    /// Registers itself as G.Audio.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _masterGroup;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Header("Music Clips")]
        [SerializeField] private List<AudioClipSet> _musicSets = new List<AudioClipSet>();

        [Header("SFX Clips")]
        [SerializeField] private List<AudioClipSet> _sfxSets = new List<AudioClipSet>();

        [Header("Pooling")]
        [SerializeField] private int _initialPoolSize = 10;
        [SerializeField] private GameObject _audioSourcePrefab;

        [Header("Music Settings")]
        [SerializeField] private float _musicFadeDuration = 1.5f;

        // Dictionaries for fast lookup
        private Dictionary<string, AudioClipSet> _musicLookup;
        private Dictionary<string, AudioClipSet> _sfxLookup;

        // Object pool
        private Queue<AudioSource> _audioSourcePool;
        private List<AudioSource> _activeSources;

        // Music
        private AudioSource _musicSource;
        private AudioSource _secondaryMusicSource; // for crossfade
        private bool _isFading;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _masterVolume = 1f;

        // Cached references
        private Transform _cachedTransform;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.Audio != null && G.Audio != this)
            {
                Debug.LogWarning("[AudioManager] Multiple AudioManager instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.Audio = this;
            G.EnsureSystem("Audio", G.Audio);

            // Cache transform
            _cachedTransform = transform;

            // Build lookup dictionaries
            BuildLookup();

            // Create object pool
            InitializePool();

            // Create dedicated music sources
            CreateMusicSources();

            // Load saved volumes
            LoadVolumes();

            // Make persistent across scenes if needed
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // No automatic music start; let game logic decide
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.Audio == this)
                G.Audio = null;

            // Cleanup pool
            if (_audioSourcePool != null)
            {
                foreach (var source in _audioSourcePool)
                {
                    if (source != null)
                        Destroy(source.gameObject);
                }
                _audioSourcePool.Clear();
            }
        }

        // Zero allocations in Update
        private void Update()
        {
            // Update fading if active
            if (_isFading)
                UpdateFade();

            // Return finished SFX sources to pool
            for (int i = _activeSources.Count - 1; i >= 0; i--)
            {
                var source = _activeSources[i];
                if (!source.isPlaying)
                {
                    ReturnToPool(source);
                }
            }
        }

        #endregion

        #region Public API - Music

        /// <summary>
        /// Play music by key. If multiple clips exist, picks one randomly.
        /// </summary>
        public void PlayMusic(string key, bool fade = true)
        {
            if (!_musicLookup.TryGetValue(key, out var set) || set.clips.Count == 0)
            {
                Debug.LogWarning($"[AudioManager] Music key '{key}' not found or empty.");
                return;
            }

            AudioClip clip = set.clips[Random.Range(0, set.clips.Count)];
            if (clip == null)
                return;

            if (_musicSource.isPlaying && _musicSource.clip == clip)
                return; // Already playing

            if (fade && _musicSource.isPlaying)
            {
                StartCrossFade(clip);
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.Play();
            }
        }

        /// <summary>
        /// Stop currently playing music.
        /// </summary>
        public void StopMusic(bool fade = true)
        {
            if (fade)
                StartFadeOut();
            else
                _musicSource.Stop();
        }

        #endregion

        #region Public API - SFX

        /// <summary>
        /// Play a sound effect by key with randomized volume and pitch.
        /// </summary>
        public void PlaySFX(string key)
        {
            if (!_sfxLookup.TryGetValue(key, out var set) || set.clips.Count == 0)
            {
                Debug.LogWarning($"[AudioManager] SFX key '{key}' not found or empty.");
                return;
            }

            AudioClip clip = set.clips[Random.Range(0, set.clips.Count)];
            float volume = Random.Range(set.volumeMin, set.volumeMax) * _sfxVolume;
            float pitch = Random.Range(set.pitchMin, set.pitchMax);

            PlaySFXInternal(clip, volume, pitch);
        }

        /// <summary>
        /// Play a specific AudioClip with optional volume and pitch overrides.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;

            float volume = _sfxVolume * volumeScale;
            PlaySFXInternal(clip, volume, pitch);
        }

        #endregion

        #region Public API - Volume Control

        /// <summary>
        /// Set master volume (0-1) and save to PlayerPrefs.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyMixerVolume(_masterGroup, _masterVolume);
            PlayerPrefs.SetFloat(_masterGroup.name, _masterVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Set music volume (0-1) and save to PlayerPrefs.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyMixerVolume(_musicGroup, _musicVolume);
            PlayerPrefs.SetFloat(_musicGroup.name, _musicVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Set SFX volume (0-1) and save to PlayerPrefs.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyMixerVolume(_sfxGroup, _sfxVolume);
            PlayerPrefs.SetFloat(_sfxGroup.name, _sfxVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Get current master volume.
        /// </summary>
        public float GetMasterVolume() => _masterVolume;

        /// <summary>
        /// Get current music volume.
        /// </summary>
        public float GetMusicVolume() => _musicVolume;

        /// <summary>
        /// Get current SFX volume.
        /// </summary>
        public float GetSFXVolume() => _sfxVolume;

        #endregion

        #region Private Helpers

        private void BuildLookup()
        {
            _musicLookup = new Dictionary<string, AudioClipSet>();
            _sfxLookup = new Dictionary<string, AudioClipSet>();

            foreach (var set in _musicSets)
            {
                if (!string.IsNullOrEmpty(set.key))
                    _musicLookup[set.key] = set;
            }

            foreach (var set in _sfxSets)
            {
                if (!string.IsNullOrEmpty(set.key))
                    _sfxLookup[set.key] = set;
            }
        }

        private void InitializePool()
        {
            _audioSourcePool = new Queue<AudioSource>(_initialPoolSize);
            _activeSources = new List<AudioSource>(_initialPoolSize);

            if (_audioSourcePrefab == null)
            {
                // Create a simple GameObject with AudioSource
                _audioSourcePrefab = new GameObject("AudioSourcePrefab");
                _audioSourcePrefab.AddComponent<AudioSource>();
                _audioSourcePrefab.transform.SetParent(_cachedTransform);
                _audioSourcePrefab.SetActive(false);
            }

            for (int i = 0; i < _initialPoolSize; i++)
            {
                AudioSource source = CreateNewPooledSource();
                _audioSourcePool.Enqueue(source);
            }
        }

        private AudioSource CreateNewPooledSource()
        {
            GameObject go = Instantiate(_audioSourcePrefab, _cachedTransform);
            go.SetActive(false);
            AudioSource source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            return source;
        }

        private AudioSource GetPooledSource()
        {
            // Try to get from pool
            if (_audioSourcePool.Count > 0)
            {
                AudioSource source = _audioSourcePool.Dequeue();
                source.gameObject.SetActive(true);
                return source;
            }

            // Pool empty, create a new one (dynamic expansion)
            Debug.LogWarning("[AudioManager] AudioSource pool exhausted, creating new source.");
            return CreateNewPooledSource();
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            _audioSourcePool.Enqueue(source);
            _activeSources.Remove(source);
        }

        private void PlaySFXInternal(AudioClip clip, float volume, float pitch)
        {
            AudioSource source = GetPooledSource();
            if (source == null)
                return;

            source.clip = clip;
            source.outputAudioMixerGroup = _sfxGroup;
            source.volume = volume;
            source.pitch = pitch;
            source.loop = false;
            source.Play();

            _activeSources.Add(source);
        }

        private void CreateMusicSources()
        {
            GameObject musicGo = new GameObject("MusicSource");
            musicGo.transform.SetParent(_cachedTransform);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.outputAudioMixerGroup = _musicGroup;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = _musicVolume;

            GameObject secondaryGo = new GameObject("SecondaryMusicSource");
            secondaryGo.transform.SetParent(_cachedTransform);
            _secondaryMusicSource = secondaryGo.AddComponent<AudioSource>();
            _secondaryMusicSource.outputAudioMixerGroup = _musicGroup;
            _secondaryMusicSource.loop = true;
            _secondaryMusicSource.playOnAwake = false;
            _secondaryMusicSource.volume = 0f;
        }

        private void LoadVolumes()
        {
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

            ApplyMixerVolume(_masterGroup, _masterVolume);
            ApplyMixerVolume(_musicGroup, _musicVolume);
            ApplyMixerVolume(_sfxGroup, _sfxVolume);
        }

        private void ApplyMixerVolume(AudioMixerGroup group, float volume)
        {
            if (_audioMixer == null)
                return;

            // Convert linear 0-1 to dB (mixer uses logarithmic scale)
            float dB = volume > 0.0001f ? 20f * Mathf.Log10(volume) : -80f;
            _audioMixer.SetFloat(group.name, dB);
        }

        private void StartCrossFade(AudioClip newClip)
        {
            // Swap roles: secondary becomes primary, primary fades out
            (_secondaryMusicSource, _musicSource) = (_musicSource, _secondaryMusicSource);

            _musicSource.clip = newClip;
            _musicSource.Play();
            _musicSource.volume = 0f;
            _secondaryMusicSource.volume = _musicVolume;

            _isFading = true;
        }

        private void StartFadeOut()
        {
            _secondaryMusicSource.Stop();
            _secondaryMusicSource.volume = 0f;
            _musicSource.volume = _musicVolume;
            _isFading = true;
        }

        private void UpdateFade()
        {
            float delta = Time.unscaledDeltaTime / _musicFadeDuration;

            // Fade in primary
            if (_musicSource.volume < _musicVolume)
            {
                _musicSource.volume = Mathf.MoveTowards(_musicSource.volume, _musicVolume, delta);
            }

            // Fade out secondary
            if (_secondaryMusicSource.volume > 0f)
            {
                _secondaryMusicSource.volume = Mathf.MoveTowards(_secondaryMusicSource.volume, 0f, delta);
                if (_secondaryMusicSource.volume <= 0f)
                {
                    _secondaryMusicSource.Stop();
                }
            }

            // Check if fade complete
            if (Mathf.Approximately(_musicSource.volume, _musicVolume) &&
                Mathf.Approximately(_secondaryMusicSource.volume, 0f))
            {
                _isFading = false;
            }
        }

        #endregion
    }
}