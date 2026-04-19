# Human Character System - Usage and Testing Procedures

## Overview

The Human Character System is a modular, data-driven system for generating, managing, and controlling human characters in the game. It follows SOLID principles, uses ScriptableObjects for configuration, and integrates with the global `G` service locator.

## Architecture Summary

The system consists of the following key components:

### Core Components
1. **HumanCharacter** - Main MonoBehaviour that orchestrates visual, movement, and state components
2. **HumanVisual** - Handles sprite assembly, coloring, and animation
3. **HumanMovement** - Manages movement logic (wandering, returning home)
4. **HumanState** - Tracks behavior state (AtHome, ExitingHome, Wandering, ReturningHome)
5. **HumanData** - Data container for appearance (body prefab, head/hair sprites, colors)

### Management Systems
1. **HumanManager** - Central manager for all human characters, handles pooling and batch updates
2. **HumanBehaviorSystem** - Orchestrates behavior transitions based on day/night and lighting events
3. **HumanGenerator** - Generates humans for houses based on configuration

### Configuration (ScriptableObjects)
1. **HumanConfig** - Main configuration (prefabs, sprites, movement settings, timing)
2. **BodyColorConfig**, **HeadColorConfig**, **HairColorConfig** - Color palettes for random selection

## Setup Instructions

### 1. Initial Setup
1. **Create Configuration Assets**:
   - Use `Tools → Create Human Config Assets` to generate the four ScriptableObjects
   - Assign body prefabs, head sprites, and hair sprites to `HumanConfig`
   - Adjust color palettes in the color configs if desired

2. **Create Human Prefab**:
   - Use `Tools → Create Human Prefab` to generate a prefab with all required components
   - Verify the prefab structure:
     - HumanCharacter (root)
     - HumanVisual, HumanMovement, HumanState components attached
     - Three child GameObjects: Body, Head, Hair (each with SpriteRenderer)

3. **Add HumanManager to Scene**:
   - Create an empty GameObject named "HumanManager"
   - Add `HumanManager.cs` component
   - Assign the `HumanConfig` asset (optional - will use G.HumanConfig if null)
   - Configure pool settings (initial size, max size)

4. **Add HumanBehaviorSystem to Scene**:
   - Create an empty GameObject named "HumanBehaviorSystem"
   - Add `HumanBehaviorSystem.cs` component
   - It will automatically register with `G` and subscribe to day/night events

### 2. Integration with Other Systems

#### Day/Night Cycle
The human behavior system responds to `DayPhaseChangedEvent` and `WindowLightsChangedEvent`:
- **Dawn**: Humans exit homes (transition to ExitingHome)
- **Day**: Humans wander with slime jump animation (Wandering)
- **Dusk/Night + Lights On**: Humans return home (ReturningHome)
- **Night**: All humans should be at home (AtHome)

Ensure that:
- `DayNightSystem` is present and registered in `G.DayNightSystem`
- `EventSystem` is present and registered in `G.Events`
- `WindowLightManager` is present and fires `WindowLightsChangedEvent`

#### House System
Each human is assigned to a `House` object. The `HumanGenerator` creates humans for houses based on:
- `HumanConfig.MaxHumansPerHouse` - maximum humans per house
- `HumanConfig.SpawnRadius` - spawn radius around house

To assign a human to a house manually:
```csharp
humanCharacter.Initialize(humanData, house, config);
```

### 3. Runtime Usage

#### Spawning Humans Programmatically
```csharp
// Get references
HumanManager manager = G.HumanManager;
House targetHouse = ...;

// Spawn a human at the house
HumanCharacter human = manager.SpawnHumanAtHouse(targetHouse);

// Or spawn with specific data
HumanData data = HumanData.CreateRandom(G.HumanConfig);
HumanCharacter human = manager.SpawnHuman(data, targetHouse);
```

#### Controlling Behavior
```csharp
// Transition a human to specific state
human.State.TransitionTo(BehaviorState.Wandering);

// Use HumanBehaviorSystem for batch transitions
G.HumanBehaviorSystem.TransitionAllToState(BehaviorState.ReturningHome);
```

#### Customizing Appearance
```csharp
// Create custom human data
HumanData data = new HumanData
{
    BodyPrefab = bodyPrefab,
    HeadSprite = headSprite,
    HairSprite = hairSprite,
    BodyColor = Color.red,
    HeadColor = Color.white,
    HairColor = Color.blue
};

// Apply to existing human
human.Visual.Initialize(data);
```

## Testing Procedures

### Unit Testing (Manual)

#### 1. Visual Assembly Test
- **Objective**: Verify humans are assembled correctly with body, head, hair
- **Steps**:
  1. Spawn a human using `TestHumanSpawner`
  2. Inspect the GameObject hierarchy in Scene view
  3. Verify all three sprite renderers are present and layered correctly
  4. Check that colors are applied (not all white)

#### 2. Movement Test
- **Objective**: Verify humans move correctly between states
- **Steps**:
  1. Spawn a human
  2. Use debug menu to transition to Wandering state
  3. Observe human moving to random positions within wander radius
  4. Transition to ReturningHome and verify human moves toward house

#### 3. State Transition Test
- **Objective**: Verify behavior states trigger appropriate actions
- **Steps**:
  1. Spawn multiple humans
  2. Use `G.HumanBehaviorSystem.TransitionAllToState()` to cycle through states
  3. Verify visual feedback (animation changes, movement)

#### 4. Day/Night Integration Test
- **Objective**: Verify humans respond to day/night cycle
- **Steps**:
  1. Set up a scene with DayNightSystem and HumanBehaviorSystem
  2. Enter Play Mode
  3. Use DayNightSystem debug controls to change time
  4. Observe humans exiting homes at dawn, wandering during day, returning at dusk

#### 5. Performance Test
- **Objective**: Ensure no GC allocations during gameplay
- **Steps**:
  1. Open Unity Profiler (CPU and Memory)
  2. Spawn 50+ humans
  3. Let system run for 60 seconds
  4. Check for GC allocations in Update loops
  5. Verify frame rate remains stable

### Automated Testing (Editor)

Use the provided test menu items:
- `Tools/Test Human System/Spawn Test Humans` - Spawns configured number of humans
- `Tools/Test Human System/Clear Test Humans` - Clears all spawned humans

### Debug Visualization

Enable debug info in components:
- **HumanManager**: Set `Show Debug Info` to true for console logs
- **HumanBehaviorSystem**: Set `Log Transitions` to true to see state changes
- **HumanMovement**: View target positions with Gizmos (enable in Scene view)

## Troubleshooting Guide

### Common Issues

#### Issue: Humans not appearing
- **Check**: HumanConfig has prefabs assigned
- **Check**: HumanManager's pool is initialized (check logs)
- **Check**: Human prefab has all required components

#### Issue: Colors are all white
- **Check**: Color config assets are assigned to HumanConfig
- **Check**: Color configs have non-empty color arrays
- **Check**: HumanData generation is using configs (not default colors)

#### Issue: Humans not moving
- **Check**: HumanMovement component is attached and enabled
- **Check**: HumanState is in a moving state (Wandering, ReturningHome)
- **Check**: Movement speed is > 0

#### Issue: Day/Night events not triggering behavior
- **Check**: HumanBehaviorSystem is registered in G.HumanBehaviorSystem
- **Check**: EventSystem is present (G.Events)
- **Check**: DayNightSystem is firing DayPhaseChangedEvent

#### Issue: Performance problems with many humans
- **Check**: ObjectPool is being used (HumanManager uses pooling)
- **Check**: No GetComponent calls in Update loops
- **Check**: All components cache references in Awake
- **Check**: HumanManager batch updates are efficient

## Performance Best Practices

The system is designed with performance in mind:

1. **Zero Allocations in Loops**:
   - No `new` object creation in Update/FixedUpdate
   - No LINQ in hot paths
   - String concatenation avoided

2. **Caching**:
   - All components cache `transform` reference in Awake
   - HumanManager caches list of active humans
   - Color configs cache color arrays

3. **Batch Processing**:
   - HumanManager updates all humans in a single loop
   - HumanBehaviorSystem processes transitions in batches

4. **Object Pooling**:
   - HumanManager uses `ObjectPool` for human instantiation
   - Humans implement `IPoolable` for proper lifecycle management

## Extension Points

### Adding New Behavior States
1. Extend `BehaviorState` enum in `HumanState.cs`
2. Add transition logic in `HumanState.TransitionTo()`
3. Update `HumanMovement` to support new movement patterns
4. Update `HumanBehaviorSystem` to trigger new states

### Custom Visual Styles
1. Add new body prefabs to `HumanConfig.BodyPrefabs`
2. Add new head/hair sprites to respective arrays
3. Extend `HumanVisual` to support additional renderers (e.g., clothing)

### Integration with New Systems
1. Subscribe to new events in `HumanBehaviorSystem.SubscribeToEvents()`
2. Add new configuration sections to `HumanConfig`
3. Create new manager systems that register with `G`

## Conclusion

The Human Character System provides a robust, performant foundation for human NPCs in the game. By following the setup and testing procedures outlined above, you can ensure the system works correctly and integrates seamlessly with other game systems.

For further details, refer to:
- `plans/human_character_system_architecture.md` - System design and architecture
- `plans/human_character_test_setup.md` - Step-by-step test setup
- Individual script comments - Detailed API documentation