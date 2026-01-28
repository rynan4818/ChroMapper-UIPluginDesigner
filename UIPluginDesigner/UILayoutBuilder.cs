using System;
using System.Collections.Generic;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIPluginDesigner
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
                BuildElement(data);
            }
            return _objects;
        }

        private void BuildElement(ElementData el)
        {
            GameObject obj = null;
            
            // HelperUIを使用してエレメントを生成する。
            // JSONデータにはAnchor/Pivot情報が含まれていないため、
            // すべてのエレメントを中央基準 (Pivot: 0.5, 0.5) として統一して生成する。
            // 座標の整合性はJSONデータ作成側で担保するものとする。

            switch (el.Type)
            {
                case ElementType.Button:
                    var btn = _ui.AddButton(_parent, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = btn.gameObject;
                    break;
                case ElementType.Label:
                    var lbl = _ui.AddLabel(_parent, el.Name, el.Text, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                    obj = lbl.Item1.gameObject;
                    break;
                case ElementType.TextInput:
                    var inp = _ui.AddTextInput(_parent, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = inp.gameObject;
                    break;
                case ElementType.Dropdown:
                    var dd = _ui.AddDropdown(_parent, new List<string>(), 0, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = dd.gameObject;
                    break;
                case ElementType.Checkbox:
                    var tgl = _ui.AddCheckbox(_parent, true, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = tgl.gameObject;
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
