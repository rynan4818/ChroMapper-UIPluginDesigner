using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using SFB;
using System.IO;
using System;
using Newtonsoft.Json;

namespace UIPluginDesigner
{
    // --- Main Designer Logic (Mimicking MenuUI behavior logic but for editing) ---

    public class DesignerController : MonoBehaviour
    {
        // 参照用に Plugin.ui を使用
        private HelperUI Ui => Plugin.ui;

        private GameObject _editorPanel;
        private GameObject _previewContainer;
        private GameObject _previewMenuBg;

        private List<ElementData> _elements = new List<ElementData>();
        private ElementData _selectedElement;

        // Inspector Inputs
        private UITextInput _inputName;
        private UITextInput _inputText;
        private UITextInput _inputX, _inputY, _inputW, _inputH;
        private UITextInput _inputFontSize;
        private UITextInput _inputMenuW, _inputMenuH, _inputMenuX, _inputMenuY;

        private void Start()
        {
            Debug.Log("DesignerController: Start");
            CreateEditorPanel();
            CreatePreviewContainer();
        }

        private void OnDestroy()
        {
            Debug.Log("DesignerController: OnDestroy");
            if (_editorPanel != null) Destroy(_editorPanel);
            if (_previewContainer != null) Destroy(_previewContainer);
        }

        private Canvas GetCanvas()
        {
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO != null) return canvasGO.GetComponent<Canvas>();
            return FindObjectOfType<Canvas>();
        }

        private void CreateEditorPanel()
        {
            var canvas = GetCanvas();
            if (canvas == null)
            {
                Debug.LogError("DesignerController: Canvas not found!");
                return;
            }

            _editorPanel = new GameObject("EditorPanel", typeof(RectTransform));
            _editorPanel.transform.SetParent(canvas.transform, false);

            Ui.AttachImage(_editorPanel, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            Ui.MoveTransform(_editorPanel.transform, 140, 300, 0, 0.5f, 70, 0);

            var drag = _editorPanel.AddComponent<SimpleDrag>();
            drag.Target = _editorPanel.transform as RectTransform;
            drag.Canvas = canvas;

            // -- Palette Buttons --
            float y = -5;
            AddPaletteBtn("Button", ElementType.Button, ref y);
            AddPaletteBtn("Label", ElementType.Label, ref y);
            AddPaletteBtn("Input", ElementType.TextInput, ref y);
            AddPaletteBtn("Dropdown", ElementType.Dropdown, ref y);
            AddPaletteBtn("Checkbox", ElementType.Checkbox, ref y);

            y -= 5;
            Ui.AddButton(_editorPanel.transform, "Save", "Save", 7, 30, 12, 0.5f, 1, -18, y, SaveLayout);
            Ui.AddButton(_editorPanel.transform, "Load", "Load", 7, 30, 12, 0.5f, 1, 18, y, LoadLayout);

            y -= 16;
            Ui.AddButton(_editorPanel.transform, "Export", "Export Code", 7, 50, 12, 0.5f, 1, 0, y, ExportCode);

            y -= 16;
            Ui.AddButton(_editorPanel.transform, "Close", "Close", 7, 50, 12, 0.5f, 1, 0, y, () => Destroy(gameObject));

            // -- Menu Settings --
            y -= 18;
            // Menu Size
            Ui.AddLabel(_editorPanel.transform, "MenuSizeL", "Menu Size", 100, 14, 0, 1, 70, y, TextAlignmentOptions.Center, 7);
            y -= 14;
            _inputMenuW = Ui.AddTextInput(_editorPanel.transform, "MenuW", "250", TextAlignmentOptions.Center, 6, 40, 14, 0, 1, 45, y, (v) => UpdateMenuSize());
            _inputMenuH = Ui.AddTextInput(_editorPanel.transform, "MenuH", "190", TextAlignmentOptions.Center, 6, 40, 14, 0, 1, 95, y, (v) => UpdateMenuSize());
            
            var adjW = _inputMenuW.gameObject.AddComponent<InputNumberAdjuster>();
            adjW.InputField = _inputMenuW.InputField;
            var adjH = _inputMenuH.gameObject.AddComponent<InputNumberAdjuster>();
            adjH.InputField = _inputMenuH.InputField;

            y -= 18;
            // Menu Position
            Ui.AddLabel(_editorPanel.transform, "MenuPosL", "Menu Pos", 100, 14, 0, 1, 70, y, TextAlignmentOptions.Center, 7);
            y -= 14;
            _inputMenuX = Ui.AddTextInput(_editorPanel.transform, "MenuX", "0", TextAlignmentOptions.Center, 6, 40, 14, 0, 1, 45, y, (v) => UpdateMenuPos());
            _inputMenuY = Ui.AddTextInput(_editorPanel.transform, "MenuY", "0", TextAlignmentOptions.Center, 6, 40, 14, 0, 1, 95, y, (v) => UpdateMenuPos());

            var adjX = _inputMenuX.gameObject.AddComponent<InputNumberAdjuster>();
            adjX.InputField = _inputMenuX.InputField;
            var adjY = _inputMenuY.gameObject.AddComponent<InputNumberAdjuster>();
            adjY.InputField = _inputMenuY.InputField;

            // -- Inspector Area (Bottom half) --
            float inspY = -175;
            Ui.AddLabel(_editorPanel.transform, "InspTitle", "Inspector", 80, 14, 0.5f, 1, 0, inspY, TextAlignmentOptions.Center, 7);
            inspY -= 16;

            Ui.AddButton(_editorPanel.transform, "DeleteElement", "DEL", 7, 35, 14, 0.5f, 1, -20, inspY, DeleteSelectedElement);
            Ui.AddButton(_editorPanel.transform, "CopyElement", "COPY", 7, 35, 14, 0.5f, 1, 20, inspY, CopySelectedElement);
            inspY -= 18;

            _inputName = CreateInspectorInput("Name", ref inspY);
            _inputText = CreateInspectorInput("Text", ref inspY);
            _inputX = CreateInspectorInput("Pos X", ref inspY, true);
            _inputY = CreateInspectorInput("Pos Y", ref inspY, true);
            _inputW = CreateInspectorInput("Size W", ref inspY, true);
            _inputH = CreateInspectorInput("Size H", ref inspY, true);
            _inputFontSize = CreateInspectorInput("Font Size", ref inspY, true);
        }

        private void UpdateMenuSize()
        {
            if (_previewMenuBg == null) return;
            if (float.TryParse(_inputMenuW.InputField.text, out float w) && 
                float.TryParse(_inputMenuH.InputField.text, out float h))
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(w, h);
            }
        }

        private void UpdateMenuPos()
        {
            if (_previewMenuBg == null) return;
            if (float.TryParse(_inputMenuX.InputField.text, out float x) && 
                float.TryParse(_inputMenuY.InputField.text, out float y))
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
            }
        }

        private void AddPaletteBtn(string text, ElementType type, ref float y)
        {
            Ui.AddButton(_editorPanel.transform, "Add" + text, "+ " + text, 8, 70, 14, 0.5f, 1, 0, y, () => {
                Debug.Log("Palette Button Clicked: " + type);
                AddElement(type);
            });
            y -= 12; // Reduced spacing
        }

        private UITextInput CreateInspectorInput(string label, ref float y, bool isNumeric = false)
        {
            Ui.AddLabel(_editorPanel.transform, label + "_L", label, 40, 12, 0, 1, 40, y, TextAlignmentOptions.Right, 7);
            var input = Ui.AddTextInput(_editorPanel.transform, label + "_I", "", TextAlignmentOptions.Left, 5, 50, 14, 0, 1, 90, y, (val) => UpdateSelectedElement());
            
            if (isNumeric)
            {
                var adjuster = input.gameObject.AddComponent<InputNumberAdjuster>();
                adjuster.InputField = input.InputField;
            }

            y -= 12;
            return input;
        }

        private void CreatePreviewContainer()
        {
            var canvas = GetCanvas();
            _previewContainer = new GameObject("PreviewContainer", typeof(RectTransform));
            _previewContainer.transform.SetParent(canvas.transform, false);
            Ui.MoveTransform(_previewContainer.transform, 0, 0, 0.5f, 0.5f, 0, 0);

            // Create the Menu Background
            _previewMenuBg = new GameObject("PreviewMenu");
            _previewMenuBg.transform.SetParent(_previewContainer.transform, false);
            Ui.AttachImage(_previewMenuBg, new Color(0.24f, 0.24f, 0.24f));
            Ui.MoveTransform(_previewMenuBg.transform, 250, 190, 0.5f, 0.5f, 0, 0);

            var dragger = _previewMenuBg.AddComponent<ElementDragHandler>();
            dragger.Canvas = canvas;
            dragger.OnDragDelta = (delta) =>
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                Vector2 newPos = rt.anchoredPosition + delta;
                newPos.x = Mathf.Round(newPos.x);
                newPos.y = Mathf.Round(newPos.y);
                rt.anchoredPosition = newPos;
                
                if (_inputMenuX != null && _inputMenuX.InputField != null)
                    _inputMenuX.InputField.SetTextWithoutNotify(newPos.x.ToString("F0"));
                if (_inputMenuY != null && _inputMenuY.InputField != null)
                    _inputMenuY.InputField.SetTextWithoutNotify(newPos.y.ToString("F0"));
            };
        }

        private void CopySelectedElement()
        {
            if (_selectedElement == null) return;
            var newEl = new ElementData
            {
                Type = _selectedElement.Type,
                Name = _selectedElement.Type.ToString() + _elements.Count,
                Text = _selectedElement.Text,
                AnchorPosX = _selectedElement.AnchorPosX + 10,
                AnchorPosY = _selectedElement.AnchorPosY - 10,
                SizeX = _selectedElement.SizeX,
                SizeY = _selectedElement.SizeY,
                FontSize = _selectedElement.FontSize
            };
            _elements.Add(newEl);
            SelectElement(newEl);
            RefreshPreview();
        }

        private void DeleteSelectedElement()
        {
            if (_selectedElement == null) return;
            _elements.Remove(_selectedElement);
            _selectedElement = null;
            
            _inputName.InputField.text = "";
            _inputText.InputField.text = "";
            _inputX.InputField.text = "";
            _inputY.InputField.text = "";
            _inputW.InputField.text = "";
            _inputH.InputField.text = "";
            _inputFontSize.InputField.text = "";

            RefreshPreview();
        }

        private void AddElement(ElementType type)
        {
            Debug.Log("AddElement: " + type);
            var data = new ElementData
            {
                Type = type,
                Name = type.ToString() + _elements.Count,
                Text = type.ToString(),
                SizeX = 160,
                SizeY = 40,
                AnchorPosX = 0,
                AnchorPosY = 0,
                FontSize = 14
            };

            if (type == ElementType.Label) { data.SizeX = 200; data.SizeY = 30; }
            if (type == ElementType.Button) { data.SizeX = 100; data.SizeY = 30; }

            _elements.Add(data);
            SelectElement(data);
            RefreshPreview();
        }

        private void SelectElement(ElementData data)
        {
            _selectedElement = data;

            _inputName.InputField.SetTextWithoutNotify(data.Name);
            _inputText.InputField.SetTextWithoutNotify(data.Text);
            _inputX.InputField.SetTextWithoutNotify(data.AnchorPosX.ToString());
            _inputY.InputField.SetTextWithoutNotify(data.AnchorPosY.ToString());
            _inputW.InputField.SetTextWithoutNotify(data.SizeX.ToString());
            _inputH.InputField.SetTextWithoutNotify(data.SizeY.ToString());
            _inputFontSize.InputField.SetTextWithoutNotify(data.FontSize.ToString());
        }

        private void UpdateSelectedElement()
        {
            if (_selectedElement == null) return;

            _selectedElement.Name = _inputName.InputField.text;
            _selectedElement.Text = _inputText.InputField.text;

            if (float.TryParse(_inputX.InputField.text, out float x)) _selectedElement.AnchorPosX = x;
            if (float.TryParse(_inputY.InputField.text, out float y)) _selectedElement.AnchorPosY = y;
            if (float.TryParse(_inputW.InputField.text, out float w)) _selectedElement.SizeX = w;
            if (float.TryParse(_inputH.InputField.text, out float h)) _selectedElement.SizeY = h;
            if (float.TryParse(_inputFontSize.InputField.text, out float f)) _selectedElement.FontSize = f;

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            Debug.Log("RefreshPreview: Elements count = " + _elements.Count);
            foreach (Transform child in _previewMenuBg.transform)
            {
                Destroy(child.gameObject);
            }

            var canvas = GetCanvas();

            foreach (var el in _elements)
            {
                GameObject obj = null;
                switch (el.Type)
                {
                    case ElementType.Button:
                        var btn = Ui.AddButton(_previewMenuBg.transform, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 1, 1, el.AnchorPosX, el.AnchorPosY, () => SelectElement(el));
                        obj = btn.gameObject;
                        break;
                    case ElementType.Label:
                        var lbl = Ui.AddLabel(_previewMenuBg.transform, el.Name, el.Text, el.SizeX, el.SizeY, 1, 1, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                        obj = lbl.Item1.gameObject;
                        break;
                    case ElementType.TextInput:
                        var inp = Ui.AddTextInput(_previewMenuBg.transform, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 1, 1, el.AnchorPosX, el.AnchorPosY, (v) => { }, 0.5f, 0.5f);
                        obj = inp.gameObject;
                        break;
                    case ElementType.Dropdown:
                        var dd = Ui.AddDropdown(_previewMenuBg.transform, new List<string> { "Option A" }, 0, el.SizeX, el.SizeY, 1, 1, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = dd.gameObject;
                        break;
                    case ElementType.Checkbox:
                        var tgl = Ui.AddCheckbox(_previewMenuBg.transform, true, el.SizeX, el.SizeY, 1, 1, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = tgl.gameObject;
                        break;
                }

                if (obj != null)
                {
                    // Add Click Trigger (Select)
                    var trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    entry.callback.AddListener((data) => SelectElement(el));
                    trigger.triggers.Add(entry);

                    // Add Drag Handler
                    var dragger = obj.AddComponent<ElementDragHandler>();
                    dragger.Canvas = canvas;
                    dragger.OnDragDelta = (delta) =>
                    {
                        // Update Data
                        el.AnchorPosX += delta.x;
                        el.AnchorPosY += delta.y;
                        
                        // Round to integer
                        el.AnchorPosX = Mathf.Round(el.AnchorPosX);
                        el.AnchorPosY = Mathf.Round(el.AnchorPosY);

                        // Update Visuals (No RefreshPreview to avoid lag/recreation)
                        var rt = obj.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(el.AnchorPosX, el.AnchorPosY);

                        // Update Inspector if selected
                        if (_selectedElement == el)
                        {
                            _inputX.InputField.SetTextWithoutNotify(el.AnchorPosX.ToString("F0"));
                            _inputY.InputField.SetTextWithoutNotify(el.AnchorPosY.ToString("F0"));
                        }
                    };
                    dragger.OnDragEnd = () =>
                    {
                         SelectElement(el); // Refresh inspector to be sure
                    };
                }
            }
        }

        private void SaveLayout()
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Save UI Layout", "", "layout", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = new LayoutData
            {
                MenuW = _previewMenuBg.GetComponent<RectTransform>().rect.width,
                MenuH = _previewMenuBg.GetComponent<RectTransform>().rect.height,
                Elements = _elements
            };

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
            PersistentUI.Instance.DisplayMessage("Layout saved!", PersistentUI.DisplayMessageType.Bottom);
        }

        private void LoadLayout()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Load UI Layout", "", "json", false);
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

            string json = File.ReadAllText(paths[0]);
            var data = JsonConvert.DeserializeObject<LayoutData>(json);

            if (data != null)
            {
                _elements = data.Elements;
                _inputMenuW.InputField.SetTextWithoutNotify(data.MenuW.ToString());
                _inputMenuH.InputField.SetTextWithoutNotify(data.MenuH.ToString());
                UpdateMenuSize();
                RefreshPreview();
                PersistentUI.Instance.DisplayMessage("Layout loaded!", PersistentUI.DisplayMessageType.Bottom);
            }
        }

        private void ExportCode()
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Export Generated Code", "", "GeneratedUI", "txt");
            if (string.IsNullOrEmpty(path)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// --- Generated Code (UI.cs style) ---");
            sb.AppendLine("// Add this to your MenuUI.cs or equivalent");
            sb.AppendLine("private void CreateUI(UI ui, GameObject menu)");
            sb.AppendLine("{");

            foreach (var el in _elements)
            {
                string x = el.AnchorPosX.ToString("F1") + "f";
                string y = el.AnchorPosY.ToString("F1") + "f";
                string w = el.SizeX.ToString("F0");
                string h = el.SizeY.ToString("F0");
                string f = el.FontSize.ToString("F0");

                switch (el.Type)
                {
                    case ElementType.Button:
                        sb.AppendLine($"    ui.AddButton(menu.transform, \"{el.Name}\", \"{el.Text}\", {f}, {w}, {h}, 1, 1, {x}, {y}, () => {{}}); // Note: Click handler is a placeholder");
                        break;
                    case ElementType.Label:
                        sb.AppendLine($"    ui.AddLabel(menu.transform, \"{el.Name}\", \"{el.Text}\", {w}, {h}, 1, 1, {x}, {y}, TextAlignmentOptions.Center, {f}");
                        break;
                    case ElementType.TextInput:
                        sb.AppendLine($"    ui.AddTextInput(menu.transform, \"{el.Name}\", \"{el.Text}\", TextAlignmentOptions.Left, {f}, {w}, {h}, 1, 1, {x}, {y}, (val) => {{}}); // Note: OnChange handler is a placeholder");
                        break;
                    case ElementType.Dropdown:
                        sb.AppendLine($"    ui.AddDropdown(menu.transform, new List<string>(), 0, {w}, {h}, 1, 1, {x}, {y}, (val) => {{}}); // Note: OnChange handler is a placeholder");
                        break;
                    case ElementType.Checkbox:
                        sb.AppendLine($"    ui.AddCheckbox(menu.transform, true, {w}, {h}, 1, 1, {x}, {y}, (val) => {{}}); // Note: OnValueChanged handler is a placeholder");
                        break;
                }
            }
            sb.AppendLine("}");
            
            File.WriteAllText(path, sb.ToString());
            PersistentUI.Instance.DisplayMessage("Code exported to file!", PersistentUI.DisplayMessageType.Bottom);
        }

        [Serializable]
        private class ElementData
        {
            public ElementType Type;
            public string Name;
            public string Text;
            public float AnchorPosX, AnchorPosY;
            public float SizeX, SizeY;
            public float FontSize;
        }

        [Serializable]
        private class LayoutData
        {
            public float MenuW, MenuH;
            public List<ElementData> Elements;
        }

        private enum ElementType { Button, Label, TextInput, Dropdown, Checkbox }
    }
}
