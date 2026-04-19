// Path: Assets/_Project/Scripts/HouseGenerator/Config/HouseConfig.cs

using Core;
using Project.Core;
using UnityEngine;

namespace Project.HouseGenerator.Config
{
    /// <summary>
    /// ScriptableObject configuration for house generation.
    /// Contains color settings, roof color pairs, and element prefabs.
    /// Registers itself into G.HouseConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "HouseConfig", menuName = "Game/House Config", order = 101)]
    public class HouseConfig : ScriptableObject
    {
        [Header("Base Color Settings")]
        [SerializeField]
        [Tooltip("Base saturation (S in HSV). Default is 10.")]
        [Range(0f, 100f)]
        private float _baseSaturation = 10f;

        [SerializeField]
        [Tooltip("Base value (V in HSV). Default is 100.")]
        [Range(0f, 100f)]
        private float _baseValue = 100f;

        [Header("Roof Color Pairs")]
        [SerializeField]
        [Tooltip("Array of light/dark color pairs for roof variations.")]
        private HouseColorPair[] _roofColorPairs = new HouseColorPair[0];

        [Header("House Element Prefabs")]
        [SerializeField]
        [Tooltip("Prefabs for doors, windows, chimneys, etc.")]
        private GameObject[] _houseElementPrefabs = new GameObject[0];

        [Header("Window Lighting")]
        [SerializeField]
        [Tooltip("Configuration for window lighting behavior.")]
        private WindowLightingConfig _windowLightingConfig = new WindowLightingConfig();

        /// <summary>
        /// Base saturation (S in HSV). Default is 10.
        /// </summary>
        public float BaseSaturation => _baseSaturation;

        /// <summary>
        /// Base value (V in HSV). Default is 100.
        /// </summary>
        public float BaseValue => _baseValue;

        /// <summary>
        /// Array of light/dark color pairs for roof variations.
        /// </summary>
        public HouseColorPair[] RoofColorPairs => _roofColorPairs;

        /// <summary>
        /// Prefabs for doors, windows, chimneys, etc.
        /// </summary>
        public GameObject[] HouseElementPrefabs => _houseElementPrefabs;

        /// <summary>
        /// Configuration for window lighting behavior.
        /// </summary>
        public WindowLightingConfig WindowLightingConfig => _windowLightingConfig;

        #region Self-Registration

        private void OnEnable()
        {
            // Register this instance as the global house config if not already set
            if (G.HouseConfig == null)
            {
                G.HouseConfig = this;
                G.EnsureSystem(nameof(HouseConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.HouseConfig == this)
                G.HouseConfig = null;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            _baseSaturation = 10f;
            _baseValue = 100f;
            _roofColorPairs = new HouseColorPair[0];
            _houseElementPrefabs = new GameObject[0];
            _windowLightingConfig = new WindowLightingConfig();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[HouseConfig] Reset to defaults.");
        }
#endif

        #endregion
    }
}