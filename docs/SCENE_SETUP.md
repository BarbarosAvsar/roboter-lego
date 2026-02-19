# Manual Scene Setup (Advanced Fallback)

Use this only if automated bootstrap is not available.  
Preferred path is `.\scripts\bootstrap-project.ps1`.

Unity verification baseline: `2022.3.40f1`.

## 1. Create the scene

1. Create `Assets/Scenes/Main.unity`.
2. Add an empty root object named `AppRoot`.

## 2. Add runtime components on `AppRoot`

Add these components:

1. `RobotContentLoader`
2. `RobotGenerator`
3. `ResourceLegoAssetProvider`
4. `AddressablesLegoAssetProvider`
5. `ProceduralLegoAssetProvider`
6. `CompositeLegoAssetProvider`
7. `RobotAssembler`
8. `RobotFaceBuilder`
9. `RobotVoiceSynthesizer`
10. `VisualPulseController`
11. `RobotAppearanceController`
12. `RobotEnvironmentController`
13. `RobotCustomizationController`
14. `CustomizationInputRouter`
15. `RobotBehaviorController`
16. `TouchInputCollector`
17. `MotionSampler`
18. `SwipeFallbackInput`
19. `SimpleAudioCueService`
20. `RobotSessionController`

## 3. Wire serialized references

Set these references in Inspector:

- `RobotGenerator.contentLoader` -> `RobotContentLoader`
- `RobotAssembler.contentLoader` -> `RobotContentLoader`
- `RobotAssembler.assetProviderBehaviour` -> `CompositeLegoAssetProvider`
- `RobotAssembler.faceBuilder` -> `RobotFaceBuilder`
- `CompositeLegoAssetProvider.resourceProvider` -> `ResourceLegoAssetProvider`
- `CompositeLegoAssetProvider.addressablesProvider` -> `AddressablesLegoAssetProvider`
- `CompositeLegoAssetProvider.proceduralProvider` -> `ProceduralLegoAssetProvider`
- `RobotBehaviorController.voiceSynthesizer` -> `RobotVoiceSynthesizer`
- `RobotBehaviorController.visualPulse` -> `VisualPulseController`
- `RobotBehaviorController.robotAssembler` -> `RobotAssembler`
- `RobotBehaviorController.audioCueServiceBehaviour` -> `SimpleAudioCueService`
- `RobotEnvironmentController.targetCamera` -> `Main Camera`
- `RobotEnvironmentController.directionalLight` -> `Directional Light`
- `RobotCustomizationController.robotAssembler` -> `RobotAssembler`
- `RobotCustomizationController.appearanceController` -> `RobotAppearanceController`
- `RobotCustomizationController.environmentController` -> `RobotEnvironmentController`
- `CustomizationInputRouter.customizationController` -> `RobotCustomizationController`
- `CustomizationInputRouter.motionSampler` -> `MotionSampler`
- `CustomizationInputRouter.targetCamera` -> `Main Camera`
- `RobotSessionController.touchInputCollector` -> `TouchInputCollector`
- `RobotSessionController.motionSampler` -> `MotionSampler`
- `RobotSessionController.swipeFallbackInput` -> `SwipeFallbackInput`
- `RobotSessionController.robotGenerator` -> `RobotGenerator`
- `RobotSessionController.robotAssembler` -> `RobotAssembler`
- `RobotSessionController.customizationController` -> `RobotCustomizationController`
- `RobotSessionController.behaviorController` -> `RobotBehaviorController`
- `RobotSessionController.customizationInputRouter` -> `CustomizationInputRouter`
- `RobotSessionController.audioCueServiceBehaviour` -> `SimpleAudioCueService`
- `RobotSessionController.hudController` -> `ToddlerHudController` (after UI is created)

## 4. Add camera/light/event system

1. Add `Main Camera`.
2. Add `Directional Light`.
3. Add `EventSystem` with `StandaloneInputModule`.

## 5. Add HUD

1. Create a Canvas (`Screen Space Overlay`).
2. Add `ToddlerHudController` to Canvas.
3. Create seven large buttons:
   - `Part - Button`
   - `Part + Button`
   - `Color Button`
   - `Environment Button`
   - `Dance Button`
   - `Sing Button`
   - `New Robot Button`
4. Assign all seven button references on `ToddlerHudController`.
5. Assign `RobotSessionController.hudController`.

## 6. Android settings

1. Build target: Android.
2. App id: `com.roboterlego.toddlerbot`.
3. Target architecture: ARM64 only.
4. Orientation: Auto rotation with portrait + landscape enabled.
5. Add `Assets/Scenes/Main.unity` to Build Settings scene list.

## 7. Validation

1. Enter Play Mode.
2. Wait for create window to complete.
3. Verify a generated robot appears.
4. Verify create-phase customization works (parts/color/environment).
5. Verify tilt/swipe/keyboard movement works in play.
6. Verify `Dance`, `Sing`, and `New Robot` buttons work.
