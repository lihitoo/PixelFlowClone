using UnityEngine;

namespace Dreamy.Feedback.Samples
{
    public sealed class FeedbackSampleSafeArea : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int screen;
        private void Update()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea; var size = new Vector2Int(Screen.width, Screen.height);
            if (area == previous && screen == size) return;
            previous = area; screen = size;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height); rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
