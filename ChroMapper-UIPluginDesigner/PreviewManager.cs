using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ChroMapper_UIPluginDesigner
{
    public class PreviewManager
    {
        private HelperUI _ui;
        private GameObject _previewContainer;
        private GameObject _previewMenuBg;
        private Canvas _canvas;

        public GameObject PreviewMenuBg => _previewMenuBg;

        public PreviewManager(HelperUI ui, Canvas canvas)
        {
            _ui = ui;
            _canvas = canvas;
        }

        public void CreateContainer(UnityAction<Vector2> onDrag)
        {
            if (_previewContainer != null) UnityEngine.Object.Destroy(_previewContainer);

            _previewContainer = new GameObject("PreviewContainer", typeof(RectTransform));
            _previewContainer.transform.SetParent(_canvas.transform, false);
            
            // Fullscreen container
            var pcRt = _previewContainer.GetComponent<RectTransform>();
            pcRt.anchorMin = Vector2.zero;
            pcRt.anchorMax = Vector2.one;
            pcRt.sizeDelta = Vector2.zero;
            pcRt.anchoredPosition = Vector2.zero;

            _previewMenuBg = new GameObject("PreviewMenu");
            _previewMenuBg.transform.SetParent(_previewContainer.transform, false);
            _ui.AttachImage(_previewMenuBg, new Color(0.24f, 0.24f, 0.24f));
            _ui.MoveTransform(_previewMenuBg.transform, 250, 190, 0.5f, 0.5f, 70, 180);

            var dragger = _previewMenuBg.AddComponent<ElementDragHandler>();
            dragger.Canvas = _canvas;
            dragger.OnDragDelta = (delta) =>
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                Vector2 newPos = rt.anchoredPosition + delta;
                newPos.x = Mathf.Round(newPos.x);
                newPos.y = Mathf.Round(newPos.y);
                rt.anchoredPosition = newPos;
                onDrag?.Invoke(newPos);
            };
        }

        public void Destroy()
        {
            if (_previewContainer != null) UnityEngine.Object.Destroy(_previewContainer);
        }

        public void UpdateSize(float w, float h)
        {
            if (_previewMenuBg == null) return;
            var rt = _previewMenuBg.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
        }

        public void UpdatePosition(float x, float y)
        {
            if (_previewMenuBg == null) return;
            var rt = _previewMenuBg.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
        }

        public void UpdateAnchor(float ax, float ay)
        {
            if (_previewMenuBg == null) return;
            var rt = _previewMenuBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
        }

        public void Refresh(List<ElementData> elements, ElementData selectedElement, UnityAction<ElementData> onSelect, UnityAction<ElementData> onUpdate)
        {
            if (_previewMenuBg == null) return;

            foreach (Transform child in _previewMenuBg.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            foreach (var el in elements)
            {
                GameObject obj = null;
                // Using 0.5f, 0.5f for anchors as per previous fix
                switch (el.Type)
                {
                    case ElementType.Button:
                        var btn = _ui.AddButton(_previewMenuBg.transform, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, () => onSelect(el));
                        obj = btn.gameObject;
                        break;
                    case ElementType.Label:
                        var lbl = _ui.AddLabel(_previewMenuBg.transform, el.Name, el.Text, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                        obj = lbl.Item1.gameObject;
                        break;
                    case ElementType.TextInput:
                        var inp = _ui.AddTextInput(_previewMenuBg.transform, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { }, 0.5f, 0.5f);
                        obj = inp.gameObject;
                        break;
                    case ElementType.Dropdown:
                        var dd = _ui.AddDropdown(_previewMenuBg.transform, new List<string> { "Option A" }, 0, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = dd.gameObject;
                        break;
                    case ElementType.Checkbox:
                        var tgl = _ui.AddCheckbox(_previewMenuBg.transform, true, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = tgl.gameObject;
                        break;
                }

                if (obj != null)
                {
                    var trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    entry.callback.AddListener((data) => onSelect(el));
                    trigger.triggers.Add(entry);

                    var dragger = obj.AddComponent<ElementDragHandler>();
                    dragger.Canvas = _canvas;
                    dragger.OnDragDelta = (delta) =>
                    {
                        el.AnchorPosX += delta.x;
                        el.AnchorPosY += delta.y;
                        el.AnchorPosX = Mathf.Round(el.AnchorPosX);
                        el.AnchorPosY = Mathf.Round(el.AnchorPosY);

                        var rt = obj.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(el.AnchorPosX, el.AnchorPosY);

                        if (selectedElement == el)
                        {
                            onUpdate(el);
                        }
                    };
                    dragger.OnDragEnd = () => onSelect(el);
                }
            }
        }
        
        public void ApplyLayoutSettings(float w, float h, float ax, float ay, float px, float py)
        {
             if (_previewMenuBg == null) return;
             _ui.MoveTransform(_previewMenuBg.transform, w, h, ax, ay, px, py);
        }
    }
}
