# Toddler Motion-to-LEGO Robot Creator

Virtual-only Android tablet app scaffold for creating and animating LEGO robots with toddler-friendly touch + motion input.

## What is implemented

- Unity-oriented C# runtime architecture under `Assets/Scripts`.
- Session loop: `Create -> Generate -> Play -> Reset`.
- Input pipeline:
  - touch event collection (debounce + stroke filtering),
  - shape recognition (`Circle`, `Square`, `Triangle`, `Line`, `Swirl`, `Unknown`),
  - motion sampling and shake-energy extraction.
- Generation pipeline:
  - deterministic feature hashing + controlled randomness,
  - module compatibility graph,
  - blueprint generation with strict compatibility checks.
- Assembly pipeline:
  - runtime robot composition via prefab references.
- Behavior pipeline:
  - gyro-driven movement with swipe fallback,
  - dance and sing routines,
  - procedural robot vocal synthesis.
- Offline content files in `Assets/Resources/RobotContent`.
- Unity edit-mode test files under `Assets/Tests/EditMode`.

## Repository layout

- `Assets/Scripts/Domain`: shared types and interfaces.
- `Assets/Scripts/Input`: touch/motion collection and feature extraction.
- `Assets/Scripts/Generation`: content models, loader, and robot generator.
- `Assets/Scripts/Assembly`: runtime robot assembly.
- `Assets/Scripts/Behavior`: movement, dance, singing, visual pulse.
- `Assets/Scripts/Session`: app state machine and loop orchestration.
- `Assets/Scripts/UI`: simple toddler button event bridge.
- `Assets/Resources/RobotContent`: bundled offline content JSON.
- `Assets/Tests/EditMode`: NUnit tests for core logic.

## Unity setup notes

1. Create/open a Unity Android project.
2. Copy this repository content into the Unity project root.
3. Provide licensed LEGO prefabs matching `prefabRef` entries from `module_catalog.json`.
4. Add a scene with:
   - `RobotSessionController`,
   - `TouchInputCollector`,
   - `MotionSampler`,
   - `RobotGenerator`,
   - `ResourceLegoAssetProvider`,
   - `RobotAssembler`,
   - `RobotBehaviorController`,
   - `ToddlerHudController`.
5. Wire references in the inspector and build an APK for tablet testing.

## Important constraints

- Offline-only runtime (no cloud/account/analytics hooks).
- No camera/AR dependency.
- No persistent save model in this scaffold.
- Official LEGO assets are assumed licensed and supplied externally.
