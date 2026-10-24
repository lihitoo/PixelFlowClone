using System;
using Cysharp.Threading.Tasks;
using Dreamy.Template.Demo;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Feature.Settings.Integration;
using Dreamy.UI;

namespace Dreamy.Template.Home
{
    // Navigation opens views; the panel manager owns their presenters.
    public sealed class HomeNavigator : IDisposable
    {
        private bool isTransitioning;
        private bool disposed;

        public async UniTask OpenAsync(HomeDestination destination)
        {
            if (disposed || isTransitioning) return;
            isTransitioning = true;
            try
            {
                switch (destination)
                {
                    case HomeDestination.Demo:
                        await PanelManager.Instance.Transition<FoundationDemoPanel>(Address.FoundationDemoPanel);
                        break;
                    case HomeDestination.Shop:
                        await PanelManager.Instance.Transition<ShopPanel>(Address.ShopPanel);
                        break;
                    case HomeDestination.Settings:
                        await PanelManager.Instance.Show<SettingsPanel>(Address.SettingsPanel);
                        break;
                    default:
                        UnityEngine.Debug.LogWarning($"[Home] Unsupported destination: {destination}.");
                        break;
                }
            }
            finally { isTransitioning = false; }
        }

        public void Dispose() => disposed = true;
    }
}
