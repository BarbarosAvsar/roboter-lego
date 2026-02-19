# roboter-lego

Virtual-only toddler app prototype (Unity, Android) that generates and animates LEGO-like 3D robots from touch + motion input.

Release verification baseline: Unity `2022.3.40f1`.

## Current status

The project is now structured to run end-to-end with automation:

- Automated Windows toolchain install script (Unity Hub + Unity editor + Android modules).
- Automated project bootstrap that creates `Assets/Scenes/Main.unity` and wires all dependencies.
- Deterministic robot generation from a compatibility-validated module catalog.
- Composite asset provider chain:
  - explicit resource mappings,
  - Addressables,
  - procedural LEGO-like fallback (guaranteed runtime baseline without external prefabs).
- Session loop implemented:
  - `Create -> Generate -> Play -> Reset`.
- Create-phase robot customization implemented:
  - swap core/arms/accessory/face/ears,
  - change robot color palette,
  - switch between 5 procedural environments.
- Input coverage for customization + gameplay:
  - touch, mouse, keyboard, gyro, and swipe fallback when gyro is unavailable.
- Procedural robot audio feedback:
  - build/mechanical/drill sequence on robot generation,
  - movement loop while moving,
  - dance music loop when dancing.
- EditMode and PlayMode automated tests.
- Android ARM64 APK build automation.

## Quick start

See `docs/QUICKSTART.md` for the full command sequence.

Minimal flow:

```powershell
.\scripts\install-toolchain.ps1
```

Then activate Unity license once in Unity Hub (Personal/Pro), and run:

```powershell
.\scripts\run-all.ps1 -UnityVersion 2022.3.40f1
```

Each Unity step has a built-in timeout and is force-terminated on timeout to avoid stuck runs.
If your machine is slower, increase limits explicitly:

```powershell
.\scripts\run-all.ps1 -UnityVersion 2022.3.40f1 -BootstrapTimeoutMinutes 30 -EditModeTimeoutMinutes 45 -PlayModeTimeoutMinutes 50 -BuildTimeoutMinutes 90
```

This runs:

1. scene/bootstrap setup,
2. EditMode tests,
3. PlayMode smoke tests,
4. Android APK build to `Builds/Android/RoboterLego.apk`.

Strict quality gate:

```powershell
.\scripts\quality-gate.ps1 -UnityVersion 2022.3.40f1
```

Screenshot capture (12 deterministic UI-visible shots):

```powershell
.\scripts\capture-screenshots.ps1 -UnityVersion 2022.3.40f1
```

Details:
- `docs/CODE_QUALITY.md`
- `docs/SCREENSHOTS.md`

## Project layout

- `Assets/Scripts/Domain` shared runtime contracts and types.
- `Assets/Scripts/Input` touch/shape/motion feature extraction.
- `Assets/Scripts/Generation` content loading, compatibility graph, blueprint generation.
- `Assets/Scripts/Assets` provider chain and procedural LEGO-like fallback generation.
- `Assets/Scripts/Assembly` runtime robot assembly.
- `Assets/Scripts/Behavior` movement, dance, sing, visual/audio feedback.
- `Assets/Scripts/Session` app state machine and loop orchestration.
- `Assets/Scripts/UI` toddler HUD bridge.
- `Assets/Editor/Bootstrap` scene and project settings automation.
- `Assets/Editor/Build` Android build entrypoint.
- `Assets/Tests/EditMode` logic tests.
- `Assets/Tests/PlayMode` runtime smoke tests.
- `Assets/Resources/RobotContent` offline module catalog and compatibility rules.
- `scripts` PowerShell automation commands.
- `docs` setup and operational documentation.

## Constraints and scope

- No physical robot control.
- No accounts, analytics, ads, or camera requirement.
- Offline runtime after install.
- Android tablet first.
- Official LEGO assets are optional; procedural fallback is the default runnable baseline.

## Controls

- Create phase:
  - Touch: tap robot part to cycle it, swipe left/right to cycle selected part, swipe up/down to change environment.
  - Mouse: click part to cycle, right-click or shift-click to reverse, wheel to change color.
  - Keyboard: `1-6` select part slot, `Q/E` cycle part, `C/V` change color, `R/T` change environment.
  - Gyro: tilt X cycles parts, tilt Y changes environment, shake changes color.
- Play phase:
  - HUD: `Dance`, `Sing`, `New Robot`.
  - Motion: gyro/swipe fallback.
  - Keyboard movement (editor/standalone): `WASD` or arrow keys.
