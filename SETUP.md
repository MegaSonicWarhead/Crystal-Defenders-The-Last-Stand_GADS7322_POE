## Crystal Defenders — Unity Setup Guide

### Overview
This guide explains how to wire the provided scripts and your Kenney assets into a working scene. Follow it top-to-bottom when setting up a new scene.

### 1) Import and Materials
- Import your Kenney assets into `Assets/Models/`.
- Create `Assets/Materials/TerrainMat` and assign:
  - Albedo: `tile` (or `snow-tile`)
  - Normal (optional): `tile-bump` (or `snow-tile-bump`)
  - Assign `TerrainMat` to the `MeshRenderer` on the procedural terrain object (created below).

### 2) Create Prefabs

#### Enemy (UFO)
1. Create an empty GameObject named `Enemy_UFO` and reset transform.
2. Add `Enemy` (auto-adds `Health`).
3. Make the visual model a child:
   - Use one of: `enemy-ufo-a`, `enemy-ufo-b`, `enemy-ufo-c`, `enemy-ufo-d`.
   - Optional child: matching weapon (`enemy-ufo-*-weapon`).
   - Adjust the child’s local rotation/scale (keep the root clean).
4. Save as a prefab (e.g., `Assets/Prefabs/Enemy_UFO.prefab`).

#### Defender (Turret/Ballista/Cannon)
1. Create an empty GameObject named `Defender_Turret` and reset transform.
2. Add `Defender` (auto-adds `Health` and `AutoAttack`).
3. Add child visuals:
   - Base: e.g., `tower-round-build-a` or `tower-square-build-a` (or `wood-structure`).
   - Weapon: one of `weapon-turret`, `weapon-ballista`, or `weapon-cannon` as a child of the base top.
4. Save as a prefab (e.g., `Assets/Prefabs/Defender_Turret.prefab`).

#### Tower (Base you defend)
1. Create an empty GameObject named `Tower_Base` and reset transform.
2. Add `Tower` (auto-adds `Health` and `AutoAttack`).
3. Stack child visuals (pick matching family):
   - Round example: `tower-round-bottom-a` → `tower-round-middle-b` → `tower-round-roof-a` → `tower-round-top-b` (+ `tower-round-crystals` optional).
   - Square example: `tower-square-bottom-a` → `tower-square-middle-b` → `tower-square-roof-a` → `tower-square-top-b`.
4. Save as a prefab (e.g., `Assets/Prefabs/Tower_Base.prefab`).

#### PlacementNode (Clickable build spot)
1. Create an empty GameObject named `PlacementNode` and reset transform.
2. Add `PlacementNode`.
3. Add a `BoxCollider` or `CapsuleCollider` (required for `OnMouseDown`).
4. Add a child visual marker: `selection-a` (or a small `tile`).
5. Save as a prefab (e.g., `Assets/Prefabs/PlacementNode.prefab`).

#### EnemySpawner (Logic only)
1. Create an empty GameObject named `EnemySpawner`.
2. Add `EnemySpawner`.
3. Optional child visual: `spawn-round` or `tile-spawn`.
4. Save as a prefab (e.g., `Assets/Prefabs/EnemySpawner.prefab`).

### 3) Build the Scene

#### Procedural Terrain
1. Create `GameObject` named `ProceduralTerrain`.
2. Add `ProceduralTerrainGenerator` (auto-adds `MeshFilter` + `MeshRenderer`).
3. Assign `Materials/TerrainMat` to the `MeshRenderer`.
4. Optional: tweak `gridWidth`, `gridHeight`, `tileSize`, `minPathCount`.

#### Visual Path Tiles (optional)
If you want visible tiles along the generated paths and markers at spawns/hub:
1. Create `GameObject` named `PathTiles`.
2. Add `PathTileDecorator`.
3. Assign:
   - `generator`: the `ProceduralTerrain` object
   - `pathTilePrefab`: a tile prefab (e.g., `tile-straight` or `snow-tile-straight`).
   - `spawnTilePrefab`: spawn marker (e.g., `spawn-round` or `tile-spawn`).
   - `hubTilePrefab`: hub marker (e.g., `tile-crystal` or `snow-tile-crystal`).
4. Press Play; it will instantiate decorative tiles at waypoints. Adjust `yOffset` for z-fighting.

#### Managers
1. Create `GameObject` named `ResourceManager` and add `ResourceManager`.
   - Configure `startingResources`, `passiveGain` if desired.
2. Create `GameObject` named `WaveManager` and add `WaveManager`.
3. Create `GameObject` named `PlacementManager` and add `DefenderPlacementManager`.
   - Assign `placementNodePrefab` to your PlacementNode prefab.
   - Set `desiredNodeCount` (e.g., 16).
4. Create `GameObject` named `GameManager` and add `GameManager`.
   - Assign references in Inspector:
     - `terrainGenerator`: `ProceduralTerrain`
     - `spawnerPrefab`: your `EnemySpawner` prefab
     - `enemyPrefab`: your `Enemy_UFO` prefab
     - `defenderPrefab`: your `Defender_Turret` prefab
     - `towerPrefab`: your `Tower_Base` prefab
     - `placementManager`: `PlacementManager`

#### Scene basics
- Ensure a `Main Camera` looks at the arena (terrain spans from `(0, 0)` to `(gridWidth*tileSize, gridHeight*tileSize)` in X/Z; hub is center).
- Ensure a `Directional Light` is present.

### 4) Inspector Defaults (from scripts)
- Defender `AutoAttack`: range 5, 2 shots/sec, 15 damage (set in `Defender.Awake`).
- Tower `AutoAttack`: range 6, 1.5 shots/sec, 20 damage (set in `Tower.Awake`).
- Enemy: moveSpeed ~3, contactDamage ~10, attackRange ~1, cooldown ~1.
- WaveManager: baseEnemiesPerSpawner ~5, increment/wave ~2, wave bonus ~100.
- ResourceManager: startingResources ~300; passive gain 5 per 10 seconds.

### 5) Playtest Checklist
1. Press Play.
2. Terrain generates; a tower spawns at the hub.
3. Nodes appear; click a node to place a defender (resources decrease by 100).
4. Enemies spawn from edges and walk paths.
5. Defenders and tower auto-attack the nearest enemies; enemies damage defenders/tower on contact range.
6. On wave clear, resources are rewarded and next wave starts.

### 6) Troubleshooting
- Nothing spawns: check `GameManager` prefab references.
- Cannot place defender: ensure `ResourceManager` exists and `PlacementNode` has a Collider.
- No enemies to shoot: ensure `WaveManager` exists; check `EnemySpawner` is registered by `GameManager`.
- Visuals misaligned/rotated: rotate/scale the model child only, not the root with scripts.
- Terrain invisible: assign `TerrainMat` to `ProceduralTerrain`’s `MeshRenderer`.

### 7) Asset Mapping Reference (quick picks)
- Enemies: `enemy-ufo-a/b/c/d` (+ `enemy-ufo-*-weapon` optional)
- Defender base: `tower-round-build-a/b/c/d/e/f` or `tower-square-build-a/b/c/d/e/f`, or `wood-structure*`
- Defender weapons: `weapon-turret`, `weapon-ballista`, `weapon-cannon`
- Tower extras: `tower-round-crystals`, tops/roofs/middles/bottoms (round or square variants)
- Placement marker: `selection-a` or `tile`
- Spawner marker: `spawn-round` or `tile-spawn`
- Terrain textures: `tile`/`tile-bump` or `snow-tile`/`snow-tile-bump`

### 8) Notes
- Keep scripts on prefab roots; put meshes as child objects.
- Current combat is instant-hit; projectile assets (`weapon-ammo-*`, `enemy-ufo-beam*`) can be used later if you add projectile logic.


