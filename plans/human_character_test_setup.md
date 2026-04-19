# Human Character System - Test Setup Instructions

This document provides step-by-step instructions for setting up and testing the human character system in Unity.

## Prerequisites

- Unity project with the human character system scripts already implemented
- The following scripts must be present:
  - `HumanCharacter.cs`
  - `HumanVisual.cs`
  - `HumanMovement.cs`
  - `HumanState.cs`
  - `HumanData.cs`
  - `HumanManager.cs`
  - `HumanGenerator.cs`
  - Configuration scripts (`HumanConfig.cs`, `BodyColorConfig.cs`, etc.)

## Step 1: Create Configuration Assets

The human character system requires ScriptableObject configuration assets. You can create them using the provided editor tool:

1. **Open Unity Editor** and ensure the project is loaded.
2. **Navigate to the top menu**: `Tools` → `Create Human Config Assets`
3. **Verify the assets** were created in `Assets/_Project/Configs/`:
   - `HumanConfig.asset`
   - `BodyColorConfig.asset`
   - `HeadColorConfig.asset`
   - `HairColorConfig.asset`

4. **Assign prefabs and sprites** to `HumanConfig`:
   - Open `HumanConfig.asset` in the Inspector
   - Assign body prefabs (GameObjects with SpriteRenderers)
   - Assign head sprites (Sprite assets)
   - Assign hair sprites (Sprite assets)
   - Ensure the color config references are automatically set (they should reference the newly created color configs)

## Step 2: Create Human Character Prefab

Create a prefab with all required components:

1. **Navigate to the top menu**: `Tools` → `Create Human Prefab`
2. **Verify the prefab** was created at `Assets/_Project/Prefabs/HumanCharacter.prefab`
3. **Open the prefab** in the Inspector and verify:
   - `HumanCharacter` component has references to `HumanVisual`, `HumanMovement`, and `HumanState`
   - `HumanVisual` component has references to three child SpriteRenderers (Body, Head, Hair)
   - All components are properly configured

## Step 3: Set Up a Test Scene

Create a simple test scene to verify the system:

1. **Create a new scene** (`File` → `New Scene`)
2. **Add a House object** (any GameObject with a `House` component) to serve as a home for humans
   - If you don't have a House component, create an empty GameObject and add the `House.cs` script
   - Position it at world origin (0,0,0)
3. **Add the TestHumanSpawner** to the scene:
   - Create an empty GameObject named "TestHumanSpawner"
   - Add the `TestHumanSpawner.cs` component
   - Assign the `HumanCharacter.prefab` to the `Human Prefab` field
   - Assign the House GameObject to the `Test House` field
   - (Optional) Assign a custom `HumanConfig` if you want to override the global config

## Step 4: Run the Test

1. **Enter Play Mode** in Unity
2. **Observe the console** for any errors
3. **Verify that humans spawn** around the house
4. **Check human behavior**:
   - Humans should have random appearances (body, head, hair colors)
   - Humans should animate (slime jump)
   - Humans should wander during day time (if day/night system is active)
   - Humans should return home when night falls

## Step 5: Manual Testing via Editor Menu

You can also use the editor menu items for quick testing:

- **Tools** → **Test Human System** → **Spawn Test Humans**
- **Tools** → **Test Human System** → **Clear Test Humans**

These menu items work both in Play Mode and Edit Mode (for quick setup).

## Step 6: Verify Integration with Global Systems

The human character system integrates with the global `G` service locator. Verify that:

1. **HumanManager** is registered in `G.HumanManager`
   - Check that `HumanManager` has an `Awake` method that registers itself
2. **HumanConfig** is registered in `G.HumanConfig`
   - The config assets automatically register themselves on enable
3. **Day/Night system** affects human behavior
   - Humans should change behavior based on day/night cycle (if implemented)

## Troubleshooting

### Issue: Humans not spawning
- Check that `HumanConfig` has prefabs assigned
- Verify that `TestHumanSpawner` has references to prefab and house
- Ensure `HumanConfig` asset is enabled (not empty)

### Issue: Missing component references
- Open the `HumanCharacter.prefab` and ensure all component references are set
- Run the `Create Human Prefab` tool again to regenerate

### Issue: Colors not applying
- Check that `BodyColorConfig`, `HeadColorConfig`, `HairColorConfig` have color arrays
- Verify that `HumanConfig` references these color configs

### Issue: Performance warnings
- Ensure no `GetComponent` calls in `Update` loops
- All components should cache references in `Awake` or `Start`

## Next Steps

After verifying the basic functionality:

1. **Integrate with the game's day/night cycle** - Humans should exit houses in morning and return in evening
2. **Connect with house lighting system** - Humans should react to window lights turning on/off
3. **Implement pooling** - Use `ObjectPool` for efficient human instantiation
4. **Add audio/visual feedback** - Sounds and particles for human interactions

## Additional Resources

- Architecture document: `plans/human_character_system_architecture.md`
- ScriptableObject configuration guide: `plans/scriptableobject_best_practices.md`
- Performance optimization checklist: `plans/performance_optimization.md`