# Screenshot Capture

Release verification baseline is Unity `2022.3.40f1`.

The automated capture flow writes UI-visible PNGs to `Builds/Screenshots/`.

## Command

```powershell
.\scripts\capture-screenshots.ps1 -UnityVersion 2022.3.40f1
```

The script runs Unity in batch mode and calls:

- `RoboterLego.Editor.Capture.RobotScreenshotBatch.CaptureTwelveWithUi()`
- runtime capture sequence: `RobotScreenshotDriver.RunSequenceAndExit(string outputDir)`

## Required output files

1. `Builds/Screenshots/01_create_default_factory.png`
2. `Builds/Screenshots/02_create_core_swap_moon.png`
3. `Builds/Screenshots/03_create_leftarm_swap_neon.png`
4. `Builds/Screenshots/04_create_rightarm_swap_desert.png`
5. `Builds/Screenshots/05_create_accessory_swap_arctic.png`
6. `Builds/Screenshots/06_create_face_ears_swap_factory.png`
7. `Builds/Screenshots/07_create_color_cycle_1.png`
8. `Builds/Screenshots/08_create_color_cycle_2.png`
9. `Builds/Screenshots/09_play_default_ui.png`
10. `Builds/Screenshots/10_play_dance_ui.png`
11. `Builds/Screenshots/11_play_move_ui.png`
12. `Builds/Screenshots/12_play_new_robot_regenerated_ui.png`

The script also writes:

- `Builds/Screenshots/index.md` manifest
- `Builds/Screenshots/_capture_complete.txt` completion marker

## Validation performed by script

1. all 12 files exist
2. each file is non-empty
3. each file has a valid PNG signature

## Troubleshooting

1. If capture times out, increase `-TimeoutMinutes`:
   ```powershell
   .\scripts\capture-screenshots.ps1 -UnityVersion 2022.3.40f1 -TimeoutMinutes 30
   ```
2. If graphics initialization fails in headless/batch mode, run a manual fallback:
   - open `Assets/Scenes/Main.unity` in Unity Editor UI,
   - use menu `RoboterLego/Tools/Capture 12 Screenshots With UI`,
   - verify files and names in `Builds/Screenshots/`.
3. If files are produced but rejected, inspect `Builds/Logs/capture-screenshots.log` for capture errors.
