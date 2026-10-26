using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelFlow
{
    public sealed class BoardCellView : MonoBehaviour, IPointerClickHandler
    {
        private Action<int, int> selected;
        private int x, y;
        public void Initialize(int column, int row, Action<int, int> callback)
        { x = column; y = row; selected = callback; }
        public void OnPointerClick(PointerEventData data)
        { if (data.button == PointerEventData.InputButton.Left) selected?.Invoke(x, y); }
        private void OnDestroy() => selected = null;
    }
}
