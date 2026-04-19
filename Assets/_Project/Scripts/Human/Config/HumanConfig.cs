// Path: Assets/_Project/Scripts/Human/Config/HumanConfig.cs

using Core;
using UnityEngine;

namespace Project.Human.Config
{
    /// <summary>
    /// ScriptableObject configuration for human character generation and behavior.
    /// Contains prefab references, color configs, generation parameters, movement settings, and animation curves.
    /// Registers itself into G.HumanConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "HumanConfig", menuName = "Game/Human Config", order = 110)]
    public class HumanConfig : ScriptableObject
    {
        [Header("Prefab Settings")]
        [SerializeField]
        [Tooltip("Array of body prefabs with different heights. One will be randomly selected during generation.")]
        private HumanCharacter[] _humanPrefabs;

        [Header("Color Config References")]
        [SerializeField]
        [Tooltip("Reference to the BodyColorConfig ScriptableObject.")]
        private BodyColorConfig _bodyColorConfig;

        [SerializeField]
        [Tooltip("Reference to the HeadColorConfig ScriptableObject.")]
        private HeadColorConfig _headColorConfig;

        [SerializeField]
        [Tooltip("Reference to the HairColorConfig ScriptableObject.")]
        private HairColorConfig _hairColorConfig;

        [Header("Generation Settings")]
        [SerializeField]
        [Tooltip("Radius around a house where humans can spawn.")]
        private float _spawnRadius = 1f;

        [SerializeField]
        [Range(1, 10)]
        [Tooltip("Maximum number of humans that can be assigned to a single house.")]
        private int _maxHumansPerHouse = 3;

        [SerializeField]
        [Tooltip("Minimum distance from the house center where humans can spawn.")]
        private float _minSpawnDistanceFromHouse = 2f;

        [Header("Movement Settings")]
        [SerializeField]
        [Tooltip("Movement speed during day (wandering phase).")]
        private float _dayMovementSpeed = 2f;

        [SerializeField]
        [Tooltip("Movement speed during night (returning home phase).")]
        private float _nightMovementSpeed = 4f;

        [SerializeField]
        [Tooltip("Maximum wander radius from spawn point during day.")]
        private float _wanderRadius = 10f;

        [SerializeField]
        [Tooltip("Speed at which humans return home when lights turn on.")]
        private float _homeReturnSpeed = 3f;

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Frequency of the slime jump animation (cycles per second).")]
        private float _slimeJumpFrequency = 1f;

        [SerializeField]
        [Tooltip("Amplitude of the slime jump animation (scale multiplier).")]
        private float _slimeJumpAmplitude = 0.2f;

        [SerializeField]
        [Tooltip("Curve defining the shape of the slime jump animation.")]
        private AnimationCurve _slimeJumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Behavior Timing")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Normalized day time when humans start exiting houses (0 = midnight, 1 = next midnight).")]
        private float _morningExitTime = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Normalized day time when humans start returning home (0 = midnight, 1 = next midnight).")]
        private float _eveningReturnTime = 0.75f;

        [SerializeField]
        [Tooltip("Time in seconds humans wait before starting to wander after exiting house.")]
        private float _exitToWanderDelay = 1f;

        /// <summary>
        /// Array of body prefabs with different heights.
        /// </summary>
        public HumanCharacter[] HumanPrefabs => _humanPrefabs;

        /// <summary>
        /// Reference to the BodyColorConfig ScriptableObject.
        /// </summary>
        public BodyColorConfig BodyColorConfig => _bodyColorConfig;

        /// <summary>
        /// Reference to the HeadColorConfig ScriptableObject.
        /// </summary>
        public HeadColorConfig HeadColorConfig => _headColorConfig;

        /// <summary>
        /// Reference to the HairColorConfig ScriptableObject.
        /// </summary>
        public HairColorConfig HairColorConfig => _hairColorConfig;

        /// <summary>
        /// Radius around a house where humans can spawn.
        /// </summary>
        public float SpawnRadius => _spawnRadius;

        /// <summary>
        /// Maximum number of humans that can be assigned to a single house.
        /// </summary>
        public int MaxHumansPerHouse => _maxHumansPerHouse;

        /// <summary>
        /// Minimum distance from the house center where humans can spawn.
        /// </summary>
        public float MinSpawnDistanceFromHouse => _minSpawnDistanceFromHouse;

        /// <summary>
        /// Movement speed during day (wandering phase).
        /// </summary>
        public float DayMovementSpeed => _dayMovementSpeed;

        /// <summary>
        /// Movement speed during night (returning home phase).
        /// </summary>
        public float NightMovementSpeed => _nightMovementSpeed;

        /// <summary>
        /// Maximum wander radius from spawn point during day.
        /// </summary>
        public float WanderRadius => _wanderRadius;

        /// <summary>
        /// Speed at which humans return home when lights turn on.
        /// </summary>
        public float HomeReturnSpeed => _homeReturnSpeed;

        /// <summary>
        /// Frequency of the slime jump animation (cycles per second).
        /// </summary>
        public float SlimeJumpFrequency => _slimeJumpFrequency;

        /// <summary>
        /// Amplitude of the slime jump animation (scale multiplier).
        /// </summary>
        public float SlimeJumpAmplitude => _slimeJumpAmplitude;

        /// <summary>
        /// Curve defining the shape of the slime jump animation.
        /// </summary>
        public AnimationCurve SlimeJumpCurve => _slimeJumpCurve;

        /// <summary>
        /// Normalized day time when humans start exiting houses.
        /// </summary>
        public float MorningExitTime => _morningExitTime;

        /// <summary>
        /// Normalized day time when humans start returning home.
        /// </summary>
        public float EveningReturnTime => _eveningReturnTime;

        /// <summary>
        /// Time in seconds humans wait before starting to wander after exiting house.
        /// </summary>
        public float ExitToWanderDelay => _exitToWanderDelay;

        #region Self‑Registration

        private void OnEnable()
        {
            // Register this instance as the global human config if not already set
            if (G.HumanConfig == null)
            {
                G.HumanConfig = this;
                G.EnsureSystem(nameof(HumanConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.HumanConfig == this)
                G.HumanConfig = null;
        }

        #endregion
    }
}