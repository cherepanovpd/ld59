// Path: Assets/_Project/Scripts/Human/HumanBehaviorSystem.cs

using System.Collections.Generic;
using Core;
using Project.Core.Events;
using Project.DayNight;
using Project.Human.Config;
using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Central system that manages behavior state transitions for all human characters.
    /// Subscribes to day/night events and window lighting events to trigger appropriate behavior changes.
    /// Registers with the 'G' service locator and works with HumanManager to update human states.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanBehaviorSystem : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logTransitions = false;

        // Tracking for house assignments (optional)
        private readonly Dictionary<House, List<HumanCharacter>> _houseAssignments = new Dictionary<House, List<HumanCharacter>>();

        #region Unity Lifecycle

        private void Awake()
        {
            CacheReferences();
            RegisterWithG();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            UnregisterFromG();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Transition all humans to a specific behavior state.
        /// </summary>
        public void TransitionAllToState(BehaviorState state)
        {
            if (G.HumanManager== null)
                return;

            var humans = G.HumanManager.GetAllHumans();
            foreach (var human in humans)
            {
                var stateComponent = human?.GetComponent<HumanState>();
                if (stateComponent != null)
                {
                    stateComponent.TransitionTo(state);
                }
            }

            if (_logTransitions)
                Debug.Log($"[HumanBehaviorSystem] Transitioned all humans to {state}");
        }

        /// <summary>
        /// Transition humans belonging to a specific house to a behavior state.
        /// </summary>
        public void TransitionHouseToState(House house, BehaviorState state)
        {
            if (house == null || !_houseAssignments.TryGetValue(house, out var humans))
                return;

            foreach (var human in humans)
            {
                var stateComponent = human?.GetComponent<HumanState>();
                if (stateComponent != null)
                {
                    stateComponent.TransitionTo(state);
                }
            }
        }

        /// <summary>
        /// Register a human with a house for targeted behavior updates.
        /// Called by HumanGenerator when a human is created.
        /// </summary>
        public void RegisterHumanWithHouse(HumanCharacter human, House house)
        {
            if (house == null || human == null)
                return;

            if (!_houseAssignments.ContainsKey(house))
                _houseAssignments[house] = new List<HumanCharacter>();

            var list = _houseAssignments[house];
            if (!list.Contains(human))
                list.Add(human);
        }

        /// <summary>
        /// Unregister a human from house tracking.
        /// Called when a human is destroyed or recycled.
        /// </summary>
        public void UnregisterHuman(HumanCharacter human)
        {
            foreach (var kvp in _houseAssignments)
            {
                kvp.Value.Remove(human);
            }
        }

        #endregion

        #region Event Handlers

        private void OnDayPhaseChanged(DayPhaseChangedEvent e)
        {
            if (_logTransitions)
                Debug.Log($"[HumanBehaviorSystem] Day phase changed to {e.NewPhase}");

            switch (e.NewPhase)
            {
                case DayPhaseChangedEvent.Phase.Dawn:
                    // Morning: humans exit homes
                    TransitionAllToState(BehaviorState.ExitingHome);
                    break;

                case DayPhaseChangedEvent.Phase.Day:
                    // Day: humans wander with slime jump animation
                    TransitionAllToState(BehaviorState.Wandering);
                    break;

                case DayPhaseChangedEvent.Phase.Dusk:
                    // Dusk: start returning when lights turn on (handled by WindowLightsChangedEvent)
                    // Optionally, we could start returning home gradually
                    TransitionAllToState(BehaviorState.ReturningHome);
                    break;

                case DayPhaseChangedEvent.Phase.Night:
                    // Night: ensure all humans are at home
                    // If any humans are still outside, force them home
                    TransitionAllToState(BehaviorState.AtHome);
                    break;
            }
        }

        private void OnWindowLightsChanged(WindowLightsChangedEvent e)
        {
            if (_logTransitions)
                Debug.Log($"[HumanBehaviorSystem] Window lights changed: LightsOn={e.LightsOn}, Hour={e.Hour}");

            // When lights turn on at dusk/night, humans should return home
            if (e.LightsOn && G.DayNightSystem != null)
            {
                var currentPhase = G.DayNightSystem.CurrentPhase;
                if (currentPhase == DayPhaseChangedEvent.Phase.Dusk || currentPhase == DayPhaseChangedEvent.Phase.Night)
                {
                    TransitionAllToState(BehaviorState.ReturningHome);
                }
            }
            // When lights turn off during day, humans may start wandering (optional)
            else if (!e.LightsOn && G.DayNightSystem != null)
            {
                var currentPhase = G.DayNightSystem.CurrentPhase;
                if (currentPhase == DayPhaseChangedEvent.Phase.Day)
                {
                    // Lights off during day could be a power outage? Not needed for now.
                }
            }
        }

        #endregion

        #region Private Helpers

        private void CacheReferences()
        {
            if (G.HumanConfig == null)
                G.HumanConfig = G.HumanConfig;
        }

        private void RegisterWithG()
        {
            if (G.HumanBehaviorSystem == null)
            {
                G.HumanBehaviorSystem = this;
                G.EnsureSystem(nameof(HumanBehaviorSystem), this);
            }
            else
            {
                Debug.LogWarning("[HumanBehaviorSystem] G.HumanBehaviorSystem already occupied by another instance. Not registering.", this);
            }
        }

        private void UnregisterFromG()
        {
            if (G.HumanBehaviorSystem == this)
                G.HumanBehaviorSystem = null;
        }

        private void SubscribeToEvents()
        {
            if (G.HasEvents())
            {
                G.Events.Subscribe<DayPhaseChangedEvent>(OnDayPhaseChanged, owner: this);
                G.Events.Subscribe<WindowLightsChangedEvent>(OnWindowLightsChanged, owner: this);
            }
            else
            {
                Debug.LogWarning("[HumanBehaviorSystem] EventSystem not available. Will not receive day/night updates.", this);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (G.HasEvents())
            {
                G.Events.UnsubscribeAll(this);
            }
        }

        #endregion
    }
}