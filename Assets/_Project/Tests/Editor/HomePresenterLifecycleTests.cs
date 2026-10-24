using System;
using Dreamy.Template.Home;
using Dreamy.UI;
using NUnit.Framework;

namespace Dreamy.Template.Tests
{
    public sealed class HomePresenterLifecycleTests
    {
        private sealed class View : IHomeView
        {
            private Action<HomeDestination> handlers;
            public int SubscriptionCount => handlers?.GetInvocationList().Length ?? 0;
            public bool Interactable { get; private set; }
            public event Action<HomeDestination> DestinationRequested
            {
                add => handlers += value;
                remove => handlers -= value;
            }
            public void SetNavigationInteractable(bool interactable) => Interactable = interactable;
        }

        [Test]
        public void CachedHomeOpening_BindsOnce_Releases_AndCreatesFreshPresenter()
        {
            var view = new View();
            var factory = new PanelPresenterFactory();
            factory.Register<View>(v => new HomePresenter(v, new HomeNavigator()));
            using var host = new PanelPresenterHost(factory, view);
            host.Show();
            var first = host.Presenter;
            host.Show();
            Assert.That(view.SubscriptionCount, Is.EqualTo(1));
            Assert.That(view.Interactable, Is.True);
            Assert.That(host.Presenter, Is.SameAs(first));

            host.Dispose();
            host.Dispose();
            Assert.That(view.SubscriptionCount, Is.Zero);
            host.Show();
            Assert.That(host.Presenter, Is.Not.SameAs(first));
            Assert.That(view.SubscriptionCount, Is.EqualTo(1));
            host.Dispose();
            Assert.That(view.SubscriptionCount, Is.Zero);
        }
    }
}
