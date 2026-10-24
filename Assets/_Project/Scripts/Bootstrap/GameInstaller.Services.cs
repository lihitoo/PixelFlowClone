using System;
using System.Collections.Generic;
using Dreamy.Core;
using UnityEngine;

namespace Dreamy.Template
{
    public sealed partial class GameInstaller
    {
        private readonly List<Action> teardownActions = new();

        internal void RegisterGameService<T>(T service, bool ownsInstance) where T : class => RegisterService(service, ownsInstance);
        internal void TrackGameTeardown(Action action) => teardownActions.Add(action);

        private void RegisterService<T>(T service, bool ownsInstance = false) where T : class
        {
            ServiceLocator.TryGet<T>(out var previous);
            TrackRegistration(service, previous, ownsInstance);
            ServiceLocator.Register(service);
        }

        private void TrackFeatureInstallation<T>(Func<T> install) where T : class
        {
            ServiceLocator.TryGet<T>(out var previous);
            T service = install();
            TrackRegistration(service, previous, ownsInstance: true);
        }

        private void TrackRegistration<T>(T service, T previous, bool ownsInstance) where T : class
        {
            teardownActions.Add(() =>
            {
                // Never unregister a replacement installed by another owner.
                if (ServiceLocator.TryGet<T>(out var current) && ReferenceEquals(current, service))
                {
                    ServiceLocator.Unregister<T>();
                    if (previous != null) ServiceLocator.Register(previous);
                }

                if (ownsInstance && service is IDisposable disposable) disposable.Dispose();
            });
        }

        private void TearDownServices()
        {
            ReleasePresentation();
            DisposeEntitlementEffects();
            while (teardownActions.Count > 0)
            {
                int index = teardownActions.Count - 1;
                Action teardown = teardownActions[index];
                teardownActions.RemoveAt(index);
                try { teardown(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }

            datasave = null;
            wallet = null;
            walletBalanceSource = null;
            dataConfig = null;
            feedbackHost = null;
        }
    }
}
