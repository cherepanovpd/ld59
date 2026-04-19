// Path: Assets/_Project/Scripts/Human/HumanVisual.cs

using Core;

using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Human
{
    /// <summary>
    /// Handles visual assembly of body, head, and hair components with proper layering.
    /// Manages SpriteRenderers and applies colors.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanVisual : MonoBehaviour
    {
        [Header("Renderer References")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _headRenderer;
        [SerializeField] private SpriteRenderer _hairRenderer;
        
        [Header("Visual Configuration")]
        [SerializeField] private HumanData _humanData;
        
        [Header("Animation")]
        [SerializeField] private float _currentJumpT = 0f;
        [SerializeField] private Vector3 _baseScale = Vector3.one;
        
        // Cached references for performance
        private Transform _headTransform;
        private Transform _hairTransform;
        
        /// <summary>
        /// The human data used to configure this visual.
        /// </summary>
        public HumanData Data => _humanData;
        
        /// <summary>
        /// Body sprite renderer.
        /// </summary>
        public SpriteRenderer BodyRenderer => _bodyRenderer;
        
        /// <summary>
        /// Head sprite renderer.
        /// </summary>
        public SpriteRenderer HeadRenderer => _headRenderer;
        
        /// <summary>
        /// Hair sprite renderer.
        /// </summary>
        public SpriteRenderer HairRenderer => _hairRenderer;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheReferences();
            _baseScale = transform.localScale;
        }
        
        private void OnValidate()
        {
            // Auto-assign renderers if they're null
            if (_bodyRenderer == null)
                _bodyRenderer = GetComponent<SpriteRenderer>();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Initialize the visual with human data.
        /// This will set sprites, colors, and configure the visual hierarchy.
        /// </summary>
        public void Initialize(HumanData data)
        {
            _humanData = data;
            
            // Ensure we have all renderers
            EnsureRenderers();
            
            // Apply colors
            ApplyColors();
            
            // Cache the base scale for animation
            _baseScale = transform.localScale;
        }
        
        /// <summary>
        /// Apply the slime jump animation at time t (0-1).
        /// </summary>
        public void ApplySlimeJump(float t)
        {
            if (_headTransform == null)
                CacheReferences();
            
            _currentJumpT = t;
            
            // Simple sine-based bounce
            float bounce = Mathf.Sin(t * Mathf.PI * 2f) * 0.1f;
            Vector3 bounceOffset = Vector3.up * bounce;
            
            // Apply to head and hair for a cute bouncy effect
            if (_headTransform != null)
                _headTransform.localPosition = Vector3.up * 0.1f + bounceOffset * 0.5f;
            
            // Slight scale variation
            float scaleFactor = 1f + bounce * 0.05f;
            transform.localScale = _baseScale * scaleFactor;
        }
        
        /// <summary>
        /// Reset animation to default state.
        /// </summary>
        public void ResetAnimation()
        {
            _currentJumpT = 0f;
            
            if (_headTransform != null)
                _headTransform.localPosition = Vector3.up * 0.1f;
            
            if (transform.localScale != Vector3.zero)
                transform.localScale = _baseScale;
        }
        
        /// <summary>
        /// Update colors from the current human data.
        /// </summary>
        public void UpdateColors()
        {
            ApplyColors();
        }
        
        /// <summary>
        /// Set a new body color.
        /// </summary>
        public void SetBodyColor(Color color)
        {
            if (_bodyRenderer != null)
                _bodyRenderer.color = color;
        }
        
        /// <summary>
        /// Set a new head color.
        /// </summary>
        public void SetHeadColor(Color color)
        {
            if (_headRenderer != null)
                _headRenderer.color = color;
        }
        
        /// <summary>
        /// Set a new hair color.
        /// </summary>
        public void SetHairColor(Color color)
        {
            if (_hairRenderer != null)
                _hairRenderer.color = color;
        }
        
        #endregion
        
        #region Private Methods
        
        private void CacheReferences()
        {
            // Cache transform references for performance
            if (_headRenderer != null)
                _headTransform = _headRenderer.transform;
            
            if (_hairRenderer != null)
                _hairTransform = _hairRenderer.transform;
        }
        
        private void EnsureRenderers()
        {
            // Ensure body renderer exists (it's required by the component)
            if (_bodyRenderer == null)
                _bodyRenderer = GetComponent<SpriteRenderer>();
            
            // Create head renderer if needed
            if (_headRenderer == null)
            {
                GameObject headObj = new GameObject("Head");
                headObj.transform.SetParent(transform);
                headObj.transform.localPosition = Vector3.up * 0.1f;
                _headRenderer = headObj.AddComponent<SpriteRenderer>();
                _headTransform = headObj.transform;
            }
            
            // Create hair renderer if needed
            if (_hairRenderer == null)
            {
                GameObject hairObj = new GameObject("Hair");
                hairObj.transform.SetParent(transform);
                hairObj.transform.localPosition = Vector3.up * 0.15f;
                _hairRenderer = hairObj.AddComponent<SpriteRenderer>();
                _hairTransform = hairObj.transform;
            }
        }
        
        private void ApplyColors()
        {
            if (_bodyRenderer != null)
                _bodyRenderer.color = _humanData.BodyColor;
            
            if (_headRenderer != null)
                _headRenderer.color = _humanData.HeadColor;
            
            if (_hairRenderer != null)
                _hairRenderer.color = _humanData.HairColor;
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        [ContextMenu("Randomize Colors")]
        private void RandomizeColorsEditor()
        {
            if (Application.isPlaying)
                return;
                
            // Get config from G if available
            var config = G.HumanConfig;
            if (config == null)
            {
                Debug.LogWarning("No HumanConfig found in G. Cannot randomize colors.");
                return;
            }
            
            // Generate random colors
            Color bodyColor = config.BodyColorConfig.GetRandomColor();
            Color headColor = config.HeadColorConfig.GetRandomColor();
            Color hairColor = config.HairColorConfig.GetRandomColor();
            
            // Apply
            SetBodyColor(bodyColor);
            SetHeadColor(headColor);
            SetHairColor(hairColor);
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [ContextMenu("Setup Renderers")]
        private void SetupRenderersEditor()
        {
            if (Application.isPlaying)
                return;
                
            EnsureRenderers();
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        
        #endregion
    }
}