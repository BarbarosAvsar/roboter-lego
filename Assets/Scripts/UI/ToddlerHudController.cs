using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoboterLego.UI
{
    public sealed class ToddlerHudController : MonoBehaviour
    {
        [Header("Play Controls")]
        [SerializeField] private Button danceButton;
        [SerializeField] private Button singButton;
        [SerializeField] private Button newRobotButton;

        [Header("Create Controls")]
        [SerializeField] private Button partPrevButton;
        [SerializeField] private Button partNextButton;
        [SerializeField] private Button colorButton;
        [SerializeField] private Button environmentButton;

        public event Action DancePressed;
        public event Action SingPressed;
        public event Action NewRobotPressed;
        public event Action PartPrevPressed;
        public event Action PartNextPressed;
        public event Action ColorPressed;
        public event Action EnvironmentPressed;

        private void OnEnable()
        {
            if (danceButton != null)
            {
                danceButton.onClick.AddListener(OnDancePressed);
            }

            if (singButton != null)
            {
                singButton.onClick.AddListener(OnSingPressed);
            }

            if (newRobotButton != null)
            {
                newRobotButton.onClick.AddListener(OnNewRobotPressed);
            }

            if (partPrevButton != null)
            {
                partPrevButton.onClick.AddListener(OnPartPrevPressed);
            }

            if (partNextButton != null)
            {
                partNextButton.onClick.AddListener(OnPartNextPressed);
            }

            if (colorButton != null)
            {
                colorButton.onClick.AddListener(OnColorPressed);
            }

            if (environmentButton != null)
            {
                environmentButton.onClick.AddListener(OnEnvironmentPressed);
            }
        }

        private void OnDisable()
        {
            if (danceButton != null)
            {
                danceButton.onClick.RemoveListener(OnDancePressed);
            }

            if (singButton != null)
            {
                singButton.onClick.RemoveListener(OnSingPressed);
            }

            if (newRobotButton != null)
            {
                newRobotButton.onClick.RemoveListener(OnNewRobotPressed);
            }

            if (partPrevButton != null)
            {
                partPrevButton.onClick.RemoveListener(OnPartPrevPressed);
            }

            if (partNextButton != null)
            {
                partNextButton.onClick.RemoveListener(OnPartNextPressed);
            }

            if (colorButton != null)
            {
                colorButton.onClick.RemoveListener(OnColorPressed);
            }

            if (environmentButton != null)
            {
                environmentButton.onClick.RemoveListener(OnEnvironmentPressed);
            }
        }

        public void SetPlayControlsVisible(bool visible)
        {
            if (danceButton != null)
            {
                danceButton.gameObject.SetActive(visible);
            }

            if (singButton != null)
            {
                singButton.gameObject.SetActive(visible);
            }

            if (newRobotButton != null)
            {
                newRobotButton.gameObject.SetActive(visible);
            }
        }

        public void SetCreateControlsVisible(bool visible)
        {
            if (partPrevButton != null)
            {
                partPrevButton.gameObject.SetActive(visible);
            }

            if (partNextButton != null)
            {
                partNextButton.gameObject.SetActive(visible);
            }

            if (colorButton != null)
            {
                colorButton.gameObject.SetActive(visible);
            }

            if (environmentButton != null)
            {
                environmentButton.gameObject.SetActive(visible);
            }
        }

        private void OnDancePressed()
        {
            DancePressed?.Invoke();
        }

        private void OnSingPressed()
        {
            SingPressed?.Invoke();
        }

        private void OnNewRobotPressed()
        {
            NewRobotPressed?.Invoke();
        }

        private void OnPartPrevPressed()
        {
            PartPrevPressed?.Invoke();
        }

        private void OnPartNextPressed()
        {
            PartNextPressed?.Invoke();
        }

        private void OnColorPressed()
        {
            ColorPressed?.Invoke();
        }

        private void OnEnvironmentPressed()
        {
            EnvironmentPressed?.Invoke();
        }
    }
}
