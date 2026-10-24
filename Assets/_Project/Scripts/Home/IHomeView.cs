using System;

namespace Dreamy.Template.Home
{
    public interface IHomeView
    {
        event Action<HomeDestination> DestinationRequested;

        void SetNavigationInteractable(bool interactable);
    }
}
