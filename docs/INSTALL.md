# Install Guide (Windows)

## Automated install (preferred)

Run from repo root:

```powershell
.\scripts\install-toolchain.ps1
```

Install commands are time-bounded to avoid hanging indefinitely. Override if needed:

```powershell
.\scripts\install-toolchain.ps1 -WingetInstallTimeoutMinutes 120 -HubInstallTimeoutMinutes 120
```

This script:

1. validates `winget`,
2. installs Unity Hub,
3. attempts Unity Hub CLI install for Editor `2022.3.40f1` with Android modules:
   - `android`
   - `android-sdk-ndk-tools`
   - `android-open-jdk`,
4. if Hub CLI is unsupported, falls back to installing `Unity 2022` editor via winget,
5. resolves and prints `Unity.exe` path.

In fallback mode, install Android Build Support manually in Unity Hub if APK build fails.

## Unity license activation (required for batchmode)

Before running `scripts/bootstrap-project.ps1`, `scripts/test-*.ps1`, or `scripts/build-android.ps1`:

1. Open Unity Hub.
2. Sign in with your Unity account.
3. Activate a Personal or Pro license.
4. Start the installed Unity Editor once from Hub to complete first-run setup.

Baseline runtime does not require external LEGO prefabs or external audio packs:
- procedural LEGO-like meshes are used as fallback,
- robot SFX/music are synthesized procedurally at runtime.

## Manual fallback

Use this if Unity Hub CLI fails on your machine policy/version.

1. Install Unity Hub from Microsoft Store or Unity website.
2. In Unity Hub, install Editor `2022.3.40f1`.
3. Add Android support modules:
   - Android Build Support,
   - Android SDK & NDK Tools,
   - OpenJDK.
4. Verify editor path exists:
   - `C:\Program Files\Unity\Hub\Editor\2022.3.40f1\Editor\Unity.exe`
5. Confirm resolver works:

```powershell
.\scripts\resolve-unity-path.ps1 -UnityVersion 2022.3.40f1
```

## Post-install verification

Run the full strict verification sequence:

```powershell
.\scripts\run-all.ps1 -UnityVersion 2022.3.40f1
.\scripts\quality-gate.ps1 -UnityVersion 2022.3.40f1
.\scripts\capture-screenshots.ps1 -UnityVersion 2022.3.40f1
```
