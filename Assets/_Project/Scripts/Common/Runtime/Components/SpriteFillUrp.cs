using UnityEngine;

namespace Common.Runtime.Components
{
    [ExecuteAlways]
    public class SpriteFillUrp : MonoBehaviour
    {
        [Header("Fill Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float fillAmount = 1f;
        [SerializeField] private bool fillFromRightToLeft = false;
    
        [Header("Animation")]
        [SerializeField] private bool animateOnStart = false;
        [SerializeField] private float animationDuration = 1f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
        private SpriteRenderer spriteRenderer;
        private MaterialPropertyBlock materialPropertyBlock;
        private float currentFill;
        private bool isAnimating;
        private float animationTimer;
        private float startFill;
        private float targetFill;
    
        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            materialPropertyBlock = new MaterialPropertyBlock();
        
            if (animateOnStart)
            {
                AnimateFill(fillAmount);
            }
            else
            {
                SetFillAmount(fillAmount);
                SetDirection(fillFromRightToLeft);
            }
        }
    
        void Update()
        {
            if (isAnimating)
            {
                animationTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(animationTimer / animationDuration);
                float curveValue = animationCurve.Evaluate(progress);
            
                currentFill = Mathf.Lerp(startFill, targetFill, curveValue);
                ApplyFill();
            
                if (progress >= 1f)
                {
                    isAnimating = false;
                }
            }
            else if (currentFill != fillAmount)
            {
                currentFill = fillAmount;
                ApplyFill();
            }
        }
    
        public void SetFillAmount(float amount)
        {
            fillAmount = Mathf.Clamp01(amount);
            if (!isAnimating)
            {
                currentFill = fillAmount;
                ApplyFill();
            }
        }
    
        public void AnimateFill(float targetAmount, float duration = -1)
        {
            if (duration > 0) animationDuration = duration;
        
            startFill = currentFill;
            targetFill = Mathf.Clamp01(targetAmount);
            animationTimer = 0f;
            isAnimating = true;
            fillAmount = targetFill;
        }
    
        public void AnimateFill(float targetAmount)
        {
            AnimateFill(targetAmount, animationDuration);
        }
    
        public void SetDirection(bool rightToLeft)
        {
            fillFromRightToLeft = rightToLeft;
            ApplyFill();
        }
    
        private void ApplyFill()
        {
            if (spriteRenderer == null || materialPropertyBlock == null) return;
        
            spriteRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetFloat("_Fill", currentFill);
            materialPropertyBlock.SetFloat("_FillDirection", fillFromRightToLeft ? 1f : 0f);
            spriteRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    
        public float GetCurrentFill() => currentFill;
        public bool IsAnimating() => isAnimating;
    
        [ContextMenu("Fill 0%")]
        public void Fill0() => SetFillAmount(0);
    
        [ContextMenu("Fill 25%")]
        public void Fill25() => SetFillAmount(0.25f);
    
        [ContextMenu("Fill 50%")]
        public void Fill50() => SetFillAmount(0.5f);
    
        [ContextMenu("Fill 75%")]
        public void Fill75() => SetFillAmount(0.75f);
    
        [ContextMenu("Fill 100%")]
        public void Fill100() => SetFillAmount(1f);
    
        [ContextMenu("Animate to 100%")]
        public void AnimateToFull() => AnimateFill(1f);
    }
}