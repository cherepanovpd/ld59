// Path: Assets/_Project/Scripts/Core/G.cs

using Data;
using Data.Configs;

using InputSystem;

using Project.Audio;
using Project.Core;
using Project.Currency.Config;
using Project.DayNight;
using Project.DayNight.Config;
using Project.HouseGenerator.Config;
using Project.Human.Config;
using Project.Effects;
using Project.Utilities;
using Project.UI.Intro;

using UI;

using UnityEngine;

using Utilities;
using Common.Runtime.Components;

using Project.Currency;
using Project.Human;
using Project.UI.SignalButton;

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
        public static DayNightVolumeController DayNight { get; set; }
        public static DayNightSystem DayNightSystem { get; set; }
        public static DayNightConfig DayNightConfig { get; set; }
        public static WindowLightManager WindowLightManager { get; set; }
        public static CurrencyConfig CurrencyConfig { get; set; }
        public static GameParameters GameParameters { get; set; }
        
        // Human System Configs
        public static HumanConfig HumanConfig { get; set; }
        public static BodyColorConfig BodyColorConfig { get; set; }
        public static HeadColorConfig HeadColorConfig { get; set; }
        public static HairColorConfig HairColorConfig { get; set; }
        
        // Human Management
        public static HumanManager HumanManager { get; set; }
        public static HumanBehaviorSystem HumanBehaviorSystem { get; set; }
        
        public static BillManager BillManager { get; set; }
        public static CurrencyCounter Currency { get; set; }
        public static BillInteractionSystem BillInteraction { get; set; }
        public static SignalScheduler SignalScheduler { get; set; }
        
        // Signal Button System
        public static SignalButtonUI SignalButton { get; set; }

        // Null-check helpers
        public static bool HasAudio() => Audio != null;
        public static bool HasDayNight() => DayNight != null;
        public static bool HasDayNightSystem() => DayNightSystem != null;
        public static bool HasDayNightConfig() => DayNightConfig != null;
        public static bool HasIntro() => Intro != null;
        public static bool HasMain() => Main != null;
        public static bool HasUI() => UI != null;
        public static bool HasInput() => Input != null;
        public static bool HasHumanBehaviorSystem() => HumanBehaviorSystem != null;
        public static bool HasSave() => Save != null;
        public static bool HasConfig() => Config != null;
        public static bool HasHouseConfig() => HouseConfig != null;
        public static bool HasGameState() => GameState != null;
        public static bool HasPool() => Pool != null;
        public static bool HasEvents() => Events != null;
        public static bool HasCamera() => Camera != null;
        public static bool HasWindowLightManager() => WindowLightManager != null;
        public static bool HasCurrencyConfig() => CurrencyConfig != null;
        public static bool HasGameParameters() => GameParameters != null;
        public static bool HasBillManager() => BillManager != null;
        public static bool HasCurrency() => Currency != null;
        public static bool HasBillInteraction() => BillInteraction != null;
        public static bool HasSignalScheduler() => SignalScheduler != null;
        public static bool HasSignalButton() => SignalButton != null;
        
        // Human System Config Helpers
        public static bool HasHumanConfig() => HumanConfig != null;
        public static bool HasBodyColorConfig() => BodyColorConfig != null;
        public static bool HasHeadColorConfig() => HeadColorConfig != null;
        public static bool HasHairColorConfig() => HairColorConfig != null;
        public static bool HasHumanManager() => HumanManager != null;

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
            DayNight = null;
            DayNightSystem = null;
            DayNightConfig = null;
            WindowLightManager = null;
            CurrencyConfig = null;
            GameParameters = null;
            
            // Human System Configs
            HumanConfig = null;
            BodyColorConfig = null;
            HeadColorConfig = null;
            HairColorConfig = null;
            
            // Human Management
            HumanManager = null;
            HumanBehaviorSystem = null;
            
            BillManager = null;
            Currency = null;
            BillInteraction = null;
            SignalScheduler = null;
            SignalButton = null;
        }
    }
}