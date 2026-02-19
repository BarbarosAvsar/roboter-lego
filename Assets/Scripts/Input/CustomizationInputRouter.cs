using RoboterLego.Assembly;
using RoboterLego.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityInput = UnityEngine.Input;

namespace RoboterLego.Input
{
    public sealed class CustomizationInputRouter : MonoBehaviour
    {
        [SerializeField] private RobotCustomizationController customizationController;
        [SerializeField] private MotionSampler motionSampler;
        [SerializeField] private Camera targetCamera;

        [Header("Gesture Thresholds")]
        [SerializeField] private float tapMaxDistancePixels = 26f;
        [SerializeField] private float swipeThresholdPixels = 90f;
        [SerializeField] private float inputDebounceSeconds = 0.35f;
        [SerializeField] private float gyroAxisThreshold = 0.45f;
        [SerializeField] private float shakeThreshold = 0.55f;

        private bool createModeActive;
        private float lastPartCommandAt;
        private float lastEnvironmentCommandAt;
        private float lastColorCommandAt;
        private Vector2 touchStart;
        private bool touchActive;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            if (!createModeActive || customizationController == null)
            {
                return;
            }

            HandleKeyboardInput();
            HandleMouseInput();
            HandleTouchInput();
            HandleGyroInput();
        }

        public void SetCreateModeActive(bool active)
        {
            createModeActive = active;
            if (!active)
            {
                touchActive = false;
            }
        }

        private void HandleKeyboardInput()
        {
            if (UnityInput.GetKeyDown(KeyCode.Alpha1))
            {
                customizationController.SelectSlot(RobotPartSlot.Core);
            }

            if (UnityInput.GetKeyDown(KeyCode.Alpha2))
            {
                customizationController.SelectSlot(RobotPartSlot.LeftArm);
            }

            if (UnityInput.GetKeyDown(KeyCode.Alpha3))
            {
                customizationController.SelectSlot(RobotPartSlot.RightArm);
            }

            if (UnityInput.GetKeyDown(KeyCode.Alpha4))
            {
                customizationController.SelectSlot(RobotPartSlot.TopAccessory);
            }

            if (UnityInput.GetKeyDown(KeyCode.Alpha5))
            {
                customizationController.SelectSlot(RobotPartSlot.Face);
            }

            if (UnityInput.GetKeyDown(KeyCode.Alpha6))
            {
                customizationController.SelectSlot(RobotPartSlot.Ears);
            }

            if (UnityInput.GetKeyDown(KeyCode.Q))
            {
                customizationController.CycleSelectedPart(-1);
            }

            if (UnityInput.GetKeyDown(KeyCode.E))
            {
                customizationController.CycleSelectedPart(1);
            }

            if (UnityInput.GetKeyDown(KeyCode.C))
            {
                customizationController.CycleColor(1);
            }

            if (UnityInput.GetKeyDown(KeyCode.V))
            {
                customizationController.CycleColor(-1);
            }

            if (UnityInput.GetKeyDown(KeyCode.R))
            {
                customizationController.CycleEnvironment(-1);
            }

            if (UnityInput.GetKeyDown(KeyCode.T))
            {
                customizationController.CycleEnvironment(1);
            }
        }

        private void HandleMouseInput()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            float scroll = UnityInput.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                customizationController.CycleColor(scroll > 0f ? 1 : -1);
            }

            if (!UnityInput.GetMouseButtonDown(0) && !UnityInput.GetMouseButtonDown(1))
            {
                return;
            }

            bool reverse = UnityInput.GetMouseButtonDown(1) || UnityInput.GetKey(KeyCode.LeftShift) || UnityInput.GetKey(KeyCode.RightShift);
            RobotPartMarker marker = RaycastPartMarker(UnityInput.mousePosition);
            if (marker != null)
            {
                customizationController.CyclePartFromMarker(marker, reverse ? -1 : 1);
            }
        }

        private void HandleTouchInput()
        {
            if (UnityInput.touchCount <= 0)
            {
                return;
            }

            Touch touch = UnityInput.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                touchActive = false;
                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchActive = true;
                    touchStart = touch.position;
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touchActive)
                    {
                        ProcessTouchGesture(touchStart, touch.position);
                    }

                    touchActive = false;
                    break;
            }
        }

        private void ProcessTouchGesture(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            if (delta.magnitude <= tapMaxDistancePixels)
            {
                RobotPartMarker marker = RaycastPartMarker(end);
                if (marker != null)
                {
                    customizationController.CyclePartFromMarker(marker, 1);
                }

                return;
            }

            if (delta.magnitude < swipeThresholdPixels)
            {
                return;
            }

            bool hasGyro = motionSampler != null && motionSampler.TryGetDirectionalTilt(out _);
            if (hasGyro)
            {
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                customizationController.CycleSelectedPart(delta.x > 0f ? 1 : -1);
            }
            else
            {
                customizationController.CycleEnvironment(delta.y > 0f ? 1 : -1);
            }
        }

        private void HandleGyroInput()
        {
            if (motionSampler == null || !motionSampler.TryGetDirectionalTilt(out var tilt))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (Mathf.Abs(tilt.x) >= gyroAxisThreshold && now - lastPartCommandAt >= inputDebounceSeconds)
            {
                customizationController.CycleSelectedPart(tilt.x > 0f ? 1 : -1);
                lastPartCommandAt = now;
            }

            if (Mathf.Abs(tilt.y) >= gyroAxisThreshold && now - lastEnvironmentCommandAt >= inputDebounceSeconds)
            {
                customizationController.CycleEnvironment(tilt.y > 0f ? 1 : -1);
                lastEnvironmentCommandAt = now;
            }

            if (motionSampler.CurrentShakeEnergy >= shakeThreshold && now - lastColorCommandAt >= inputDebounceSeconds)
            {
                customizationController.CycleColor(1);
                lastColorCommandAt = now;
            }
        }

        private RobotPartMarker RaycastPartMarker(Vector2 screenPosition)
        {
            Camera camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null)
            {
                return null;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return null;
            }

            return hit.collider != null ? hit.collider.GetComponentInParent<RobotPartMarker>() : null;
        }

        private void ResolveDependencies()
        {
            if (customizationController == null)
            {
                customizationController = GetComponent<RobotCustomizationController>() ?? UnityObjectLookup.FindFirst<RobotCustomizationController>();
            }

            if (motionSampler == null)
            {
                motionSampler = GetComponent<MotionSampler>() ?? UnityObjectLookup.FindFirst<MotionSampler>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
    }
}
