using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Dreamy.Feature.Settings.Integration;
using Dreamy.Template.Demo;
using Dreamy.Template.Home;
using Dreamy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamy.Template
{
    public sealed partial class GameInstaller
    {
        private PanelPresenterFactory presenterFactory;
        private readonly Dictionary<PanelManager, PanelPresenterFactory> previousFactories = new();
        private bool isOpeningRateUs;

        private void InstallPresentation()
        {
            if (gameProfile == null)
            {
                presenterFactory.Register<HomePanel>(view => new HomePresenter(view, new HomeNavigator()));
                presenterFactory.Register<FoundationDemoPanel>(view =>
                    new FoundationDemoRoot(view, datasave, () => dataConfig.GetTable<TemplateConfig>()));
            }
            RegisterService<PanelPresenterFactory>(presenterFactory);
            SceneManager.sceneLoaded += AttachScenePresentation;
            SceneManager.sceneUnloaded += PruneScenePresentation;
            foreach (var manager in FindObjectsByType<PanelManager>(
                FindObjectsInactive.Include)) AttachPresentation(manager);
        }

        private void AttachScenePresentation(Scene scene, LoadSceneMode mode)
        {
            if (State != BootstrapState.Ready) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var manager in root.GetComponentsInChildren<PanelManager>(true))
                    AttachPresentation(manager);
        }

        private void PruneScenePresentation(Scene scene)
        {
            foreach (var manager in new List<PanelManager>(previousFactories.Keys))
                if (manager == null || manager.gameObject.scene == scene) previousFactories.Remove(manager);
        }

        private void AttachPresentation(PanelManager manager)
        {
            if (manager == null || previousFactories.ContainsKey(manager)) return;
            previousFactories.Add(manager, manager.PresenterFactory);
            manager.PresenterFactory = presenterFactory;
        }

        private async UniTask OpenRateUsAsync()
        {
            if (isOpeningRateUs || State != BootstrapState.Ready || !PanelManager.HasInstance) return;
            isOpeningRateUs = true;
            try { await PanelManager.Instance.Transition<RateUsPanel>(Address.RateUsPanel); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception, this); }
            finally { isOpeningRateUs = false; }
        }

        private void ReleasePresentation()
        {
            SceneManager.sceneLoaded -= AttachScenePresentation;
            SceneManager.sceneUnloaded -= PruneScenePresentation;
            foreach (var entry in previousFactories)
                if (entry.Key != null && ReferenceEquals(entry.Key.PresenterFactory, presenterFactory))
                    entry.Key.PresenterFactory = entry.Value;
            previousFactories.Clear();
            presenterFactory = null;
        }
    }
}
