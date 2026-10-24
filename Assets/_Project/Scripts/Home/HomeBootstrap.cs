using System;
using Cysharp.Threading.Tasks;
using Dreamy.UI;
using UnityEngine;

namespace Dreamy.Template.Home
{
    [DisallowMultipleComponent]
    public sealed class HomeBootstrap : MonoBehaviour
    {
        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                await UniTask.WaitUntil(
                    () => GameInstaller.State is BootstrapState.Ready or BootstrapState.Failed,
                    cancellationToken: this.GetCancellationTokenOnDestroy());

                if (GameInstaller.State == BootstrapState.Failed)
                {
                    Debug.LogError($"[Home] Bootstrap failed: {GameInstaller.InitializationException}", this);
                    return;
                }

                await PanelManager.Instance.Show<HomePanel>(Address.HomePanel);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

    }
}
