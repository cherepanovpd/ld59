using UnityEngine;

namespace Project.UI.SignalButton.Config
{
    /// <summary>
    /// Contains animation curves for all visual transitions in the signal button system.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "UI/SignalButton/AnimationConfig")]
    public class AnimationConfig : ScriptableObject
    {
        [Header("Hover Animation")]
        [SerializeField, Tooltip("Curve for button scale animation when hovered (time 0-1, value 1 to hoverScaleMultiplier)")]
        private AnimationCurve _hoverScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.1f);
        
        [SerializeField, Tooltip("Curve for outline fade-in animation when hovered (time 0-1, value 0-1)")]
        private AnimationCurve _outlineFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Click Animation")]
        [SerializeField, Tooltip("Curve for button scale pulse when clicked (time 0-1, value 1 to clickScale and back)")]
        private AnimationCurve _clickScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

        [Header("Arrow Movement")]
        [SerializeField, Tooltip("Curve for arrow movement pattern (time 0-1, value 0-1 mapped to angle range)")]
        private AnimationCurve _arrowMovementCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Cooldown Visualization")]
        [SerializeField, Tooltip("Curve for cooldown fill animation (time 0-1, value 1-0)")]
        private AnimationCurve _cooldownFillCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        // Public properties with getters (read-only access)
        public AnimationCurve HoverScaleCurve => _hoverScaleCurve;
        public AnimationCurve OutlineFadeCurve => _outlineFadeCurve;
        public AnimationCurve ClickScaleCurve => _clickScaleCurve;
        public AnimationCurve ArrowMovementCurve => _arrowMovementCurve;
        public AnimationCurve CooldownFillCurve => _cooldownFillCurve;

        /// <summary>
        /// Validates animation curves to ensure they have valid keyframes.
        /// Called automatically by Unity Editor.
        /// </summary>
        private void OnValidate()
        {
            EnsureCurveNotEmpty(ref _hoverScaleCurve, AnimationCurve.EaseInOut(0f, 1f, 1f, 1.1f));
            EnsureCurveNotEmpty(ref _outlineFadeCurve, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            EnsureCurveNotEmpty(ref _clickScaleCurve, AnimationCurve.EaseInOut(0f, 1f,  1f, 1f));
            EnsureCurveNotEmpty(ref _arrowMovementCurve, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            EnsureCurveNotEmpty(ref _cooldownFillCurve, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }

        /// <summary>
        /// Ensures a curve has at least one keyframe, replacing it with a default if empty.
        /// </summary>
        private static void EnsureCurveNotEmpty(ref AnimationCurve curve, AnimationCurve defaultCurve)
        {
            if (curve == null || curve.keys.Length == 0)
            {
                curve = defaultCurve;
            }
        }

        /// <summary>
        /// Creates a deep copy of the animation curve to prevent accidental modification of the asset.
        /// </summary>
        /// <param name="curve">Original curve</param>
        /// <returns>New curve with same keyframes</returns>
        public AnimationCurve GetCopyOfCurve(AnimationCurve curve)
        {
            if (curve == null) return null;
            
            var newCurve = new AnimationCurve();
            foreach (var key in curve.keys)
            {
                newCurve.AddKey(key);
            }
            newCurve.preWrapMode = curve.preWrapMode;
            newCurve.postWrapMode = curve.postWrapMode;
            return newCurve;
        }
    }
}