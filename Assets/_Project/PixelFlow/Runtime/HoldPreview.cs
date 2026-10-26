using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelFlow
{
    public sealed class HoldPreview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private bool holding;
        public event Action<bool> Changed;
        public void OnPointerDown(PointerEventData data)
        { if (data.button == PointerEventData.InputButton.Left) SetHolding(true); }
        public void OnPointerUp(PointerEventData data) => SetHolding(false);
        public void OnPointerExit(PointerEventData data) => SetHolding(false);
        private void OnDisable() => SetHolding(false);
        private void SetHolding(bool value)
        { if (holding == value) return; holding = value; Changed?.Invoke(value); }
    }
}
