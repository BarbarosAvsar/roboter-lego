# Scene Setup (Unity)

## Objects

1. Create empty `AppRoot` object.
2. Add components to `AppRoot`:
   - `RobotContentLoader`
   - `RobotGenerator`
   - `ResourceLegoAssetProvider`
   - `RobotAssembler`
   - `RobotVoiceSynthesizer`
   - `VisualPulseController`
   - `RobotBehaviorController`
   - `TouchInputCollector`
   - `MotionSampler`
   - `SwipeFallbackInput`
   - `SimpleAudioCueService`
   - `RobotSessionController`
3. Assign references in `RobotSessionController` fields.

## UI

1. Create a Canvas with three large buttons:
   - Dance
   - Sing
   - New Robot
2. Add `ToddlerHudController` to the Canvas.
3. Wire the button references.
4. Assign `ToddlerHudController` to `RobotSessionController`.

## Prefabs

1. Import licensed LEGO prefabs.
2. Add mappings in `ResourceLegoAssetProvider`:
   - key = `prefabRef` from `module_catalog.json`
   - value = matching prefab.
3. Run in editor and verify session loop:
   - auto create phase (6s),
   - generated robot appears,
   - tilt/swipe moves robot,
   - Dance/Sing/New Robot buttons respond.
