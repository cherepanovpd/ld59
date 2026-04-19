// Path: Assets/_Project/Scripts/Human/HumanManager.cs

using System.Collections.Generic;

using Common.Runtime.Extensions;

using Core;
using Project.Human.Config;
using UnityEngine;

using Utilities;

namespace Project.Human
{
    /// <summary>
    /// Central management system for all human characters.
    /// Registers with 'G' service locator and provides batch operations.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HumanConfig _config;
        
        [Header("Performance Settings")]
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private int _maxPoolSize = 100;
        [SerializeField] private bool _preloadOnAwake = true;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        [SerializeField] private bool _logSpawnEvents = false;
        
        // Active humans tracking
        private readonly List<HumanCharacter> _activeHumans = new List<HumanCharacter>();
        private readonly Dictionary<House, List<HumanCharacter>> _humansByHouse = new Dictionary<House, List<HumanCharacter>>();
        
        // Cached references for performance
        private Transform _cachedTransform;
        
        /// <summary>
        /// Total number of active human characters.
        /// </summary>
        public int ActiveHumanCount => _activeHumans.Count;
        
        /// <summary>
        /// Configuration used by this manager.
        /// </summary>
        public HumanConfig Config => _config;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _cachedTransform = transform;
            
            // Register with global service locator
            G.HumanManager = this;
            
            // Ensure we have a config
            if (_config == null)
                _config = G.HumanConfig;
            
            if (_showDebugInfo)
                Debug.Log($"[HumanManager] Initialized with config: {(_config != null ? _config.name : "null")}");
        }
        
        private void OnDestroy()
        {
            // Unregister from global service locator
            if (G.HumanManager == this)
                G.HumanManager = null;
            
            // Clean up all humans
            CleanupAllHumans();
            
            if (_showDebugInfo)
                Debug.Log("[HumanManager] Destroyed and cleaned up all humans.");
        }
        
        private void Update()
        {
            // Batch update all active humans
            UpdateAllHumans(Time.deltaTime);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Register a human character with this manager.
        /// Called automatically by HumanCharacter.Awake().
        /// </summary>
        public void RegisterHuman(HumanCharacter human)
        {
            if (human == null)
                return;
            
            if (!_activeHumans.Contains(human))
            {
                _activeHumans.Add(human);
                
                // Track by house
                House house = human.HomeHouse;
                if (house != null)
                {
                    if (!_humansByHouse.ContainsKey(house))
                        _humansByHouse[house] = new List<HumanCharacter>();
                    
                    _humansByHouse[house].Add(human);
                }
                
                if (_logSpawnEvents)
                    Debug.Log($"[HumanManager] Registered human: {human.name}");
            }
        }
        
        /// <summary>
        /// Unregister a human character from this manager.
        /// Called automatically by HumanCharacter.OnDestroy().
        /// </summary>
        public void UnregisterHuman(HumanCharacter human)
        {
            if (human == null)
                return;
            
            _activeHumans.Remove(human);
            
            // Remove from house tracking
            House house = human.HomeHouse;
            if (house != null && _humansByHouse.ContainsKey(house))
            {
                _humansByHouse[house].Remove(human);
                
                // Clean up empty house lists
                if (_humansByHouse[house].Count == 0)
                    _humansByHouse.Remove(house);
            }
            
            if (_logSpawnEvents)
                Debug.Log($"[HumanManager] Unregistered human: {human.name}");
        }
        
        /// <summary>
        /// Get all humans belonging to a specific house.
        /// </summary>
        public List<HumanCharacter> GetHumansForHouse(House house)
        {
            if (house == null || !_humansByHouse.ContainsKey(house))
                return new List<HumanCharacter>();
            
            return _humansByHouse[house];
        }
        
        /// <summary>
        /// Get the number of humans currently assigned to a house.
        /// </summary>
        public int GetHumanCountForHouse(House house)
        {
            if (house == null || !_humansByHouse.ContainsKey(house))
                return 0;
            
            return _humansByHouse[house].Count;
        }
        
        /// <summary>
        /// Check if a house has reached its maximum human capacity.
        /// </summary>
        public bool IsHouseFull(House house)
        {
            if (house == null)
                return false;
            
            int currentCount = GetHumanCountForHouse(house);
            return currentCount >= house.MaxResidents;
        }
        
        /// <summary>
        /// Spawn a new human character near a house.
        /// Uses object pooling for performance.
        /// </summary>
        public HumanCharacter SpawnHuman(House house, HumanData data = null)
        {
            if (house == null)
            {
                Debug.LogWarning("[HumanManager] Cannot spawn human: house is null.");
                return null;
            }
            
            // Check if house is full
            if (IsHouseFull(house))
            {
                if (_logSpawnEvents)
                    Debug.Log($"[HumanManager] House {house.HouseId} is at capacity ({house.MaxResidents}).");
                return null;
            }
            
            // Get or add HumanCharacter component
            HumanCharacter human = Instantiate(_config.HumanPrefabs.GetRandom(), this.transform);
            if (human == null)
            {
                Debug.LogWarning("[HumanManager] Pooled object missing HumanCharacter component.");
                return null;
            }
            
            // Initialize with data
            if (data == null)
                data = HumanData.CreateRandom(_config);
            
            human.Initialize(data, house, _config);
            
            // Position near house
            Vector3 spawnPosition = house.GetRandomSpawnPosition();
            human.transform.position = spawnPosition;
            human.gameObject.SetActive(true);
            
            // Register will happen automatically via HumanCharacter.Awake()
            // but we need to ensure it's tracked immediately
            RegisterHuman(human);
            
            if (_logSpawnEvents)
                Debug.Log($"[HumanManager] Spawned human near house {house.HouseId} at {spawnPosition}");
            
            return human;
        }
        
        /// <summary>
        /// Spawn multiple humans near a house.
        /// </summary>
        public List<HumanCharacter> SpawnHumans(House house, int count)
        {
            List<HumanCharacter> spawned = new List<HumanCharacter>();
            
            for (int i = 0; i < count; i++)
            {
                // Stop if house is full
                if (IsHouseFull(house))
                    break;
                
                HumanCharacter human = SpawnHuman(house);
                if (human != null)
                    spawned.Add(human);
            }
            
            return spawned;
        }
        
        /// <summary>
        /// Get all active human characters.
        /// Returns a new list containing references to all currently active humans.
        /// </summary>
        public List<HumanCharacter> GetAllHumans()
        {
            List<HumanCharacter> result = new List<HumanCharacter>(_activeHumans.Count);
            
            for (int i = 0; i < _activeHumans.Count; i++)
            {
                HumanCharacter human = _activeHumans[i];
                if (human != null && human.gameObject.activeSelf)
                {
                    result.Add(human);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get all humans with a specific behavior state.
        /// </summary>
        public List<HumanCharacter> GetHumansByState(BehaviorState state)
        {
            List<HumanCharacter> result = new List<HumanCharacter>();
            
            for (int i = 0; i < _activeHumans.Count; i++)
            {
                HumanCharacter human = _activeHumans[i];
                if (human != null && human.gameObject.activeSelf && human.GetCurrentBehaviorState() == state)
                {
                    result.Add(human);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Update all active humans in a batch for performance.
        /// </summary>
        public void UpdateAllHumans(float deltaTime)
        {
            // Use for loop to avoid allocations from enumerator
            for (int i = 0; i < _activeHumans.Count; i++)
            {
                HumanCharacter human = _activeHumans[i];
                if (human != null && human.gameObject.activeSelf)
                {
                    human.UpdateCharacter(deltaTime);
                }
            }
        }
        
        #endregion
        
        #region Object Pool Integration
        
        #endregion
        
        #region Private Methods
        
        private void CleanupAllHumans()
        {
            // Clear tracking lists
            _activeHumans.Clear();
            _humansByHouse.Clear();
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        [ContextMenu("Spawn Test Human")]
        private void SpawnTestHumanEditor()
        {
            if (Application.isPlaying)
                return;
                
            Debug.LogWarning("[HumanManager] Test spawn only works in Play mode.");
        }
        
        [ContextMenu("Log Human Statistics")]
        private void LogHumanStatisticsEditor()
        {
            if (!Application.isPlaying)
                return;
                
            Debug.Log($"[HumanManager] Statistics:\n" +
                     $"Active Humans: {ActiveHumanCount}\n" +
                     $"Total Houses: {_humansByHouse.Count}");
        }
#endif
        
        #endregion
    }
}