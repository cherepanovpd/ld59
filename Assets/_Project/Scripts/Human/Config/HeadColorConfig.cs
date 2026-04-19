// Path: Assets/_Project/Scripts/Human/Config/HeadColorConfig.cs

using Core;
using UnityEngine;

namespace Project.Human.Config
{
    /// <summary>
    /// ScriptableObject configuration for head color palette.
    /// Contains an array of colors for random selection during human generation.
    /// Registers itself into G.HeadColorConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "HeadColorConfig", menuName = "Game/Head Color Config", order = 112)]
    public class HeadColorConfig : ScriptableObject
    {
        [Header("Color Palette")]
        [SerializeField]
        [Tooltip("Array of head colors. One will be randomly selected during generation.")]
        private Color[] _palette = new Color[]
        {
            new Color(0.95f, 0.85f, 0.75f), // Very light skin
            new Color(0.85f, 0.75f, 0.65f), // Light skin
            new Color(0.75f, 0.65f, 0.55f), // Medium skin
            new Color(0.65f, 0.55f, 0.45f), // Tan skin
            new Color(0.55f, 0.45f, 0.35f), // Dark skin
        };

        /// <summary>
        /// Array of head colors.
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
            // Register this instance as the global head color config if not already set
            if (G.HeadColorConfig == null)
            {
                G.HeadColorConfig = this;
                G.EnsureSystem(nameof(HeadColorConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.HeadColorConfig == this)
                G.HeadColorConfig = null;
        }

        #endregion
    }
}