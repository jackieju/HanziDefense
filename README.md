# Hanzi Zombie Defense

A mobile 3D game (iPhone/iPad) where zombies approach you from the front, each with a Chinese character above their head. Hand-write the character with correct stroke order anywhere on screen to kill the zombie before it reaches you.

## Requirements

- **Unity 2022.3 LTS** (or newer)
- **Universal Render Pipeline (URP)**
- **iOS Build Support** module installed in Unity Hub

## Platform

- **Primary target**: iPhone and iPad (iOS 15+)
- **Orientation**: Landscape only
- **Input**: Touch (finger drawing on full screen)
- **Also works**: Unity Editor with mouse for testing

## Setup Instructions

### 1. Open in Unity

Open the `HanziZombieDefense` folder as a Unity project. Unity will import all packages automatically from `Packages/manifest.json`:

- Input System
- AI Navigation
- TextMeshPro
- Cinemachine
- URP

### 2. Configure Input System

When prompted, select **"Both"** for the Active Input Handling (or set it manually in Project Settings > Player > Active Input Handling > Both).

### 3. Apply iOS Settings

Run **Tools > Hanzi Zombie Defense > Apply iOS Settings** to configure:
- Target: iPhone + iPad
- iOS 15.0 minimum
- Landscape only orientation
- IL2CPP scripting backend (required for iOS)
- ARM64 architecture
- Automatic signing

### 4. Apply Mobile Optimizations

Run **Tools > Hanzi Zombie Defense > Apply Mobile Optimizations** to set:
- Reduced shadow distance
- Lower particle budgets
- Mobile-appropriate quality settings
4. Click **Apply**

### 4. Set Up URP

1. Go to **Edit > Project Settings > Graphics**
2. Assign `Assets/_Project/Settings/URP/URP-HighQuality.asset` as the Scriptable Render Pipeline Settings

### 5. Import TMP Essentials

1. Go to **Window > TextMeshPro > Import TMP Essential Resources**
2. Import the essentials package

### 6. Generate CJK Font

1. Download [Noto Sans SC](https://fonts.google.com/noto/specimen/Noto+Sans+SC) (or Source Han Sans)
2. Import the .ttf/.otf into `Assets/_Project/Fonts/`
3. Go to **Window > TextMeshPro > Font Asset Creator**
4. Set Source Font to Noto Sans SC
5. Set Atlas Resolution to 4096x4096
6. Set Character Set to "Characters from File"
7. Point it to a text file containing all characters from `hanzi_index.json`
8. Generate and save as `Assets/_Project/Fonts/NotoSansSC-SDF.asset`

### 7. Process Hanzi Data (Already Done)

The `StreamingAssets/Hanzi/` folder already contains:
- `hanzi_index.json` — index of all 9,574 characters with stroke counts
- `graphics/*.json` — per-character stroke data from Make Me a Hanzi

To re-process or update: **Tools > Hanzi Zombie Defense > Process Graphics Data**

### 8. Build Scenes

The project has 3 scenes configured in Build Settings:
1. `Boot` — initialization
2. `MainMenu` — start screen
3. `Gameplay` — main game

Create these scenes in `Assets/_Project/Scenes/` and set up the GameObjects as described in the architecture.

### 9. Create Prefabs

Key prefabs to create:
- **Player Rig** — Static camera + PlayerTargeting + PlayerHealth + PlayerCamera (no CharacterController)
- **Zombie** — NavMeshAgent + Animator + all Zombie scripts + world-space canvas
- **Explosion VFX** — ParticleSystem prefab

### 10. Bake NavMesh

1. Open the Gameplay scene
2. Mark your ground/terrain as Navigation Static
3. Go to **Window > AI > Navigation**
4. Bake the NavMesh

## Project Structure

```
Assets/_Project/
├── Scripts/          (51 C# files across 15 systems)
├── ScriptableObjects/ (definitions + instance assets)
├── Prefabs/          (player, zombies, VFX, UI, environment)
├── Scenes/           (Boot, MainMenu, Gameplay)
├── Art/              (models, textures, materials, animations, shaders)
├── Audio/            (SFX, music, VO)
├── Fonts/            (CJK SDF font)
├── StreamingAssets/  (9,574 hanzi character JSONs)
└── Settings/         (URP, Input Actions)
```

## Game Design

- **Fixed position** — player never moves, camera is static
- **Zombies from front** — spawn in lanes (left/center/right) and walk toward player
- **Sequential targeting** — nearest zombie is auto-targeted, one at a time
- **Full-screen drawing** — touch/draw anywhere on screen to write the character
- **Stroke order matters** — must write strokes in correct order

## Game Architecture

- **Stroke Recognition**: Resampled 32-point matching with direction + shape + endpoint validation
- **Data Source**: Make Me a Hanzi (MIT license) — median stroke skeletons
- **Zombie AI**: NavMesh pathfinding with difficulty-scaled speed
- **Difficulty**: AnimationCurve-driven scaling over time (spawn rate, speed, character complexity)
- **Object Pooling**: All zombies and VFX are pooled for performance
- **Mobile-first**: Touch input, 60fps target, landscape orientation

## Controls

- **Touch/Drag anywhere** — Draw strokes to write the character
- **Pause button** — Pause game (on-screen UI button)

## Building for iOS

1. Run **Tools > Hanzi Zombie Defense > Apply iOS Settings**
2. **File > Build Settings > iOS > Build**
3. Open the Xcode project
4. Set your signing team
5. Build to device

## License

- Game code: Your license
- Stroke data: [Make Me a Hanzi](https://github.com/skishore/makemeahanzi) — Arphic Public License (graphics), CC-BY-SA (dictionary)
- Font: Noto Sans SC — SIL Open Font License
