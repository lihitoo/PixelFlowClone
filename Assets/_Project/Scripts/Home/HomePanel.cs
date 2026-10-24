using System;
using Dreamy.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Template.Home
{
    public sealed partial class HomePanel : UIPanel, IHomeView
    {
        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button demoButton;

        public override bool CanBack => false;
        public override bool CanCache => true;

        public event Action<HomeDestination> DestinationRequested;

        private void OnEnable()
        {
            BindMap();
            shopButton?.onClick.AddListener(RequestShop);
            settingsButton?.onClick.AddListener(RequestSettings);
            if (demoButton != null)
            {
                demoButton.onClick.AddListener(RequestDemo);
            }
        }

        protected override void OnDisable()
        {
            UnbindMap();
            shopButton?.onClick.RemoveListener(RequestShop);
            settingsButton?.onClick.RemoveListener(RequestSettings);
            demoButton?.onClick.RemoveListener(RequestDemo);
            base.OnDisable();
        }

        public void SetNavigationInteractable(bool interactable)
        {
            if (shopButton != null) shopButton.interactable = interactable;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (demoButton != null) demoButton.interactable = interactable;
        }

        private void RequestDemo() => DestinationRequested?.Invoke(HomeDestination.Demo);

        private void RequestShop() => DestinationRequested?.Invoke(HomeDestination.Shop);
        private void RequestSettings() => DestinationRequested?.Invoke(HomeDestination.Settings);
    }
}
