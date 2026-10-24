using System;
using System.Collections.Generic;
using System.Reflection;
using Dreamy.Core;
using Dreamy.UI;
using NUnit.Framework;
using UnityEngine;

namespace Dreamy.Template.Tests
{
    public sealed class InstallerOwnershipTests
    {
        private GameObject root;
        private GameInstaller installer;

        private interface IFixtureService { }
        private sealed class FixtureService : IFixtureService, IDisposable
        {
            private readonly Action onDispose;

            public FixtureService(Action onDispose = null) => this.onDispose = onDispose;

            public int DisposeCount { get; private set; }
            public void Dispose()
            {
                DisposeCount++;
                onDispose?.Invoke();
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Keep the fixture inactive so Awake cannot start the real bootstrap.
            root = new GameObject("Installer ownership fixture");
            root.SetActive(false);
            installer = root.AddComponent<GameInstaller>();
        }

        [TearDown]
        public void TearDown()
        {
            TearDownServices();
            ServiceLocator.Unregister<IFixtureService>();
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void TearDown_DoesNotUnregisterAReplacement_AndDisposesOwnedInstanceOnce()
        {
            var owned = new FixtureService();
            var replacement = new FixtureService();
            Register(owned, ownsInstance: true);
            ServiceLocator.Register<IFixtureService>(replacement);

            TearDownServices();
            TearDownServices();

            Assert.That(ServiceLocator.Get<IFixtureService>(), Is.SameAs(replacement));
            Assert.That(owned.DisposeCount, Is.EqualTo(1));
            Assert.That(replacement.DisposeCount, Is.Zero);
        }

        [Test]
        public void TearDown_ReleasesRegistrationsInReverseOrder()
        {
            var order = new List<string>();
            var first = new FixtureService(() => order.Add("first"));
            var second = new FixtureService(() => order.Add("second"));
            Register(first, ownsInstance: true);
            Register(second, ownsInstance: true);

            TearDownServices();
            TearDownServices();

            Assert.That(order, Is.EqualTo(new[] { "second", "first" }));
            Assert.That(ServiceLocator.IsRegistered<IFixtureService>(), Is.False);
        }

        [Test]
        public void TearDown_RestoresThePreviousRegistration()
        {
            var previous = new FixtureService();
            var owned = new FixtureService();
            ServiceLocator.Register<IFixtureService>(previous);
            Register(owned, ownsInstance: true);

            TearDownServices();

            Assert.That(ServiceLocator.Get<IFixtureService>(), Is.SameAs(previous));
            Assert.That(owned.DisposeCount, Is.EqualTo(1));
            Assert.That(previous.DisposeCount, Is.Zero);
        }

        [Test]
        public void TearDown_UnregistersButDoesNotDisposeABorrowedService()
        {
            var borrowed = new FixtureService();
            Register(borrowed, ownsInstance: false);

            TearDownServices();

            Assert.That(ServiceLocator.IsRegistered<IFixtureService>(), Is.False);
            Assert.That(borrowed.DisposeCount, Is.Zero);
        }

        [Test]
        public void PresentationTeardown_RestoresPreviousFactory()
        {
            var managerRoot = new GameObject("Panel manager ownership fixture");
            try
            {
                var manager = managerRoot.AddComponent<PanelManager>();
                var previous = new PanelPresenterFactory();
                var owned = new PanelPresenterFactory();
                manager.PresenterFactory = previous;
                SetPresenterFactory(owned);
                AttachPresentation(manager);
                AttachPresentation(manager);
                Assert.That(manager.PresenterFactory, Is.SameAs(owned));

                TearDownServices();
                TearDownServices();

                Assert.That(manager.PresenterFactory, Is.SameAs(previous));
            }
            finally { UnityEngine.Object.DestroyImmediate(managerRoot); }
        }

        [Test]
        public void PresentationTeardown_PreservesReplacementFactory()
        {
            var managerRoot = new GameObject("Panel manager replacement fixture");
            try
            {
                var manager = managerRoot.AddComponent<PanelManager>();
                var owned = new PanelPresenterFactory();
                var replacement = new PanelPresenterFactory();
                SetPresenterFactory(owned);
                AttachPresentation(manager);
                manager.PresenterFactory = replacement;

                TearDownServices();

                Assert.That(manager.PresenterFactory, Is.SameAs(replacement));
            }
            finally { UnityEngine.Object.DestroyImmediate(managerRoot); }
        }

        private void SetPresenterFactory(PanelPresenterFactory factory) =>
            typeof(GameInstaller).GetField("presenterFactory", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(installer, factory);

        private void AttachPresentation(PanelManager manager) =>
            typeof(GameInstaller).GetMethod("AttachPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(installer, new object[] { manager });

        private void Register(IFixtureService service, bool ownsInstance)
        {
            typeof(GameInstaller).GetMethod("RegisterService", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(IFixtureService)).Invoke(installer, new object[] { service, ownsInstance });
        }

        private void TearDownServices()
        {
            typeof(GameInstaller).GetMethod("TearDownServices", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(installer, null);
        }
    }
}
