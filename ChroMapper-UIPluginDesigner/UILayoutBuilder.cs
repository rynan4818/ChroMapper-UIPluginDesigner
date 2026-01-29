using System;
using System.Collections.Generic;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChroMapper_UIPluginDesigner
{
    public class UILayoutBuilder
    {
        private HelperUI _ui;
        private Transform _parent;
        private Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();

        public UILayoutBuilder(HelperUI ui, Transform parent)
        {
            _ui = ui;
            _parent = parent;
        }

        public Dictionary<string, GameObject> Build(JSONNode root)
        {
            var arr = root[UIConstants.KeyElements].AsArray;
            foreach (JSONNode n in arr)
            {
                var data = ElementData.FromJSON(n);
                BuildElement(data, _parent);
            }
            return _objects;
        }

        private void BuildElement(ElementData el, Transform currentParent)
        {
            GameObject obj = null;
            
            // HelperUIを使用してエレメントを生成する。
            // JSONデータにはAnchor/Pivot情報が含まれていないため、
            // すべてのエレメントを中央基準 (Pivot: 0.5, 0.5) として統一して生成する。
            // 座標の整合性はJSONデータ作成側で担保するものとする。

            switch (el.Type)
            {
                case ElementType.Button:
                    var btn = _ui.AddButton(currentParent, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = btn.gameObject;
                    break;
                case ElementType.Label:
                    var lbl = _ui.AddLabel(currentParent, el.Name, el.Text, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                    obj = lbl.Item1.gameObject;
                    break;
                case ElementType.TextInput:
                    var inp = _ui.AddTextInput(currentParent, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = inp.gameObject;
                    break;
                case ElementType.Dropdown:
                    var dd = _ui.AddDropdown(currentParent, new List<string>(), 0, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = dd.gameObject;
                    break;
                case ElementType.Checkbox:
                    var tgl = _ui.AddCheckbox(currentParent, true, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = tgl.gameObject;
                    break;
                case ElementType.RadioButton:
                    var radio = _ui.AddCheckbox(currentParent, false, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = radio.gameObject;
                    
                    // Add ToggleGroup to parent if missing
                    var group = currentParent.GetComponent<ToggleGroup>() ?? currentParent.gameObject.AddComponent<ToggleGroup>();
                    radio.group = group;

                    // ChroMapper's Toggle often needs a text label
                    _ui.AddLabel(obj.transform, el.Name + "_Label", el.Text, el.SizeX - 20, el.SizeY, 0, 0.5f, 60, 0, TextAlignmentOptions.Left, 12, 0, 0.5f);
                    break;
                case ElementType.Slider:
                    var slider = _ui.AddSlider(currentParent, el.Name, el.MinValue, el.MinValue, el.MaxValue, el.IsInteger, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = slider.gameObject;
                    break;
                case ElementType.Image:
                    obj = new GameObject(el.Name, typeof(RectTransform));
                    obj.transform.SetParent(currentParent, false);
                    _ui.MoveTransform(obj.transform, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY);
                    Color col;
                    if (!ColorUtility.TryParseHtmlString(el.HexColor, out col)) col = Color.white;
                    _ui.AttachSimpleImage(obj, col);
                    break;
                case ElementType.VerticalLayout:
                case ElementType.HorizontalLayout:
                    obj = new GameObject(el.Name);
                    var rt = obj.AddComponent<RectTransform>();
                    rt.SetParent(currentParent);
                    _ui.MoveTransform(rt, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY);
                    
                    // 背景用にImageを追加（透明にするか、デバッグ用に見えるようにするかは要検討だが、一旦透明に近い色で）
                     var img = obj.AddComponent<Image>();
                    img.color = new Color(0, 0, 0, 0.2f); // 少し見えるようにして配置を確認しやすくする

                    HorizontalOrVerticalLayoutGroup group = null;
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
                    
                    // 子要素の再帰的生成
                    foreach (var childData in el.Children)
                    {
                        BuildElement(childData, obj.transform);
                    }
                    break;
                case ElementType.ScrollRect:
                    obj = new GameObject(el.Name);
                    var rtScroll = obj.AddComponent<RectTransform>();
                    rtScroll.SetParent(currentParent);
                    _ui.MoveTransform(rtScroll, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY);

                    var scrollRect = obj.AddComponent<ScrollRect>();
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.scrollSensitivity = el.ScrollSensitivity;

                    // Image for ScrollRect (transparent background for raycast)
                    var imgScroll = obj.AddComponent<Image>();
                    imgScroll.color = new Color(0, 0, 0, 0.1f);

                    // Viewport
                    var viewportObj = new GameObject("Viewport");
                    var rtViewport = viewportObj.AddComponent<RectTransform>();
                    rtViewport.SetParent(rtScroll);
                    // Stretch Viewport to fill ScrollRect
                    rtViewport.anchorMin = Vector2.zero;
                    rtViewport.anchorMax = Vector2.one;
                    rtViewport.sizeDelta = Vector2.zero;
                    rtViewport.pivot = new Vector2(0, 1); // Top-Left pivot usually for content
                    
                    var imgViewport = viewportObj.AddComponent<Image>();
                    imgViewport.color = new Color(0, 0, 0, 0.1f);
                    var mask = viewportObj.AddComponent<Mask>();
                    mask.showMaskGraphic = false;

                    scrollRect.viewport = rtViewport;

                    // Content
                    var contentObj = new GameObject("Content");
                    var rtContent = contentObj.AddComponent<RectTransform>();
                    rtContent.SetParent(rtViewport);
                    // Top-Left aligned content
                    rtContent.anchorMin = new Vector2(0, 1);
                    rtContent.anchorMax = new Vector2(1, 1); // Stretch width
                    rtContent.pivot = new Vector2(0.5f, 1);
                    rtContent.sizeDelta = new Vector2(0, 0); // Height will be driven by ContentSizeFitter

                    var contentImg = contentObj.AddComponent<Image>();
                    contentImg.color = new Color(0, 0, 0, 0.0f);

                    var layout = contentObj.AddComponent<VerticalLayoutGroup>();
                    layout.padding = new RectOffset(el.PaddingLeft, el.PaddingRight, el.PaddingTop, el.PaddingBottom);
                    layout.spacing = el.Spacing;
                    layout.childAlignment = el.Alignment;
                    layout.childControlWidth = el.ChildControlWidth;
                    layout.childControlHeight = el.ChildControlHeight;
                    layout.childForceExpandWidth = el.ChildForceExpandWidth;
                    layout.childForceExpandHeight = el.ChildForceExpandHeight;

                    var csf = contentObj.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    
                    scrollRect.content = rtContent;

                    // --- Vertical Scrollbar ---
                    var scrollbarObj = new GameObject("Scrollbar Vertical");
                    var rtScrollbar = scrollbarObj.AddComponent<RectTransform>();
                    rtScrollbar.SetParent(rtScroll);
                    rtScrollbar.anchorMin = new Vector2(1, 0);
                    rtScrollbar.anchorMax = new Vector2(1, 1);
                    rtScrollbar.pivot = new Vector2(1, 1);
                    rtScrollbar.sizeDelta = new Vector2(20, 0); // Width 20
                    rtScrollbar.anchoredPosition = Vector2.zero;

                    var imgScrollbarBg = scrollbarObj.AddComponent<Image>();
                    imgScrollbarBg.color = new Color(0, 0, 0, 0.1f);

                    var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
                    scrollbar.direction = Scrollbar.Direction.BottomToTop;

                    // Sliding Area
                    var slidingArea = new GameObject("Sliding Area");
                    var rtSlidingArea = slidingArea.AddComponent<RectTransform>();
                    rtSlidingArea.SetParent(rtScrollbar);
                    rtSlidingArea.anchorMin = Vector2.zero;
                    rtSlidingArea.anchorMax = Vector2.one;
                    rtSlidingArea.sizeDelta = new Vector2(-20, -20);
                    rtSlidingArea.offsetMin = new Vector2(10, 10);
                    rtSlidingArea.offsetMax = new Vector2(-10, -10);

                    // Handle
                    var handleObj = new GameObject("Handle");
                    var rtHandle = handleObj.AddComponent<RectTransform>();
                    rtHandle.SetParent(rtSlidingArea);
                    rtHandle.sizeDelta = new Vector2(20, 20);
                    
                    var imgHandle = handleObj.AddComponent<Image>();
                    imgHandle.color = new Color(0.8f, 0.8f, 0.8f, 1f);

                    scrollbar.targetGraphic = imgHandle;
                    scrollbar.handleRect = rtHandle;
                    
                    scrollRect.verticalScrollbar = scrollbar;
                    scrollRect.verticalScrollbarVisibility = el.ScrollVisibility;
                    scrollRect.verticalScrollbarSpacing = -3;

                    // Adjust Viewport to not overlap scrollbar (optional, but standard)
                    rtViewport.offsetMax = new Vector2(-20, 0); 

                    // Populate Content
                    foreach (var childData in el.Children)
                    {
                        BuildElement(childData, contentObj.transform);
                    }
                    break;
            }

            if (obj != null)
            {
                _objects[el.Name] = obj;
            }
        }
        
        public T Get<T>(string name) where T : Component
        {
            if (_objects.ContainsKey(name))
            {
                return _objects[name].GetComponent<T>();
            }
            return null;
        }

        public GameObject GetObject(string name)
        {
             if (_objects.ContainsKey(name)) return _objects[name];
             return null;
        }
    }
}
