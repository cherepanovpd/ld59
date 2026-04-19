// Path: Assets/_Project/Scripts/Human/Config/HairColorConfig.cs

using Core;
using UnityEngine;

namespace Project.Human.Config
{
    /// <summary>
    /// ScriptableObject configuration for hair color palette.
    /// Contains an array of colors for random selection during human generation.
    /// Registers itself into G.HairColorConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "HairColorConfig", menuName = "Game/Hair Color Config", order = 113)]
    public class HairColorConfig : ScriptableObject
    {
        [Header("Color Palette")]
        [SerializeField]
        [Tooltip("Array of hair colors. One will be randomly selected during generation.")]
        private Color[] _palette = new Color[]
        {
            new Color(0.1f, 0.1f, 0.1f),     // Black
            new Color(0.3f, 0.2f, 0.1f),     // Dark brown
            new Color(0.5f, 0.35f, 0.2f),    // Brown
            new Color(0.7f, 0.5f, 0.3f),     // Light brown
            new Color(0.9f, 0.7f, 0.4f),     // Blonde
            new Color(0.8f, 0.6f, 0.4f),     // Honey blonde
            new Color(0.7f, 0.5f, 0.4f),     // Auburn
            new Color(0.6f, 0.3f, 0.2f),     // Red brown
            new Color(0.4f, 0.4f, 0.4f),     // Gray
            new Color(0.9f, 0.9f, 0.9f),     // White
        };

        /// <summary>
        /// Array of hair colors.
        /// </summary>
        public Color[] Palette => _palette;

        /// <summary>
        /// Returns a random color from the palette.
        /// </summary>
        public Color GetRandomColor()
        {
            if (_palette == null || _palette.Length == 0)
                return Color.white;

            return _palette[Random.Range(0, _palette.Length)];
        }

        #region Self‑Registration

        private void OnEnable()
        {
            // Register this instance as the global hair color config if not already set
            if (G.HairColorConfig == null)
            {
                G.HairColorConfig = this;
                G.EnsureSystem(nameof(HairColorConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.HairColorConfig == this)
                G.HairColorConfig = null;
        }

        #endregion
    }
}