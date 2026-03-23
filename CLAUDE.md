# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**MoleMan** is a 3D dungeon crawler action game built in Unity 6000.0.61f1. This is a school project featuring procedural dungeon generation, first-person combat, enemy AI, and an inventory system with both temporary (dungeon run) and persistent (meta-progression) components.

## Development Commands

### Unity Editor
- Open the project in Unity Hub with Unity 6000.0.61f1
- Main development scene: [Assets/Scenes/DungeonTest2.unity](Assets/Scenes/DungeonTest2.unity)
- Press Play in editor to test
- Debug controls: Alpha1 key triggers enemy attacks, Tab shows inventory

### Testing
The project uses Unity Test Framework (package version 1.6.0). Run tests via:
- Unity Editor: Window > General > Test Runner
- Command line: `Unity.exe -runTests -projectPath . -testResults results.xml`

### Build
Standard Unity build process via File > Build Settings or:
```
Unity.exe -quit -batchmode -projectPath . -buildWindows64Player build/MoleMan.exe
```

## Architecture

### Player System
- [PlayerController.cs](Assets/Scripts/PlayerController.cs) - First-person movement with SphereCast ground detection (more reliable than CharacterController.isGrounded)
- [PlayerAttack.cs](Assets/Scripts/PlayerAttack.cs) - Animation-driven combat, 0.78s attack cooldown
- [PlayerState.cs](Assets/Scripts/PlayerState.cs) - Health and invincibility frames

### Enemy AI System
- [Enemy.cs](Assets/Scripts/Enemy.cs) - State machine with visual states via child GameObjects (not Animator). Four attack types: Melee, Projectile, Lunging, Unique (via `IUniqueAttack` interface)
- [Strafer.cs](Assets/Scripts/Strafer.cs) - Three movement modes: Default (approach/retreat), Strafe (circle player), Sneak (flank at camera angles)
- [EnemyAttack.cs](Assets/Scripts/EnemyAttack.cs) - Serializable data class (not MonoBehaviour) for inline inspector editing
- [AttackHitbox.cs](Assets/Scripts/AttackHitbox.cs) - Trigger-based damage with one-hit-per-activation

### Inventory & Items
- [Inventory.cs](Assets/Scripts/Inventory.cs) - Dictionary-based with dual inventories (temporary dungeon + persistent meta)
- [Item.cs](Assets/Scripts/Item.cs) - ScriptableObject item definitions (create via Assets > Create > Scriptable Objects > Item)
- [MaterialDrop.cs](Assets/Scripts/MaterialDrop.cs) - Auto-collect pickups with magnet effect

### Dungeon Generation (Custom System)
- [DungeonBuilder.cs](Assets/Scripts/DungeonBuilder.cs) - Grid-based procedural generation (main path → branch rooms → extension rooms → room type replacement → extra connections)
- [RoomType.cs](Assets/Scripts/RoomType.cs) - ScriptableObject defining room properties, constraints (distance from start/boss, max connections), and prefab pools
- [DoorConnector.cs](Assets/Scripts/DoorConnector.cs) - Connection points between rooms/hallways, manages door/wall/locked states
- [RoomPopulator.cs](Assets/Scripts/RoomPopulator.cs) - Spawns enemies by difficulty budget or random, builds runtime NavMesh
- [RoomStarter.cs](Assets/Scripts/RoomStarter.cs) - Activates room when player enters, tracks grid position
- [FloorChecker.cs](Assets/Scripts/FloorChecker.cs) - Detects player entry, triggers room activation and minimap updates
- Room prefabs in [Assets/Rooms/RoomPrefabs/](Assets/Rooms/RoomPrefabs/)
- Room types defined as ScriptableObjects in [Assets/Rooms/](Assets/Rooms/) (StartRoom, NormalRoom, KeyRoom, BossRoom, ItemRoom)

### Minimap System
- [MinimapManager.cs](Assets/Scripts/MinimapManager.cs) - Generates grid-based UI minimap from floorplan, reveals rooms as player explores, highlights current room
- Minimap prefab: [Assets/Prefabs/UI/MiniMap.prefab](Assets/Prefabs/UI/MiniMap.prefab)
- Room square prefab: [Assets/Prefabs/UI/RoomSquare.prefab](Assets/Prefabs/UI/RoomSquare.prefab)
- Updates via coroutine polling RoomStarter.hasActivated

### Door System
- [LockedDoor.cs](Assets/LockedDoor.cs) - Key-based locked door with animation, consumes key item from inventory on unlock
- DoorConnector manages three states per connection: Open (doorObject), Closed (wallObject), Locked (lockedObject)
- Boss room connection set to locked state during dungeon generation

### Input
- New Input System configured ([InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions))
- Note: Player scripts currently use legacy Input class despite configuration

## Key Technical Decisions

1. **Custom Dungeon Generation**: Grid-based with constraint satisfaction (replaced Dungeon Architect)
   - Phase-based generation: main path → branches → extensions → room type placement → extra connections
   - Room placement uses rotation testing and overlap detection via floor colliders
   - Distance constraints for special rooms (key/item rooms) using BFS pathfinding
   - Connector-based snapping system for rooms and hallways
2. **Runtime NavMesh**: RoomPopulator builds NavMesh per-room after enemy spawn (not pre-baked)
3. **Custom Ground Detection**: SphereCast instead of CharacterController.isGrounded
4. **Visual State Management**: Child GameObjects with materials, not Animator states
5. **Attack as Data**: EnemyAttack is [Serializable] class for inline inspector editing
6. **Interface-based Extension**: `IUniqueAttack` for custom enemy attacks
7. **Coroutine-based State**: Attack sequences use coroutines for timing
8. **Component References**: FindGameObjectWithTag pattern for Player, MainCamera, GameManager
9. **ScriptableObject-based Design**: Room types and items defined as ScriptableObjects for designer control

## Code Conventions

- **Naming**: PascalCase for public fields, camelCase for private fields
- **Inspector Organization**: Use NaughtyAttributes (BoxGroup, Foldout, ShowIf) for grouping, [SerializeField] for private visibility
- **Component References**: Cache components in Start() after GetComponent calls
- **Singleton Access**: Use FindGameObjectWithTag for Player, MainCamera, GameManager
- **Audio Volume**: All audio clips have separate [Range(0,1)] volume parameters



### Asset Store Assets
- NaughtyAttributes - Inspector enhancements ([BoxGroup], [ShowIf], [Button], [MinMaxSlider], etc.)
- MoreMountains Feedbacks - Audio/visual feedback system


## Interaction Guidelines for Claude Code

**This is a learning-focused project. The user has moderate Unity coding experience and wants teaching assistance, not complete solutions.**

### Teaching Approach
- **Do not write code blocks unless specifically requested** - Teach concepts, introduce relevant functions, provide instructions
- User knows basics (coroutines, methods, variables, common functions) - Skip fundamental explanations
- Introduce concepts and Unity-specific functions first, provide specific instructions only if needed
- Avoid repetition - Reference previous messages ("See #x two messages ago") only when warranted
- Assume information has been received unless clarification is specifically requested

### Communication Style
- **Be concise and brief** while still providing useful information
- **Be objective, not sycophantic** - Make honest technical judgments, not excessive praise
- Compliments only when deserved and not excessively
- Avoid over-explaining - Match existing code formatting and comment style when editing

### Technical Standards
- **Follow DRY, KISS, YAGNI principles** - Keep things simple while accomplishing the desired effect
- Don't over-optimize for a small student project - Reasonable trade-offs are acceptable if they don't significantly impact performance
- **Use Unity 6 documentation** - Do not predict method names; look up Unity 6 specifics or ask for clarification
- Suggest better script organization when appropriate (separate scripts, different existing scripts, alternative approaches)

### Code Formatting
- **Null checks**: Format as `"X not assigned"`
- **Debug logs**: Avoid Update() logs that flood the console
- Match existing code style, formatting, and naming conventions for consistency

### Token Management
- **ALWAYS prefer subagents over direct tools**: Use Task tool with subagent_type='Explore' for codebase exploration, searches, and research tasks
- **Direct tool usage ONLY for specific known targets**: Use Read, Grep, Glob directly only when you have exact file paths or patterns
- **Context preservation**: Subagents consume their own token budget, keeping main conversation context clean
- **When to use subagents**:
  - Exploring unfamiliar systems or architecture
  - Searching for patterns across multiple files
  - Researching how features work
  - Any task requiring multiple search iterations
  - Gathering information before making changes
- **When direct tools are acceptable**:
  - Reading a specific file the user mentioned
  - Editing files you already understand
  - Simple single-file operations
