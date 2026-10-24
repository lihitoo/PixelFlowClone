using System;
using Cysharp.Threading.Tasks;
using Dreamy.Settings;
using Dreamy.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dreamy.Feature.Settings.Integration
{
    public sealed class RateUsPanel : UIPanel, IRateUsView
    {
        [SerializeField] private Button[] starButtons;
        [SerializeField] private Button rateButton;
        [SerializeField] private Button closeButton;


        public override bool CanBack => true;

        public event Action<int> RatingSelected;
        public event Action RateRequested;
        public event Action CloseRequested;

        private UnityAction[] starListeners;

        private void Awake()
        {
            starListeners = new UnityAction[starButtons.Length];
            for (int index = 0; index < starButtons.Length; index++)
            {
                int rating = index + 1;
                starListeners[index] = () => SelectRating(rating);
            }
        }

        private void OnEnable()
        {
            for (int index = 0; index < starButtons.Length; index++)
            {
                starButtons[index].onClick.AddListener(starListeners[index]);
            }

            rateButton.onClick.AddListener(RequestRate);
            closeButton.onClick.AddListener(RequestClose);
        }

        protected override void OnDisable()
        {
            for (int index = 0; index < starButtons.Length; index++)
            {
                starButtons[index].onClick.RemoveListener(starListeners[index]);
            }

            rateButton.onClick.RemoveListener(RequestRate);
            closeButton.onClick.RemoveListener(RequestClose);
            base.OnDisable();
        }

        public void SetRating(int rating)
        {
            for (int index = 0; index < starButtons.Length; index++)
            {
                starButtons[index].image.color = index < rating
                    ? new Color(1f, 0.78f, 0.18f, 1f)
                    : new Color(0.25f, 0.22f, 0.35f, 1f);
            }
        }

        public new void SetInteractable(bool interactable)
        {
            rateButton.interactable = interactable;
            closeButton.interactable = true;
            for (int index = 0; index < starButtons.Length; index++)
            {
                starButtons[index].interactable = interactable;
            }
        }

        public void Close() => Hide().Forget();

        private void SelectRating(int rating) => RatingSelected?.Invoke(rating);
        private void RequestRate() => RateRequested?.Invoke();
        private void RequestClose() => CloseRequested?.Invoke();
    }
}
