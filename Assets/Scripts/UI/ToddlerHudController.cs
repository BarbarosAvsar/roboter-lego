using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoboterLego.UI
{
    public sealed class ToddlerHudController : MonoBehaviour
    {
        [SerializeField] private Button danceButton;
        [SerializeField] private Button singButton;
        [SerializeField] private Button newRobotButton;

        public event Action DancePressed;
        public event Action SingPressed;
        public event Action NewRobotPressed;

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
    }
}
