using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using ChroMapper_UIPluginDesigner.UserResources;

namespace ChroMapper_UIPluginDesigner.Components
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

        private Dictionary<GameObject, ElementData> _objToData = new Dictionary<GameObject, ElementData>();

        public void Refresh(List<ElementData> elements, ElementData selectedElement, UnityAction<ElementData> onSelect, UnityAction<ElementData> onUpdate, UnityAction<ElementData, ElementData> onReparent)
        {
            if (_previewMenuBg == null) return;

            foreach (Transform child in _previewMenuBg.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            _objToData.Clear();

            BuildElementsRecursive(elements, _previewMenuBg.transform, selectedElement, onSelect, onUpdate, onReparent);
        }

        private void BuildElementsRecursive(List<ElementData> elements, Transform parent, ElementData selectedElement, UnityAction<ElementData> onSelect, UnityAction<ElementData> onUpdate, UnityAction<ElementData, ElementData> onReparent)
        {
            foreach (var el in elements)
            {
                GameObject obj = null;
                // Using 0.5f, 0.5f for anchors as per previous fix
                switch (el.Type)
                {
                    case ElementType.Button:
                        var btn = _ui.AddButton(parent, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, () => onSelect(el));
                        obj = btn.gameObject;
                        break;
                    case ElementType.Label:
                        var lbl = _ui.AddLabel(parent, el.Name, el.Text, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                        obj = lbl.Item1.gameObject;
                        break;
                    case ElementType.TextInput:
                        var inp = _ui.AddTextInput(parent, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { }, 0.5f, 0.5f);
                        obj = inp.gameObject;
                        break;
                    case ElementType.Dropdown:
                        var dd = _ui.AddDropdown(parent, new List<string> { "Option A" }, 0, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = dd.gameObject;
                        break;
                    case ElementType.Checkbox:
                        var tgl = _ui.AddCheckbox(parent, true, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = tgl.gameObject;
                        break;
                    case ElementType.RadioButton:
                        var radio = _ui.AddCheckbox(parent, false, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { if(v) onSelect(el); });
                        obj = radio.gameObject;
                        var group = parent.GetComponent<ToggleGroup>() ?? parent.gameObject.AddComponent<ToggleGroup>();
                        radio.group = group;
                        _ui.AddLabel(obj.transform, el.Name + "_Label", el.Text, el.SizeX - 20, el.SizeY, 0, 0.5f, 60, 0, TextAlignmentOptions.Left, 12, 0, 0.5f);
                        break;
                    case ElementType.Slider:
                        var slider = _ui.AddSlider(parent, el.Name, el.MinValue, el.MinValue, el.MaxValue, el.IsInteger, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = slider.gameObject;
                        break;
                    case ElementType.Image:
                        obj = new GameObject(el.Name, typeof(RectTransform));
                        obj.transform.SetParent(parent, false);
                        _ui.MoveTransform(obj.transform, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY);
                        Color col;
                        if (!ColorUtility.TryParseHtmlString(el.HexColor, out col)) col = Color.white;
                        _ui.AttachSimpleImage(obj, col);
                        break;
                    case ElementType.VerticalLayout:
                    case ElementType.HorizontalLayout:
                        obj = new GameObject(el.Name);
                        var rt = obj.AddComponent<RectTransform>();
                        rt.SetParent(parent);
                        _ui.MoveTransform(rt, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY);
                        
                        var img = obj.AddComponent<Image>();
                        img.color = new Color(1, 1, 1, 0.1f); // Visual feedback for layout group

                        UnityEngine.UI.HorizontalOrVerticalLayoutGroup group = null;
                        if (el.Type == ElementType.VerticalLayout)
                            group = obj.AddComponent<VerticalLayoutGroup>();
                        else
                            group = obj.AddComponent<HorizontalLayoutGroup>();

                        group.padding = new RectOffset(el.PaddingLeft, el.PaddingRight, el.PaddingTop, el.PaddingBottom);
                        group.spacing = el.Spacing;
                        group.childAlignment = el.Alignment;
                        group.childControlWidth = el.ChildControlWidth;
                        group.childControlHeight = el.ChildControlHeight;
                        group.childForceExpandWidth = el.ChildForceExpandWidth;
                        group.childForceExpandHeight = el.ChildForceExpandHeight;

                        // Click to select layout group
                        var triggerLG = obj.AddComponent<EventTrigger>();
                        var entryLG = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                        entryLG.callback.AddListener((data) => {
                             onSelect(el);
                        });
                        triggerLG.triggers.Add(entryLG);

                        BuildElementsRecursive(el.Children, obj.transform, selectedElement, onSelect, onUpdate, onReparent);
                        break;
                    case ElementType.ScrollRect:
                        obj = new GameObject(el.Name);
                        var rtScroll = obj.AddComponent<RectTransform>();
                        rtScroll.SetParent(parent);
                        _ui.MoveTransform(rtScroll, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY);

                        var scrollRect = obj.AddComponent<ScrollRect>();
                        scrollRect.horizontal = false;
                        scrollRect.vertical = true;
                        scrollRect.scrollSensitivity = el.ScrollSensitivity;
                        scrollRect.verticalScrollbarVisibility = el.ScrollVisibility;

                        var imgScroll = obj.AddComponent<Image>();
                        imgScroll.color = new Color(1, 1, 1, 0.1f);

                        // Viewport
                        var viewportObj = new GameObject("Viewport");
                        var rtViewport = viewportObj.AddComponent<RectTransform>();
                        rtViewport.SetParent(rtScroll);
                        rtViewport.anchorMin = Vector2.zero;
                        rtViewport.anchorMax = Vector2.one;
                        rtViewport.sizeDelta = Vector2.zero;
                        
                        var mask = viewportObj.AddComponent<Mask>();
                        mask.showMaskGraphic = false;
                        var imgViewport = viewportObj.AddComponent<Image>();
                        imgViewport.color = new Color(1, 1, 1, 0.1f);

                        scrollRect.viewport = rtViewport;

                        // Content
                        var contentObj = new GameObject("Content");
                        var rtContent = contentObj.AddComponent<RectTransform>();
                        rtContent.SetParent(rtViewport);
                        rtContent.anchorMin = new Vector2(0, 1);
                        rtContent.anchorMax = new Vector2(1, 1);
                        rtContent.pivot = new Vector2(0.5f, 1);
                        rtContent.sizeDelta = Vector2.zero;

                        var lg = contentObj.AddComponent<VerticalLayoutGroup>();
                        lg.padding = new RectOffset(el.PaddingLeft, el.PaddingRight, el.PaddingTop, el.PaddingBottom);
                        lg.spacing = el.Spacing;
                        lg.childAlignment = el.Alignment;
                        lg.childControlWidth = el.ChildControlWidth;
                        lg.childControlHeight = el.ChildControlHeight;
                        lg.childForceExpandWidth = el.ChildForceExpandWidth;
                        lg.childForceExpandHeight = el.ChildForceExpandHeight;

                        var csf = contentObj.AddComponent<ContentSizeFitter>();
                        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        
                        scrollRect.content = rtContent;

                        // Scrollbar
                        var sbObj = new GameObject("Scrollbar");
                        var rtSb = sbObj.AddComponent<RectTransform>();
                        rtSb.SetParent(rtScroll);
                        rtSb.anchorMin = new Vector2(1, 0);
                        rtSb.anchorMax = new Vector2(1, 1);
                        rtSb.pivot = new Vector2(1, 1);
                        rtSb.sizeDelta = new Vector2(20, 0);
                        rtSb.anchoredPosition = Vector2.zero;
                        
                        var imgSb = sbObj.AddComponent<Image>();
                        imgSb.color = new Color(0, 0, 0, 0.1f);
                        
                        // Handle (Visual only for preview mostly, but let's make it complete)
                        var handleObj = new GameObject("Handle");
                        var rtHandle = handleObj.AddComponent<RectTransform>();
                        rtHandle.SetParent(rtSb);
                        rtHandle.anchorMin = Vector2.zero; 
                        rtHandle.anchorMax = Vector2.one;
                        rtHandle.sizeDelta = Vector2.zero;
                        rtHandle.offsetMin = new Vector2(2, 2);
                        rtHandle.offsetMax = new Vector2(-2, -2);
                        
                        var imgHandle = handleObj.AddComponent<Image>();
                        imgHandle.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);

                        var scrollbar = sbObj.AddComponent<Scrollbar>();
                        scrollbar.handleRect = rtHandle;
                        scrollbar.direction = Scrollbar.Direction.BottomToTop;
                        scrollRect.verticalScrollbar = scrollbar;
                        scrollRect.verticalScrollbarSpacing = -3;

                        // Adjust viewport
                        var vp = scrollRect.viewport;
                        if (vp != null)
                        {
                            vp.offsetMax = new Vector2(-20, 0);
                        }

                        // Trigger for selecting the ScrollRect
                        var triggerSR = obj.AddComponent<EventTrigger>();
                        var entrySR = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                        entrySR.callback.AddListener((data) => { onSelect(el); });
                        triggerSR.triggers.Add(entrySR);

                        BuildElementsRecursive(el.Children, contentObj.transform, selectedElement, onSelect, onUpdate, onReparent);
                        break;
                }

                if (obj != null)
                {
                    _objToData[obj] = el;

                    if (el.Type != ElementType.VerticalLayout && el.Type != ElementType.HorizontalLayout && el.Type != ElementType.ScrollRect)
                    {
                        var trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
                        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                        entry.callback.AddListener((data) => onSelect(el));
                        trigger.triggers.Add(entry);
                    }
                    
                    // Highlight selected
                    if (el == selectedElement)
                    {
                        var outline = obj.AddComponent<UnityEngine.UI.Outline>();
                        outline.effectColor = Color.yellow;
                        outline.effectDistance = new Vector2(2, -2);
                    }

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
                    dragger.OnDragEnd = (eventData) => {
                        onSelect(el);
                        
                        // Raycast to find drop target
                        var results = new List<RaycastResult>();
                        EventSystem.current.RaycastAll(eventData, results);

                        foreach (var result in results)
                        {
                            if (result.gameObject == obj) continue; // Ignore self

                            if (_objToData.ContainsKey(result.gameObject))
                            {
                                var targetData = _objToData[result.gameObject];
                                if (targetData.Type == ElementType.VerticalLayout || targetData.Type == ElementType.HorizontalLayout || targetData.Type == ElementType.ScrollRect)
                                {
                                    onReparent(el, targetData);
                                    return;
                                }
                            }
                            else if (result.gameObject == _previewMenuBg)
                            {
                                onReparent(el, null); // Move to root
                                return;
                            }
                        }
                    };
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
