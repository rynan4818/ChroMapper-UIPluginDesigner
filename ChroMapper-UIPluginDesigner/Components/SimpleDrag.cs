using UnityEngine;
using UnityEngine.EventSystems;

namespace ChroMapper_UIPluginDesigner.Components
{
    public class SimpleDrag : MonoBehaviour, IDragHandler
    {
        public RectTransform Target;
        public Canvas Canvas;
        public void OnDrag(PointerEventData eventData)
        {
            Target.anchoredPosition += eventData.delta / Canvas.scaleFactor;
        }
    }
}
