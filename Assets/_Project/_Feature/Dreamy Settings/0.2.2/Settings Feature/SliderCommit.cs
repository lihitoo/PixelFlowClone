using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dreamy.Feature.Settings.Integration
{
    [RequireComponent(typeof(Slider))]
    public sealed class SliderCommit : MonoBehaviour, IPointerUpHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private Slider slider;
        private float committed;
        public Slider Slider => slider;
        public event Action<float> Changed;
        public void Render(float value) { committed = value; slider.SetValueWithoutNotify(value); }
        private void Commit()
        {
            if (Mathf.Approximately(committed, slider.value)) return;
            committed = slider.value; Changed?.Invoke(committed);
        }
        public void OnPointerUp(PointerEventData eventData) => Commit();
        public void OnDeselect(BaseEventData eventData) => Commit();
        public void OnSubmit(BaseEventData eventData) => Commit();
    }
}
