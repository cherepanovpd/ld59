# Currency Generation System - Implementation Guide

## Overview
This guide explains how to set up and use the currency generation system implemented for your Unity game. The system allows houses to generate bills when a signal ring passes through them, with bills flying to the currency counter when hovered.

## Files Created

### Core Scripts
1. **`Assets/_Project/Scripts/Currency/Config/CurrencyConfig.cs`**
   - ScriptableObject configuration for currency generation
   - Create asset via: Right-click in Project window → Create → Game → Currency Config
   - Configure bill count/value ranges, flight settings, animations

2. **`Assets/_Project/Scripts/Currency/BillManager.cs`**
   - Central orchestrator for bill lifecycle
   - Self-registers as `G.BillManager`
   - Manages object pooling (simplified version)

3. **`Assets/_Project/Scripts/Currency/BillController.cs`**
   - Attached to each bill prefab
   - Handles states: Idle, Hovered, Flying, Collected
   - Manages animations (pulse, flicker, flight)

4. **`Assets/_Project/Scripts/Currency/CurrencyCounter.cs`**
   - Tracks total currency with smooth increment animations
   - Self-registers as `G.Currency`
   - Requires UI references (TextMeshPro text, Image icon)

5. **`Assets/_Project/Scripts/Currency/HouseCurrencyGenerator.cs`**
   - Attached to house prefabs
   - Detects signal ring collisions via `OnTriggerEnter2D`
   - Triggers house animation and bill generation

6. **`Assets/_Project/Scripts/Currency/HouseAnimator.cs`**
   - Optional separate component for house spring animation
   - Can be used independently

### Updated Files
1. **`Assets/_Project/Scripts/Core/G.cs`**
   - Added three new static properties:
     - `CurrencyConfig` (ScriptableObject)
     - `BillManager` (BillManager instance)
     - `Currency` (CurrencyCounter instance)
   - Added corresponding null-check helpers
   - Updated `Clear()` method

## Setup Steps

### Step 1: Create CurrencyConfig Asset
1. Right-click in Project window → Create → Game → Currency Config
2. Name it `CurrencyConfig` and save to `Assets/_Project/Configs/`
3. Configure the settings:
   - **Bill Generation**: Set count range (1-3) and value range (10-100)
   - **Flight Settings**: Adjust duration, height, curve
   - **Visual Settings**: Assign bill prefab (create in Step 2)
   - **Light2D Settings**: Configure flicker intensity and speed

### Step 2: Create Bill Prefab
1. Create empty GameObject named "Bill"
2. Add components:
   - `SpriteRenderer` (assign dollar bill sprite from `Assets/_Project/Sprites/UI/`)
   - `CircleCollider2D` (set as Trigger, adjust radius)
   - `Light2D` (Point Light, for glow effect)
   - `BillController` script
3. Configure `BillController`:
   - Assign component references
   - Set `Collider Radius Multiplier` to 1.5 (makes collider larger than sprite)
4. Save as prefab: `Assets/_Project/Prefabs/Currency/Bill.prefab`
5. Assign this prefab to `CurrencyConfig.BillPrefab`

### Step 3: Set Up Currency Counter UI
1. Create or locate your currency display UI (Canvas with TextMeshPro and Image)
2. Add `CurrencyCounter` component to the UI GameObject
3. Assign references:
   - `Currency Text`: TextMeshPro text displaying amount
   - `Currency Icon`: Image of dollar bill icon
   - `Counter Position`: RectTransform where bills should fly to (optional)

### Step 4: Set Up BillManager
1. Create empty GameObject named "BillManager" in scene
2. Add `BillManager` component
3. Assign `CurrencyConfig` reference (or leave null to use `G.CurrencyConfig`)
4. Configure pool settings (initial size: 20, max size: 100)

### Step 5: Modify House Prefabs
1. Open each house prefab in `Assets/_Project/Prefabs/House/`
2. Add `HouseCurrencyGenerator` component
3. Configure:
   - `Collider`: Should be a trigger collider (CircleCollider2D recommended)
   - `House Visual`: Transform to animate (usually the house root)
   - `Signal Ring Layer`: Set to match your signal ring's layer
4. Ensure the house has a Collider2D (trigger) for detection

### Step 6: Configure Signal Rings
1. Ensure `TowerSignalRing` prefab has:
   - `CircleCollider2D` as trigger
   - Layer set to match `HouseCurrencyGenerator.SignalRingLayer`
   - Tag "SignalRing" (optional but recommended)

## Integration with Existing Systems

### 1. Audio Integration
The system calls `G.Audio.PlaySFX()` for:
- House generation: "HouseGenerate"
- Bill collection: "CurrencyCollect"

Ensure these SFX are defined in your `AudioManager`.

### 2. Object Pool Integration
The system uses the existing `ObjectPool` class (`G.Pool`). For optimal performance:
- Extend `BillManager` to use proper pooling (current implementation is simplified)
- Consider implementing `IPoolable` interface in `BillController`

### 3. Day/Night System
No direct dependencies, but consider:
- Adjusting bill light intensity based on time of day
- Modifying generation rates based on day/night cycle

## Testing

### Quick Test Commands
1. **Test Bill Generation**: Select `BillManager` in scene, use Context Menu → "Generate Test Bills"
2. **Test Currency Add**: Select `CurrencyCounter`, use Context Menu → "Add 100 Currency"
3. **Test House Generation**: Select a house with `HouseCurrencyGenerator`, use Context Menu → "Test Generation"

### Manual Testing
1. Enter Play mode
2. Ensure `G` has all currency systems registered (check console for warnings)
3. Trigger a signal ring to pass through a house
4. Verify bills appear and animate
5. Hover mouse over a bill to collect it
6. Verify currency counter updates smoothly

## Performance Considerations

### Implemented Optimizations
1. **Zero allocations in Update loops**: All animations use cached DOTween sequences
2. **Component caching**: All `GetComponent<>()` calls in `Awake()`
3. **Object pooling**: Bills are managed through pool (simplified implementation)

### Recommended Further Optimizations
1. **Implement proper pooling**: Extend `BillManager` to use the existing `ObjectPool` system
2. **Batch updates**: If many bills are active, consider updating them in batches
3. **LOD system**: Reduce animation complexity for distant bills
4. **Pool warming**: Preload bill pool during loading screens

## Troubleshooting

### Common Issues

1. **Bills not generating**
   - Check `HouseCurrencyGenerator` collider is trigger
   - Verify signal ring layer matches
   - Ensure `BillManager` is registered in `G`

2. **Bills not flying to counter**
   - Check `CurrencyCounter` is registered in `G`
   - Verify `BillController` can find target position
   - Ensure mouse events are working (requires `Camera` with Physics Raycaster)

3. **Currency counter not updating**
   - Verify `CurrencyCounter` has `TextMeshPro` reference
   - Check `G.Currency` is not null
   - Ensure `AddCurrency` is being called

4. **Performance issues with many bills**
   - Reduce pool size in `BillManager`
   - Simplify bill animations
   - Implement proper object pooling

## Extension Points

### 1. Custom Bill Calculators
Replace the random value generation in `CurrencyConfig.GetRandomBillValue()` with:
- Formula-based calculations
- House value multipliers
- Time-of-day modifiers

### 2. Advanced Flight Paths
Modify `BillController.StartFlightToCounter()` to support:
- Different curve types (Bezier, Catmull-Rom)
- Obstacle avoidance
- Multi-stage flights

### 3. Visual Variants
Add to `CurrencyConfig.BillSprites` array:
- Different bill denominations ($1, $5, $20, $100)
- Worn vs. new bill variants
- Themed bills for special events

### 4. Save System Integration
Extend `CurrencyCounter` to:
- Save/load currency amount via `G.Save`
- Track lifetime earnings
- Achievements based on milestones

## Next Steps

### Phase 2: Polish
1. Create particle effects for bill generation/collection
2. Add screen shake for large currency amounts
3. Implement sound effects for all interactions
4. Create visual feedback for house generation

### Phase 3: Advanced Features
1. Implement bill stacking (multiple bills fly together)
2. Add bill combos (bonus for collecting multiple quickly)
3. Create currency multipliers (power-ups, upgrades)
4. Add bill physics (wind effects, collisions)

## Support
Refer to the architecture document `plans/currency_generation_architecture.md` for detailed system design and class diagrams.