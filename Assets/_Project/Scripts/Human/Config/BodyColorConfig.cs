// Path: Assets/_Project/Scripts/Human/Config/BodyColorConfig.cs

using Core;
using UnityEngine;

namespace Project.Human.Config
{
    /// <summary>
    /// ScriptableObject configuration for body color palette.
    /// Contains an array of colors for random selection during human generation.
    /// Registers itself into G.BodyColorConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "BodyColorConfig", menuName = "Game/Body Color Config", order = 111)]
    public class BodyColorConfig : ScriptableObject
    {
        [Header("Color Palette")]
        [SerializeField]
        [Tooltip("Array of body colors. One will be randomly selected during generation.")]
        private Color[] _palette = new Color[]
        {
            new Color(0.9f, 0.8f, 0.7f), // Light skin
            new Color(0.7f, 0.6f, 0.5f), // Medium skin
            new Color(0.5f, 0.4f, 0.3f), // Dark skin
            new Color(0.8f, 0.7f, 0.6f), // Light brown
            new Color(0.6f, 0.5f, 0.4f), // Medium brown
        };

        /// <summary>
        /// Array of body colors.
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
            // Register this instance as the global body color config if not already set
            if (G.BodyColorConfig == null)
            {
                G.BodyColorConfig = this;
                G.EnsureSystem(nameof(BodyColorConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.BodyColorConfig == this)
                G.BodyColorConfig = null;
        }

        #endregion
    }
}