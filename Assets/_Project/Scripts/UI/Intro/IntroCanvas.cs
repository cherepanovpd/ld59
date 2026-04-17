// Path: Assets/_Project/Scripts/UI/Intro/IntroCanvas.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Intro
{
    /// <summary>
    /// MonoBehaviour that manages the Intro UI Canvas and implements IIntroCanvas.
    /// Controls background and two text elements with fade capabilities.
    /// </summary>
    public class IntroCanvas : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private SpriteRenderer _backgroundImage;

        [Header("Text Elements")]
        [SerializeField] private TMP_Text _text1;
        [SerializeField] private TMP_Text _text2;

        [Header("Skip Functionality")]
        [SerializeField] private bool _enableClickSkip = true;

        // Event for skip requests
        public event System.Action OnSkipRequested;

        /// <summary>
        /// Cache references and set initial state.
        /// </summary>
        private void Awake()
        {
            if (_text1 != null)
            {
                _text1.alpha = 0f;
                _text1.gameObject.SetActive(true);
            }
            if (_text2 != null)
            {
                _text2.alpha = 0f;
                _text2.gameObject.SetActive(true);
            }
            if (_backgroundImage != null)
            {
                _backgroundImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Invoke skip event and log.
        /// </summary>
        private void RequestSkip()
        {
            Debug.Log("[IntroCanvas] Skip requested.");
            OnSkipRequested?.Invoke();
        }

        #region IIntroCanvas Implementation

        public void SetText1Alpha(float alpha)
        {
            if (_text1 != null)
            {
                _text1.alpha = Mathf.Clamp01(alpha);
            }
        }

        public void SetText2Alpha(float alpha)
        {
            if (_text2 != null)
            {
                _text2.alpha = Mathf.Clamp01(alpha);
            }
        }

        public void SetBackgroundAlpha(float alpha)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color =  new Color(_backgroundImage.color.r, _backgroundImage.color.g, _backgroundImage.color.b,  Mathf.Clamp01(alpha));
            }
        }

        public void SetText1Visible(bool visible)
        {
            if (_text1 != null)
            {
                _text1.gameObject.SetActive(visible);
            }
        }

        public void SetText2Visible(bool visible)
        {
            if (_text2 != null)
            {
                _text2.gameObject.SetActive(visible);
            }
        }

        #endregion
    }
}