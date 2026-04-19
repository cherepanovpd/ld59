// Path: Assets/_Project/Scripts/Tower/Tower.cs

using Core;

using DG.Tweening;
using Project.Effects;
using UnityEngine;

namespace Project.Tower
{
    /// <summary>
    /// Main controller for a tower entity.
    /// Manages the tower's visual blink and the associated signal ring expansion.
    /// </summary>
    [RequireComponent(typeof(TowerLightBlinker))]
    public class Tower : MonoBehaviour
    {
        [Header("Blink Configuration")]
        [SerializeField, Tooltip("Reference to the TowerLightBlinker component. Auto-fetched if not assigned.")]
        private TowerLightBlinker _lightBlinker;

        [Header("Signal Ring")]
        [SerializeField, Tooltip("Prefab of the ring that expands when the tower blinks.")]
        private TowerSignalRing _ringPrefab;

        [SerializeField, Tooltip("If true, a new ring instance is created for each blink. Otherwise, reuse a pooled instance.")]
        private bool _createNewRingEachBlink = true;

        [SerializeField, Tooltip("Maximum radius of the signal ring.")]
        [Min(0.1f)]
        private float _ringRadius = 8f;

        [SerializeField, Tooltip("Duration of the ring expansion.")]
        [Min(0.01f)]
        private float _ringExpandDuration = 1.2f;

        [SerializeField, Tooltip("Delay between blink start and ring expansion (seconds).")]
        [Min(0f)]
        private float _ringDelay = 0.1f;

        [Header("Ring Pooling (if not creating new each blink)")]
        [SerializeField, Tooltip("Maximum number of rings to keep pooled. Only used if _createNewRingEachBlink is false.")]
        [Min(1)]
        private int _ringPoolSize = 3;

        // Runtime state
        private TowerSignalRing _currentRing;
        private TowerSignalRing[] _ringPool;
        private int _poolIndex;

        /// <summary>
        /// Reference to the attached TowerLightBlinker.
        /// </summary>
        public TowerLightBlinker LightBlinker => _lightBlinker;

        /// <summary>
        /// Whether the tower is currently blinking (single blink in progress).
        /// </summary>
        public bool IsBlinking { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            InitializeRingPool();
        }

        private void Start()
        {
            // Optionally start continuous blinking automatically
            // _lightBlinker.StartContinuousBlinking();
        }

        private void OnDestroy()
        {
            CleanupRings();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Triggers a single tower blink and launches a signal ring.
        /// </summary>
        /// <param name="ringRadius">Optional custom radius for the ring. If zero, uses the serialized _ringRadius.</param>
        /// <param name="ringDuration">Optional custom expansion duration. If zero, uses the serialized _ringExpandDuration.</param>
        public void TriggerBlinkWithRing(float ringRadius = 0f, float ringDuration = 0f)
        {
            if (_lightBlinker == null)
            {
                Debug.LogWarning($"{nameof(Tower)}: No TowerLightBlinker assigned.", this);
                return;
            }
            
            G.Audio.PlaySFX("signal");

            IsBlinking = true;
            
            G.Camera.UpdateScreenShakeSetting(1.2f);

            // Trigger the visual blink
            _lightBlinker.TriggerSingleBlink();

            // Schedule ring expansion after a short delay
            float delay = _ringDelay;
            float radius = ringRadius > 0f ? ringRadius : _ringRadius;
            float duration = ringDuration > 0f ? ringDuration : _ringExpandDuration;

            DOVirtual.DelayedCall(delay, () =>
            {
                LaunchSignalRing(radius, duration);
                IsBlinking = false;
            });
        }

        /// <summary>
        /// Starts continuous blinking (if not already active) and optionally launches rings on each blink.
        /// This method does NOT automatically launch rings; you need to hook into blink events.
        /// </summary>
        public void StartContinuousBlinking()
        {
            if (_lightBlinker == null) return;

            _lightBlinker.StartContinuousBlinking();
        }

        /// <summary>
        /// Stops continuous blinking.
        /// </summary>
        public void StopContinuousBlinking()
        {
            if (_lightBlinker == null) return;

            _lightBlinker.StopContinuousBlinking();
        }

        /// <summary>
        /// Immediately stops any ongoing ring expansion and hides it.
        /// </summary>
        public void StopCurrentRing()
        {
            if (_currentRing != null)
            {
                _currentRing.StopExpansion(true);
                _currentRing = null;
            }
        }

        #endregion

        #region Private Helpers

        private void CacheComponents()
        {
            if (_lightBlinker == null)
            {
                _lightBlinker = GetComponent<TowerLightBlinker>();
            }

            if (_lightBlinker == null)
            {
                Debug.LogError($"{nameof(Tower)} requires a {nameof(TowerLightBlinker)} component.", this);
            }
        }

        private void InitializeRingPool()
        {
            if (_createNewRingEachBlink || _ringPrefab == null) return;

            _ringPool = new TowerSignalRing[_ringPoolSize];
            for (int i = 0; i < _ringPoolSize; i++)
            {
                TowerSignalRing ring = Instantiate(_ringPrefab, transform);
                ring.gameObject.SetActive(false);
                _ringPool[i] = ring;
            }
            _poolIndex = 0;
        }

        private void LaunchSignalRing(float radius, float duration)
        {
            if (_ringPrefab == null)
            {
                Debug.LogWarning($"{nameof(Tower)}: No ring prefab assigned.", this);
                return;
            }

            TowerSignalRing ring = GetAvailableRing();
            if (ring == null) return;

            ring.transform.position = transform.position;
            ring.gameObject.SetActive(true);
            ring.StartExpansion(transform.position, radius, duration);

            _currentRing = ring;
        }

        private TowerSignalRing GetAvailableRing()
        {
            if (_createNewRingEachBlink)
            {
                return Instantiate(_ringPrefab);
            }

            if (_ringPool == null || _ringPool.Length == 0)
            {
                Debug.LogError($"{nameof(Tower)}: Ring pool not initialized.", this);
                return null;
            }

            // Simple round‑robin pool
            TowerSignalRing ring = _ringPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _ringPool.Length;
            return ring;
        }

        private void CleanupRings()
        {
            if (_ringPool != null)
            {
                foreach (TowerSignalRing ring in _ringPool)
                {
                    if (ring != null)
                    {
                        Destroy(ring.gameObject);
                    }
                }
                _ringPool = null;
            }

            if (_currentRing != null && _currentRing.gameObject != null)
            {
                Destroy(_currentRing.gameObject);
                _currentRing = null;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_lightBlinker == null)
            {
                _lightBlinker = GetComponent<TowerLightBlinker>();
            }
        }
        
        [ContextMenu("Test Trigger Blink With Ring")]
        private void TestTriggerBlinkWithRing()
        {
            TriggerBlinkWithRing();
        }
#endif

        #endregion
    }
}