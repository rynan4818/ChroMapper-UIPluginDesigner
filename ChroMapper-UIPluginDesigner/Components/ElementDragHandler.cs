using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ChroMapper_UIPluginDesigner.Components
{
    public class ElementDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<Vector2> OnDragDelta;
        public Action<PointerEventData> OnDragEnd;
        public Canvas Canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Optional: Add logic if needed, otherwise leave empty or handle initialization
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (OnDragDelta != null)
            {
                var parentRT = transform.parent as RectTransform;
                if (parentRT == null) return;

                Vector2 localPos;
                Vector2 prevLocalPos;
                Camera cam = eventData.pressEventCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, eventData.position, cam, out localPos) &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, eventData.position - eventData.delta, cam, out prevLocalPos))
                {
                    OnDragDelta(localPos - prevLocalPos);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnDragEnd?.Invoke(eventData);
        }
    }
}
