// Path: Assets/_Project/Scripts/Core/G.cs

using Data;
using Data.Configs;

using InputSystem;

using Project.Audio;
using Project.Core;
using Project.HouseGenerator.Config;
using Project.Utilities;
using Project.UI.Intro;

using UI;

using UnityEngine;

using Utilities;
using Common.Runtime.Components;

namespace Core
{
    /// <summary>
    /// Global service locator for core systems.
    /// Follows self-registration pattern: objects register themselves in Awake().
    /// </summary>
    public static class G
    {
        // Core Systems
        public static AudioManager Audio { get; set; }
        public static Main Main { get; set; }
        public static UIManager UI { get; set; }
        public static InputManager Input { get; set; }
        public static SaveSystem Save { get; set; }
        public static GameConfig Config { get; set; }
        public static HouseConfig HouseConfig { get; set; }
        public static GameStateManager GameState { get; set; }
        public static ObjectPool Pool { get; set; }
        public static EventSystem Events { get; set; }
        public static IntroSystem Intro { get; set; }
        public static CameraManager Camera { get; set; }

        // Null-check helpers
        public static bool HasAudio() => Audio != null;
        public static bool HasIntro() => Intro != null;
        public static bool HasMain() => Main != null;
        public static bool HasUI() => UI != null;
        public static bool HasInput() => Input != null;
        public static bool HasSave() => Save != null;
        public static bool HasConfig() => Config != null;
        public static bool HasHouseConfig() => HouseConfig != null;
        public static bool HasGameState() => GameState != null;
        public static bool HasPool() => Pool != null;
        public static bool HasEvents() => Events != null;
        public static bool HasCamera() => Camera != null;

        /// <summary>
        /// Safety check: logs a warning if a required system is missing.
        /// </summary>
        public static void EnsureSystem<T>(string systemName, T system) where T : class
        {
            if (system == null)
            {
                Debug.LogWarning($"[G] {systemName} is not registered. This may cause runtime errors.");
            }
        }

        /// <summary>
        /// Clear all references (useful for scene transitions or testing).
        /// </summary>
        public static void Clear()
        {
            Audio = null;
            Main = null;
            UI = null;
            Input = null;
            Save = null;
            Config = null;
            HouseConfig = null;
            GameState = null;
            Pool = null;
            Events = null;
            Intro = null;
            Camera = null;
        }
    }
}