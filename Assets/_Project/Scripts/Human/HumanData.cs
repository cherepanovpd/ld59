// Path: Assets/_Project/Scripts/Human/HumanData.cs

using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Data container for a human character's visual configuration.
    /// This is a struct to be lightweight and passed by value.
    /// </summary>
    [System.Serializable]
    public class HumanData
    {
        [Header("Colors")]
        [SerializeField] private Color _bodyColor;
        [SerializeField] private Color _headColor;
        [SerializeField] private Color _hairColor;
        
        /// <summary>
        /// Color for the body renderer.
        /// </summary>
        public Color BodyColor => _bodyColor;
        
        /// <summary>
        /// Color for the head renderer.
        /// </summary>
        public Color HeadColor => _headColor;
        
        /// <summary>
        /// Color for the hair renderer.
        /// </summary>
        public Color HairColor => _hairColor;
        
        /// <summary>
        /// Create a HumanData with all visual components and colors.
        /// </summary>
        public HumanData(
            Color bodyColor,
            Color headColor,
            Color hairColor)
        {
            _bodyColor = bodyColor;
            _headColor = headColor;
            _hairColor = hairColor;
        }
        
        /// <summary>
        /// Create a random HumanData using the provided config.
        /// </summary>
        public static HumanData CreateRandom(Config.HumanConfig config)
        {
            if (config == null)
            {
                Debug.LogError("HumanConfig is null, cannot create random HumanData.");
                return default;
            }
            // Get random colors from configs
            Color bodyColor = config.BodyColorConfig.GetRandomColor();
            Color headColor = config.HeadColorConfig.GetRandomColor();
            Color hairColor = config.HairColorConfig.GetRandomColor();
            
            return new HumanData(
                bodyColor,
                headColor,
                hairColor
            );
        }
    }
}