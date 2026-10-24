using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelFlow
{
    public sealed class PigView : MonoBehaviour, IPointerClickHandler
    {
        public int ShooterId { get; private set; }
        private Action<int> selected;
        private TMP_Text ammo;
        private GameObject marker;
        private Camera worldCamera;

        public void Initialize(int id, Action<int> callback, TMP_Text label, GameObject selectionMarker, Camera camera)
        {
            ShooterId = id; selected = callback; ammo = label; marker = selectionMarker; worldCamera = camera;
        }
        public void SetState(int ammunition, bool selectable)
        {
            ammo.SetText("{0}", ammunition);
            marker.SetActive(selectable);
            ammo.transform.rotation = worldCamera.transform.rotation;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) selected?.Invoke(ShooterId);
        }
        private void OnDestroy() => selected = null;
    }
}
