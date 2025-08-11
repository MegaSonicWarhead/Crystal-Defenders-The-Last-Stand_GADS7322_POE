## Crystal Defenders: The Last Stand — Game Concept Document

Version: 1.0  
Date: 2025-08-11

### 1. Game Overview

#### 1.1 High Concept
Crystal Defenders is a strategic tower defense game where players protect a powerful crystal tower from waves of enemies using a combination of the tower’s own defenses and strategically placed defenders. Set in a procedurally generated medieval landscape, each playthrough offers a unique tactical challenge.

#### 1.2 Core Pillars
- Strategic Depth — Every defender placement matters
- Dynamic Defense — Multiple paths require constant attention
- Resource Management — Balance between building and upgrading
- Procedural Challenge — New terrain every game

### 2. Gameplay Elements

#### 2.1 The Crystal Tower
- Central Element: A mystical crystal tower serving as the player’s base
- Capabilities:
  - Auto-attacks nearby enemies
  - Substantial health pool: 1000 HP
  - Generates ambient energy (passive resource gain)
  - Visual state changes reflect health level

#### 2.2 Defenders
- Role: Primary defensive units placed by the player
- Characteristics:
  - Health: 200 HP
  - Attack Range: 5 tiles
  - Attack Speed: 2 shots/second
  - Damage: 15 per hit
  - Cost: 100 resources
- Placement Mechanics:
  - Can only be placed on designated elevated positions
  - Must have clear line of sight to paths
  - Cannot block enemy paths
  - Strategic positioning affects effectiveness

#### 2.3 Enemies
- Basic Enemy Type:
  - Health: 100 HP
  - Movement Speed: 3 tiles/second
  - Attack Damage: 10
  - Attack Range: 1 tile
  - Resource Drop: 25 on defeat
- Behavior:
  - Spawn from predetermined points
  - Navigate along paths toward the tower
  - Attack defenders in range
  - Prioritize the most direct path to the tower

### 3. World Design

#### 3.1 Procedural Terrain
- Grid-based System: 20x20 tile grid
- Key Features:
  1. Three distinct paths to the tower
  2. Variable elevation enabling tactical advantages
  3. Strategic defender placement locations
  4. Environmental obstacles

#### 3.2 Visual Style
- Medieval fantasy theme
- Clear visual distinction between:
  - Walkable paths
  - Defender placement areas
  - Obstacles
  - Tower area
- Height variations shown through terrain elevation
- Clear visibility and readability of gameplay elements

### 4. Systems Design

#### 4.1 Resource System
- Starting Resources: 300
- Income Sources:
  - Enemy defeats: +25 per kill
  - Wave completion bonus: +100
  - Passive generation: +5 per 10 seconds
- Expenditures:
  - Defender placement: 100
  - Defender repairs: 25 for 50 HP

#### 4.2 Progression System
- Wave-based Challenge:
  - Increasing enemy count per wave
  - Progressive difficulty scaling
  - Resource rewards scale with challenge
- Strategic Depth:
  - Each wave requires tactical adaptation
  - Resource management becomes more crucial
  - Defender positioning becomes more critical

### 5. User Interface

#### 5.1 HUD Elements
- Health Indicators:
  - Tower health bar (top center)
  - Defender health bars (above units)
  - Enemy health bars (above units)
- Resource Display:
  - Current resources (top right)
  - Income rate
  - Costs for actions
- Game State:
  - Wave counter
  - Enemy count
  - Score tracker

#### 5.2 Player Feedback
- Visual Feedback:
  - Attack ranges when placing defenders
  - Path highlighting on enemy approach
  - Damage numbers
  - Resource gain notifications
- Audio Cues:
  - Combat sounds
  - Resource collection
  - Wave start/end
  - Warning signals

### 6. Technical Requirements

#### 6.1 Performance Targets
- Terrain generation under 2 seconds
- Stable 60 FPS during gameplay
- Support for 50+ active units
- Quick level restart capability

#### 6.2 Platform Specifications
- Target Platform: PC (Windows)
- Minimum Requirements:
  - CPU: Dual-core 2.0 GHz
  - RAM: 4 GB
  - GPU: DirectX 11 compatible
  - Storage: 1 GB available space

### 7. Educational Value

#### 7.1 Technical Learning Outcomes
- Procedural content generation
- Pathfinding algorithms
- Game balance mathematics
- Resource system design
- Combat system implementation

#### 7.2 Assessment Criteria Alignment
- Demonstrates procedural generation expertise
- Shows understanding of game systems
- Implements complete game loop
- Provides engaging player experience
- Meets complexity requirements

### 8. Future Expansion Possibilities

#### 8.1 Potential Additions
- Multiple defender types
- Advanced enemy types
- Environmental hazards
- Power-up system
- Achievement system

#### 8.2 Scope Control
- Focus on core mechanics first
- Ensure base game is solid
- Document potential expansions
- Maintain modularity for additions
 
 ### 9. Requirements Compliance Matrix
 
 | Area | Requirement | Design Satisfaction | Acceptance Verification |
 |---|---|---|---|
 | Procedural Terrain | Mesh generated at runtime | Terrain mesh built from a procedural heightmap and triangulated at scene start/new game | Start new game: observe unique heightmap; profiler shows no pre-baked mesh assets loaded |
 | Procedural Terrain | Different every run | Perlin/Simplex noise seeded per run; seed shown in debug | Start game 3 times: terrain silhouettes differ; seed logged each run |
 | Procedural Terrain | Multiple pathways to central point | Three+ carved paths from map edges to central hub | Gizmo/debug overlay shows at least 3 distinct, traversable paths |
 | Procedural Terrain | At least three pathways | Path generator enforces count ≥ 3 | Count of path sets is ≥ 3 |
 | Tower | Placed at central meeting point | Tower spawns on hub tile where paths converge | On load, tower position equals hub tile; paths reach this tile |
 | Tower | Has health and losing condition | 1000 HP; game over at 0 HP | Reduce tower HP to 0: game transitions to Game Over state |
 | Tower | Automatically attacks | Auto-targets nearest enemy in range; periodic shots | Enemy in range receives damage ticks with no player input |
 | Defenders | Set, predetermined locations based on terrain | Defender nodes generated from elevated, non-path tiles with LOS to paths | Node gizmos appear post-generation; count logged |
 | Defenders | Cannot be placed on paths | Path tiles flagged non-placeable | Attempt place on path: placement rejected with feedback |
 | Defenders | Attack automatically | Auto-fire at enemies in range with internal cooldown | Enemy entering range takes damage without input |
 | Defenders | Have health; can be attacked/destroyed | 200 HP; enemies can damage and destroy defenders | Enemy attacks reduce defender HP; at 0 HP, unit despawns |
 | Enemies | Spawn locations based on terrain | Spawners placed at path origins on map edges | Spawner gizmos appear at path starts |
 | Enemies | Spawn at set interval | Configurable spawn cadence per spawner | Timer emits enemies at interval T ± tolerance |
 | Enemies | Move toward tower | Follow their assigned path toward hub | Enemies traverse path nodes toward hub without detours |
 | Enemies | Can attack tower and defenders | Melee range 1; damages targets on contact | Combat logs reflect damage to both unit types |
 | Enemies | Have health; can be destroyed | 100 HP; die at 0 HP and drop resources | Health bar depletes; death triggers resource gain |
 | Game Loop | Complete loop; balanced economy | Resource gains from kills, wave bonus, passive tick; spend on defenders/repairs | Player can win/lose; softlock prevention rules covered below |
 | UI/UX | Clear status, costs, health, end state | HUD shows HP bars, resources, costs; clear Game Over | Visual bars, labels, and end-state screen verified |
 | Complexity | Suitable difficulty and originality | Procedural generation, pathfinding, AI combat, economy | Feature list meets/exceeds module expectations |
 
 ### 10. Acceptance Criteria (Given/When/Then)
 
 - Procedural Terrain
   - Given a new game start, when terrain generation completes, then a new runtime mesh is created and differs from the previous run by seed.
   - Given generation completes, when visualizing path overlays, then at least three non-overlapping paths connect map edges to the hub.
 - Tower
   - Given the hub tile is computed, when the scene starts, then the tower spawns on that tile.
   - Given an enemy is within tower range, when no player input is provided, then the tower periodically damages that enemy.
   - Given tower HP reaches 0, when damage is applied, then the Game Over screen displays and input to build/place is disabled.
 - Defenders
   - Given a valid defender node, when the player spends 100 resources, then a defender is placed on that node and begins auto-attacking.
   - Given a path tile, when attempting to place a defender, then placement is rejected with a clear message and sound.
   - Given a defender is attacked, when it reaches 0 HP, then it despawns.
 - Enemies
   - Given a spawner at a path origin, when the spawn timer elapses, then an enemy spawns and begins moving along the path toward the hub.
   - Given an enemy in range of a tower or defender, when the cooldown elapses, then damage is applied to the target.
   - Given an enemy reaches 0 HP, when death occurs, then resources are awarded to the player.
 - Game Loop & Economy
   - Given enemies are defeated, when resources accumulate, then the player can purchase additional defenders or repairs within HUD constraints.
   - Given the player is out of resources, when passive income ticks occur, then resources increase by 5 every 10 seconds.
 - UI/UX
   - Given gameplay is active, when observing HUD, then tower/defender/enemy HP bars, resource totals, costs, and wave indicators are visible.
   - Given the game is paused, when the player resumes or restarts, then gameplay state properly resumes or re-initializes.
 
 ### 11. Technical Implementation Plan
 
 - Terrain Generation (Runtime)
   - Heightmap: Use Perlin/Simplex noise with randomized seed per new game. Normalize heights and apply falloff toward edges for readability.
   - Mesh: Build grid (20x20 tiles) and triangulate into a single mesh at runtime. Mark tile metadata: height, isPath, isPlaceable, hasLOS.
   - Performance: Generate under 2 seconds; cache computed noise; use burst/Jobs (if engine supports) or optimized loops.
 - Pathway Generation (≥ 3 paths)
   - Hub: Choose central region; compute exact hub tile by maximizing distance from edges or preselect grid center.
   - Sources: Select three or more edge points on different map sides.
   - Carving: Use randomized A* or multi-source BFS on the height grid with a cost that prefers lower slopes; widen paths to N tiles.
   - Validation: Ensure each path reaches the hub; prevent full overlap except at hub; mark tiles `isPath=true`.
 - Tower Placement & Combat
   - Place tower on hub tile; set HP=1000. Auto-target nearest enemy within range R using spatial query; fire every S seconds, dealing D damage.
 - Defender Nodes & Placement
   - Node selection: Sample elevated, non-path tiles within LOS to paths; ensure spacing and a fixed count (e.g., 12–20 nodes).
   - LOS: Grid raycast (Bresenham) or engine raycast to any path tile; precompute boolean `hasLOS` for candidate tiles.
   - Placement rules: Only on nodes; block if on path or insufficient resources; cost=100; repairs=25 for +50 HP.
 - Enemy Spawners & Behavior
   - Spawners: Place at each path origin; interval-based spawning with configurable cadence.
   - Movement: Follow path waypoints toward hub; attack defenders/tower at range 1 when in contact; damage=10; HP=100.
 - Economy & Waves
   - Rewards: +25 per enemy kill; +100 per wave; +5 every 10s passive.
   - Scaling: Increase enemy count per wave; adjust spawn intervals slightly to escalate difficulty.
 - UI/UX
   - HUD: Top-center tower HP; unit HP bars; top-right resources and income; costs visible on build UI; wave and enemy counters.
   - Feedback: Placement previews with range circles; damage numbers; resource popups; audio for key events.
 - Restart/Performance
   - Restart fully re-seeds terrain; reuse allocs to avoid GC spikes; target 60 FPS with ≤ 2 ms generation spikes off main loop.
 
 ### 12. Test & QA Plan
 
 - Generation Tests: Seeded runs produce identical terrain; unseeded runs differ. Path count ≥ 3 and reach hub.
 - Rule Tests: Placement forbidden on path; enemies respect path; defenders and tower auto-attack without input.
 - Combat/Economy: Damage, cooldowns, rewards numbers match specs; passive income ticks on schedule.
 - UI/UX: All HUD elements visible and updating; pause/restart works; Game Over triggers correctly at 0 HP.
 - Performance: Terrain generation < 2s; 60 FPS with 50+ active units in a stress scene.
