# Quickstart

Unity verification baseline: `2022.3.40f1`.

## Prerequisites

- Windows with PowerShell
- Internet access for Unity installation
- `winget` available
- Unity license activated (Personal or Pro) for batchmode

## One-command pipeline

From repo root:

```powershell
.\scripts\run-all.ps1 -UnityVersion 2022.3.40f1
```

`run-all` uses per-step timeouts and stops/kills stuck Unity processes automatically.
Increase limits on slower machines:

```powershell
.\scripts\run-all.ps1 -UnityVersion 2022.3.40f1 -BootstrapTimeoutMinutes 30 -EditModeTimeoutMinutes 45 -PlayModeTimeoutMinutes 50 -BuildTimeoutMinutes 90
```

`run-all` executes:

1. `bootstrap-project.ps1`
2. `test-editmode.ps1`
3. `test-playmode.ps1`
4. `build-android.ps1`

## First-time machine setup

If Unity is not installed yet:

```powershell
.\scripts\install-toolchain.ps1
```

Then open Unity Hub and complete:

1. Sign in with your Unity account.
2. Activate a Personal/Pro editor license.

After license activation:

```powershell
.\scripts\run-all.ps1 -UnityVersion 2022.3.40f1
```

## Individual commands

```powershell
.\scripts\bootstrap-project.ps1 -UnityVersion 2022.3.40f1
.\scripts\test-editmode.ps1 -UnityVersion 2022.3.40f1
.\scripts\test-playmode.ps1 -UnityVersion 2022.3.40f1
.\scripts\build-android.ps1 -UnityVersion 2022.3.40f1
.\scripts\quality-gate.ps1 -UnityVersion 2022.3.40f1
.\scripts\capture-screenshots.ps1 -UnityVersion 2022.3.40f1
```

## Outputs

- Test results:
  - `Builds/TestResults/editmode-results.xml`
  - `Builds/TestResults/playmode-results.xml`
- Android APK:
  - `Builds/Android/RoboterLego.apk`

## Runtime controls

- Create phase customization:
  - HUD: `Part -`, `Part +`, `Color`, `Environment`.
  - Keyboard: `1-6` select slot, `Q/E` cycle slot, `C/V` cycle color, `R/T` cycle environment.
  - Touch/mouse/gyro are also supported.
- Play phase:
  - HUD: `Dance`, `Sing`, `New Robot`.
  - Movement: gyro, swipe fallback, and `WASD`/arrow keys in editor/standalone.
