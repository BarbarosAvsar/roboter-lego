# Code Quality Gates

Release verification baseline is Unity `2022.3.40f1`.

## Mandatory gates

All of the following must be true before delivery:

1. `zero` compile errors.
2. `zero` C# compiler warnings (`warning CS####`) in Unity logs.
3. no direct obsolete scene lookup API usage in project code (`FindObjectOfType`, `FindObjectsOfType`) outside compatibility helper wrappers.
4. no duplicated logic where a shared utility is appropriate.
5. documentation is synchronized with actual scripts, paths, and runtime behavior.

## Automated check

Run from repo root after bootstrap/tests/build:

```powershell
.\scripts\quality-gate.ps1 -UnityVersion 2022.3.40f1
```

The script fails fast when:

1. expected artifacts are missing:
   - `Builds/Logs/test-editmode.log`
   - `Builds/Logs/test-playmode.log`
   - `Builds/Logs/build-android.log`
   - `Builds/TestResults/editmode-results.xml`
   - `Builds/TestResults/playmode-results.xml`
   - `Builds/Android/RoboterLego.apk`
2. artifacts are stale beyond the allowed age window.
3. test XML is missing `<test-run>`, reports non-passed result, or reports failures.
4. any Unity log contains `warning CS####` or `error CS####`.
5. direct obsolete lookup API usage is detected in `Assets/**/*.cs` outside `UnityObjectLookup`.

## Full strict loop

```powershell
.\scripts\bootstrap-project.ps1 -UnityVersion 2022.3.40f1
.\scripts\test-editmode.ps1 -UnityVersion 2022.3.40f1
.\scripts\test-playmode.ps1 -UnityVersion 2022.3.40f1 -SkipBootstrap
.\scripts\build-android.ps1 -UnityVersion 2022.3.40f1 -SkipBootstrap
.\scripts\quality-gate.ps1 -UnityVersion 2022.3.40f1
```
