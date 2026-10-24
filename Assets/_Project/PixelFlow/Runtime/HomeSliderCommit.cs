using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelFlow
{
    // A slider drag previews its handle normally and commits once at pointer up
    // or keyboard-focus exit, rather than serializing the profile every frame.
    public sealed class HomeSliderCommit : MonoBehaviour, IPointerUpHandler, IDeselectHandler
    {
        private Action commit;
        public void Initialize(Action callback) => commit = callback;
        public void OnPointerUp(PointerEventData eventData) => commit?.Invoke();
        public void OnDeselect(BaseEventData eventData) => commit?.Invoke();
        private void OnDestroy() => commit = null;
    }
}
