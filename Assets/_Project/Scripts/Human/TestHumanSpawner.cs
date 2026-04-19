// Path: Assets/_Project/Scripts/Human/TestHumanSpawner.cs

using Core;
using Project.Human.Config;
using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Simple test component that spawns a human character when the game starts.
    /// Useful for verifying the human character system works correctly.
    /// </summary>
    public class TestHumanSpawner : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private int _humanCount = 3;
        [SerializeField] private float _spawnRadius = 5f;
        
        [Header("References")]
        [SerializeField] private GameObject _humanPrefab;
        [SerializeField] private House _testHouse;
        [SerializeField] private HumanConfig _configOverride;
        
        private void Start()
        {
            if (_spawnOnStart)
                SpawnTestHumans();
        }
        
        /// <summary>
        /// Spawn test humans around the test house.
        /// </summary>
        public void SpawnTestHumans()
        {
            if (_humanPrefab == null)
            {
                Debug.LogError("Human prefab not assigned to TestHumanSpawner.");
                return;
            }
            
            if (_testHouse == null)
            {
                Debug.LogError("Test house not assigned to TestHumanSpawner.");
                return;
            }
            
            HumanConfig config = _configOverride ?? G.HumanConfig;
            if (config == null)
            {
                Debug.LogError("HumanConfig not found in G. Please ensure a HumanConfig asset exists and is registered.");
                return;
            }
            
            for (int i = 0; i < _humanCount; i++)
            {
                Vector3 randomOffset = Random.insideUnitCircle * _spawnRadius;
                Vector3 spawnPosition = _testHouse.transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
                
                GameObject humanGo = Instantiate(_humanPrefab, spawnPosition, Quaternion.identity);
                HumanCharacter humanCharacter = humanGo.GetComponent<HumanCharacter>();
                
                if (humanCharacter != null)
                {
                    humanCharacter.InitializeWithRandomData(_testHouse, config);
                    Debug.Log($"Spawned human {i + 1} at {spawnPosition}");
                }
                else
                {
                    Debug.LogWarning($"Human prefab missing HumanCharacter component.");
                }
            }
            
            Debug.Log($"Spawned {_humanCount} test humans.");
        }
        
        /// <summary>
        /// Clear all spawned humans (destroys all HumanCharacter objects in the scene).
        /// </summary>
        public void ClearTestHumans()
        {
            HumanCharacter[] humans = FindObjectsOfType<HumanCharacter>();
            foreach (HumanCharacter human in humans)
            {
                if (human != null && human.gameObject != gameObject)
                    Destroy(human.gameObject);
            }
            
            Debug.Log($"Cleared {humans.Length} humans.");
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Test Human System/Spawn Test Humans")]
        private static void SpawnTestHumansFromMenu()
        {
            TestHumanSpawner spawner = FindObjectOfType<TestHumanSpawner>();
            if (spawner == null)
            {
                Debug.LogError("No TestHumanSpawner found in the scene. Please add one.");
                return;
            }
            
            spawner.SpawnTestHumans();
        }
        
        [UnityEditor.MenuItem("Tools/Test Human System/Clear Test Humans")]
        private static void ClearTestHumansFromMenu()
        {
            TestHumanSpawner spawner = FindObjectOfType<TestHumanSpawner>();
            if (spawner == null)
            {
                Debug.LogError("No TestHumanSpawner found in the scene.");
                return;
            }
            
            spawner.ClearTestHumans();
        }
        #endif
    }
}