// Path: Assets/_Project/Scripts/HouseGenerator/HouseGenerator.cs

using Core;
using Project.HouseGenerator.Config;
using UnityEngine;

namespace Project.HouseGenerator
{
    /// <summary>
    /// MonoBehaviour that procedurally generates houses with configurable colors and elements.
    /// Follows the project's architecture: uses G.HouseConfig for configuration,
    /// caches references, and ensures zero allocations in execution loops.
    /// </summary>
    public class HouseGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private SpriteRenderer _baseRenderer;

        [SerializeField]
        private SpriteRenderer _roofLeftRenderer;

        [SerializeField]
        private SpriteRenderer _roofRightRenderer;

        [Header("Generation Settings")]
        [SerializeField]
        [Tooltip("If true, house will be generated automatically on Awake.")]
        private bool _generateOnAwake = true;

        [SerializeField]
        [Tooltip("If true, house will be regenerated on Start (useful for testing).")]
        private bool _regenerateOnStart = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_generateOnAwake)
                GenerateHouse();
        }

        private void Start()
        {
            if (_regenerateOnStart)
                GenerateHouse();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Main method to create a complete house with base, roof, and random elements.
        /// </summary>
        public void GenerateHouse()
        {
            if (!ValidateConfig())
                return;

            GenerateBase();
            GenerateRoof();
            GenerateElements();
        }

        /// <summary>
        /// Creates the house base with a random hue using HSV color from config.
        /// </summary>
        public void GenerateBase()
        {
            if (_baseRenderer == null)
                return;

            float hue = Random.Range(0f, 1f);
            float saturation = G.HouseConfig.BaseSaturation / 100f;
            float value = G.HouseConfig.BaseValue / 100f;

            Color baseColor = GetRandomColorFromHSV(hue, saturation, value);
            _baseRenderer.color = baseColor;
        }

        /// <summary>
        /// Creates left/right roof parts with a random color pair from config.
        /// </summary>
        public void GenerateRoof()
        {
            if (_roofLeftRenderer == null || _roofRightRenderer == null)
                return;

            HouseColorPair[] pairs = G.HouseConfig.RoofColorPairs;
            if (pairs == null || pairs.Length == 0)
            {
                Debug.LogWarning("[HouseGenerator] No roof color pairs defined in config. Using fallback colors.");
                _roofLeftRenderer.color = Color.gray;
                _roofRightRenderer.color = Color.gray;
                return;
            }

            int randomIndex = Random.Range(0, pairs.Length);
            HouseColorPair selectedPair = pairs[randomIndex];

            _roofLeftRenderer.color = selectedPair.LightColor;
            _roofRightRenderer.color = selectedPair.DarkColor;
        }

        /// <summary>
        /// Instantiates random door/window prefabs from config as children.
        /// </summary>
        public void GenerateElements()
        {
            GameObject[] prefabs = G.HouseConfig.HouseElementPrefabs;
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("[HouseGenerator] No house element prefabs defined in config.");
                return;
            }

            // For now, instantiate one random element as a child
            int randomIndex = Random.Range(0, prefabs.Length);
            GameObject prefab = prefabs[randomIndex];
            if (prefab == null)
            {
                Debug.LogError("[HouseGenerator] Selected prefab is null.");
                return;
            }

            Instantiate(prefab, transform);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Converts HSV values to RGB Color using Unity's Color.HSVToRGB.
        /// </summary>
        /// <param name="h">Hue [0,1]</param>
        /// <param name="s">Saturation [0,1]</param>
        /// <param name="v">Value [0,1]</param>
        /// <returns>RGB Color</returns>
        public static Color GetRandomColorFromHSV(float h, float s, float v)
        {
            return Color.HSVToRGB(h, s, v);
        }

        #endregion

        #region Private Setup

        private bool ValidateConfig()
        {
            if (!G.HasHouseConfig())
            {
                Debug.LogError("[HouseGenerator] G.HouseConfig is not registered.");
                return false;
            }

            return true;
        }

        #endregion
    }
}