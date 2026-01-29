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
