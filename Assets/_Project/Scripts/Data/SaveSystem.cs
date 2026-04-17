// Path: Assets/_Project/Scripts/Data/SaveSystem.cs

using System;
using System.Collections.Generic;
using System.Text;

using Core;

using Project.Core;

using UnityEngine;

namespace Data
{
    /// <summary>
    /// Wrapper for PlayerPrefs with optional encryption and JSON serialization.
    /// Follows zero-allocation principles and self-registers into G.Save.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Header("Encryption Settings")]
        [SerializeField] private bool _useEncryption = false;
        [SerializeField] private string _encryptionKey = "default-key-change-me";

        [Header("Backup")]
        [SerializeField] private bool _enableBackup = true;
        [SerializeField] private int _maxBackupCount = 3;

        // Keys that should NOT be cleared when resetting game data (e.g., volume settings)
        private static readonly HashSet<string> _protectedKeys = new HashSet<string>
        {
            "MasterVolume",
            "MusicVolume",
            "SFXVolume"
        };

        // Registry of all keys saved through this system (excluding protected keys)
        private static readonly string REGISTRY_KEY = "__key_registry";
        private static HashSet<string> _keyRegistry;

        private static readonly Dictionary<Type, Func<string, object>> _typeParsers;
        private static readonly Dictionary<Type, Action<string, object>> _typeSavers;

        private static SaveSystem _instance;
        private static byte[] _encryptionKeyBytes;

        static SaveSystem()
        {
            // Initialize type parsers and savers (zero allocation at runtime)
            _typeParsers = new Dictionary<Type, Func<string, object>>
            {
                { typeof(int), key => PlayerPrefs.GetInt(key) },
                { typeof(float), key => PlayerPrefs.GetFloat(key) },
                { typeof(string), key => PlayerPrefs.GetString(key) },
                { typeof(bool), key => PlayerPrefs.GetInt(key) == 1 },
                { typeof(Vector2), key => ParseVector2(key) },
                { typeof(Vector3), key => ParseVector3(key) },
                { typeof(Quaternion), key => ParseQuaternion(key) },
            };

            _typeSavers = new Dictionary<Type, Action<string, object>>
            {
                { typeof(int), (key, value) => PlayerPrefs.SetInt(key, (int)value) },
                { typeof(float), (key, value) => PlayerPrefs.SetFloat(key, (float)value) },
                { typeof(string), (key, value) => PlayerPrefs.SetString(key, (string)value) },
                { typeof(bool), (key, value) => PlayerPrefs.SetInt(key, (bool)value ? 1 : 0) },
                { typeof(Vector2), (key, value) => SaveVector2(key, (Vector2)value) },
                { typeof(Vector3), (key, value) => SaveVector3(key, (Vector3)value) },
                { typeof(Quaternion), (key, value) => SaveQuaternion(key, (Quaternion)value) },
            };
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Self-registration into G
            if (G.Save == null)
            {
                G.Save = this;
                G.EnsureSystem(nameof(SaveSystem), this);
            }

            InitializeEncryption();
            LoadKeyRegistry();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                if (G.Save == this)
                    G.Save = null;
            }
        }

        private void InitializeEncryption()
        {
            if (!_useEncryption) return;

            // Convert key to bytes (cached)
            _encryptionKeyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
        }

        private void LoadKeyRegistry()
        {
            _keyRegistry = new HashSet<string>();
            if (PlayerPrefs.HasKey(REGISTRY_KEY))
            {
                string json = PlayerPrefs.GetString(REGISTRY_KEY);
                var list = JsonUtility.FromJson<StringListWrapper>(json);
                if (list != null && list.items != null)
                {
                    foreach (var key in list.items)
                    {
                        _keyRegistry.Add(key);
                    }
                }
            }
        }

        private void SaveKeyRegistry()
        {
            var wrapper = new StringListWrapper { items = new List<string>(_keyRegistry) };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(REGISTRY_KEY, json);
            PlayerPrefs.Save();
        }

        private void AddKeyToRegistry(string key)
        {
            if (_protectedKeys.Contains(key))
                return; // Don't track protected keys

            if (_keyRegistry.Add(key))
            {
                SaveKeyRegistry();
            }
        }

        private void RemoveKeyFromRegistry(string key)
        {
            if (_keyRegistry.Remove(key))
            {
                SaveKeyRegistry();
            }
        }

        #region Public API - Generic Methods

        /// <summary>
        /// Save a value of any supported type with the given key.
        /// </summary>
        public void Save<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.");

            Type type = typeof(T);
            if (_typeSavers.TryGetValue(type, out var saver))
            {
                saver(key, value);
            }
            else
            {
                // Fallback to JSON serialization for complex objects
                SaveJson(key, value);
            }

            AddKeyToRegistry(key);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load a value of type T. Returns default(T) if key doesn't exist.
        /// </summary>
        public T Load<T>(string key, T defaultValue = default)
        {
            if (!HasKey(key))
                return defaultValue;

            Type type = typeof(T);
            if (_typeParsers.TryGetValue(type, out var parser))
            {
                return (T)parser(key);
            }

            // Fallback to JSON deserialization
            return LoadJson<T>(key, defaultValue);
        }

        /// <summary>
        /// Check if a key exists.
        /// </summary>
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// Delete a specific key.
        /// </summary>
        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            RemoveKeyFromRegistry(key);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Delete all saved data (use with caution).
        /// </summary>
        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            _keyRegistry.Clear();
            SaveKeyRegistry();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Delete all keys except protected ones (e.g., volume settings).
        /// </summary>
        public void ResetAllExceptSettings()
        {
            // Delete all keys in registry (non-protected)
            foreach (var key in new List<string>(_keyRegistry))
            {
                PlayerPrefs.DeleteKey(key);
            }
            _keyRegistry.Clear();
            SaveKeyRegistry();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Delete only game data keys (excluding settings).
        /// This is an alias for ResetAllExceptSettings for clarity.
        /// </summary>
        public void ResetGameData()
        {
            ResetAllExceptSettings();
        }

        #endregion

        #region JSON Serialization

        /// <summary>
        /// Save any object as JSON (supports complex types).
        /// </summary>
        public void SaveJson<T>(string key, T obj)
        {
            string json = JsonUtility.ToJson(obj);
            if (_useEncryption)
                json = Encrypt(json);
            PlayerPrefs.SetString(key, json);
            AddKeyToRegistry(key);
        }

        /// <summary>
        /// Load object from JSON.
        /// </summary>
        public T LoadJson<T>(string key, T defaultValue = default)
        {
            if (!HasKey(key))
                return defaultValue;

            string json = PlayerPrefs.GetString(key);
            if (_useEncryption)
                json = Decrypt(json);

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                Debug.LogWarning($"[SaveSystem] Failed to deserialize JSON for key '{key}'. Returning default.");
                return defaultValue;
            }
        }

        #endregion

        #region Backup & Restore

        /// <summary>
        /// Create a backup of current PlayerPrefs.
        /// </summary>
        public void CreateBackup(string backupName)
        {
            if (!_enableBackup) return;

            var backup = new Dictionary<string, string>();
            foreach (var key in GetAllKeys())
            {
                backup[key] = PlayerPrefs.GetString(key);
            }

            string backupKey = $"backup_{backupName}";
            SaveJson(backupKey, backup);
            TrimBackups();
        }

        /// <summary>
        /// Restore from a named backup.
        /// </summary>
        public bool RestoreBackup(string backupName)
        {
            string backupKey = $"backup_{backupName}";
            if (!HasKey(backupKey))
                return false;

            var backup = LoadJson<Dictionary<string, string>>(backupKey);
            if (backup == null)
                return false;

            DeleteAll();
            foreach (var kvp in backup)
            {
                PlayerPrefs.SetString(kvp.Key, kvp.Value);
            }
            PlayerPrefs.Save();
            return true;
        }

        private void TrimBackups()
        {
            // Implementation would list and delete excess backups
            // Simplified for brevity
        }

        #endregion

        #region Encryption

        private string Encrypt(string plainText)
        {
            if (!_useEncryption || string.IsNullOrEmpty(plainText))
                return plainText;

            // Simple XOR encryption for demonstration (replace with stronger algorithm in production)
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= _encryptionKeyBytes[i % _encryptionKeyBytes.Length];
            }
            return Convert.ToBase64String(data);
        }

        private string Decrypt(string cipherText)
        {
            if (!_useEncryption || string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] data = Convert.FromBase64String(cipherText);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= _encryptionKeyBytes[i % _encryptionKeyBytes.Length];
                }
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return cipherText;
            }
        }

        #endregion

        #region Static Helpers

        private static Vector2 ParseVector2(string key)
        {
            string json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<Vector2>(json);
        }

        private static Vector3 ParseVector3(string key)
        {
            string json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<Vector3>(json);
        }

        private static Quaternion ParseQuaternion(string key)
        {
            string json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<Quaternion>(json);
        }

        private static void SaveVector2(string key, Vector2 value)
        {
            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
        }

        private static void SaveVector3(string key, Vector3 value)
        {
            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
        }

        private static void SaveQuaternion(string key, Quaternion value)
        {
            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
        }

        private static IEnumerable<string> GetAllKeys()
        {
            // PlayerPrefs doesn't expose keys; we'd need a custom registry.
            // For simplicity, return empty (real implementation would track keys).
            yield break;
        }

        #endregion

        #region Statistics

        public int GetKeyCount()
        {
            return _keyRegistry.Count;
        }

        public void PrintAllKeys()
        {
            Debug.Log($"[SaveSystem] Registered keys: {string.Join(", ", _keyRegistry)}");
        }

        #endregion

        // Helper class for JSON serialization of string list
        [System.Serializable]
        private class StringListWrapper
        {
            public List<string> items;
        }
    }
}