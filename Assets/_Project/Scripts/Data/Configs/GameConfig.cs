// Path: Assets/_Project/Scripts/Data/Configs/GameConfig.cs

using Core;

using Project.Core;

using UnityEngine;

namespace Data.Configs
{
    /// <summary>
    /// ScriptableObject for game balance and configuration.
    /// Registers itself into G.Config on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Project/Game Config", order = 100)]
    public class GameConfig : ScriptableObject
    {
        // No fields - placeholder for future configuration
        // If you need to add game balance variables, add them here.

        #region Self-Registration

        private void OnEnable()
        {
            // Register this instance as the global config if not already set
            if (G.Config == null)
            {
                G.Config = this;
                G.EnsureSystem(nameof(GameConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.Config == this)
                G.Config = null;
        }

        #endregion

        #region Editor Helpers

        #if UNITY_EDITOR
        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            // No fields to reset
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[GameConfig] Reset to defaults (no fields).");
        }

        [ContextMenu("Print Current Values")]
        private void PrintValues() { Debug.Log("[GameConfig] No configuration fields defined."); }
        #endif

        #endregion
    }
}