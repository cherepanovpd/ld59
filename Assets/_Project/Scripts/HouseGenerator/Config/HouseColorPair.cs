// Path: Assets/_Project/Scripts/HouseGenerator/Config/HouseColorPair.cs

using UnityEngine;

namespace Project.HouseGenerator.Config
{
    /// <summary>
    /// Serializable pair of light and dark colors for roof coloring.
    /// </summary>
    [System.Serializable]
    public struct HouseColorPair
    {
        [SerializeField]
        [Tooltip("Light color variant for roof highlights")]
        private Color _lightColor;

        [SerializeField]
        [Tooltip("Dark color variant for roof shadows")]
        private Color _darkColor;

        /// <summary>
        /// Light color variant for roof highlights.
        /// </summary>
        public Color LightColor => _lightColor;

        /// <summary>
        /// Dark color variant for roof shadows.
        /// </summary>
        public Color DarkColor => _darkColor;

        /// <summary>
        /// Creates a new color pair.
        /// </summary>
        /// <param name="light">Light color.</param>
        /// <param name="dark">Dark color.</param>
        public HouseColorPair(Color light, Color dark)
        {
            _lightColor = light;
            _darkColor = dark;
        }
    }
}