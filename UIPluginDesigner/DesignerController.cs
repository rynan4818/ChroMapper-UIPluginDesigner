using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using System.IO;
using System;
using SimpleJSON;

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
        private UITextInput _inputAnchorX, _inputAnchorY;

        public void Start()
        {
            if (Ui == null) Debug.LogError("DesignerController: Plugin.ui is null in Start!");
            CreateEditorPanel();
            CreatePreviewContainer();
        }

        public void OnDestroy()
        {
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

            // Load Layout from Embedded Resource
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "UIPluginDesigner.editor_layout.json";

            string json = null;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }

            if (!string.IsNullOrEmpty(json))
            {
                var root = JSON.Parse(json);

                _editorPanel = new GameObject("EditorPanel", typeof(RectTransform));
                _editorPanel.transform.SetParent(canvas.transform, false);

                // Apply Panel Settings from JSON
                float pW = root["PanelWidth"] != null ? root["PanelWidth"].AsFloat : 140;
                float pH = root["PanelHeight"] != null ? root["PanelHeight"].AsFloat : 300;
                float pAX = root["PanelAnchorX"] != null ? root["PanelAnchorX"].AsFloat : 0;
                float pAY = root["PanelAnchorY"] != null ? root["PanelAnchorY"].AsFloat : 0.5f;
                float pX = root["PanelPosX"] != null ? root["PanelPosX"].AsFloat : 70;
                float pY = root["PanelPosY"] != null ? root["PanelPosY"].AsFloat : 0;
                
                Ui.AttachImage(_editorPanel, new Color(0.1f, 0.1f, 0.1f, 0.95f));
                Ui.MoveTransform(_editorPanel.transform, pW, pH, pAX, pAY, pX, pY);

                var drag = _editorPanel.AddComponent<SimpleDrag>();
                drag.Target = _editorPanel.transform as RectTransform;
                drag.Canvas = canvas;
                
                var builder = new UILayoutBuilder(Ui, _editorPanel.transform);
                var objects = builder.Build(root);
                
                BindEditorEvents(builder);
            }
            else
            {
                Debug.LogError("Editor Layout Resource not found: " + resourceName);
                
                _editorPanel = new GameObject("EditorPanel", typeof(RectTransform));
                _editorPanel.transform.SetParent(canvas.transform, false);
                Ui.MoveTransform(_editorPanel.transform, 140, 300, 0, 0.5f, 70, 0); // Fallback
                
                Ui.AddLabel(_editorPanel.transform, "Error", "Layout Resource not found", 140, 50, 0.5f, 0.5f, 0, 0, TextAlignmentOptions.Center, 12);
            }
        }

        private void BindEditorEvents(UILayoutBuilder builder)
        {
            // Palette
            float y = -5; // dummy ref
            if (builder.GetObject("AddButton") != null) builder.Get<Button>("AddButton").onClick.AddListener(() => AddElement(ElementType.Button));
            if (builder.GetObject("AddLabel") != null) builder.Get<Button>("AddLabel").onClick.AddListener(() => AddElement(ElementType.Label));
            if (builder.GetObject("AddInput") != null) builder.Get<Button>("AddInput").onClick.AddListener(() => AddElement(ElementType.TextInput));
            if (builder.GetObject("AddDropdown") != null) builder.Get<Button>("AddDropdown").onClick.AddListener(() => AddElement(ElementType.Dropdown));
            if (builder.GetObject("AddCheckbox") != null) builder.Get<Button>("AddCheckbox").onClick.AddListener(() => AddElement(ElementType.Checkbox));

            // Actions
            if (builder.GetObject("Save") != null) builder.Get<Button>("Save").onClick.AddListener(SaveLayout);
            if (builder.GetObject("Load") != null) builder.Get<Button>("Load").onClick.AddListener(LoadLayout);
            if (builder.GetObject("Export") != null) builder.Get<Button>("Export").onClick.AddListener(ExportCode);
            if (builder.GetObject("Close") != null) builder.Get<Button>("Close").onClick.AddListener(() => Destroy(gameObject));

            // Menu Size Inputs
            _inputMenuW = builder.Get<UITextInput>("MenuW");
            _inputMenuH = builder.Get<UITextInput>("MenuH");
            
            if (_inputMenuW != null) {
                _inputMenuW.InputField.onEndEdit.AddListener((v) => UpdateMenuSize());
                _inputMenuW.InputField.onValueChanged.AddListener((v) => UpdateMenuSize());
                var adj = _inputMenuW.gameObject.AddComponent<InputNumberAdjuster>();
                adj.InputField = _inputMenuW.InputField;
            }
            if (_inputMenuH != null) {
                _inputMenuH.InputField.onEndEdit.AddListener((v) => UpdateMenuSize());
                _inputMenuH.InputField.onValueChanged.AddListener((v) => UpdateMenuSize());
                var adj = _inputMenuH.gameObject.AddComponent<InputNumberAdjuster>();
                adj.InputField = _inputMenuH.InputField;
            }

            // Menu Pos Inputs
            _inputMenuX = builder.Get<UITextInput>("MenuX");
            _inputMenuY = builder.Get<UITextInput>("MenuY");

            if (_inputMenuX != null) {
                _inputMenuX.InputField.onEndEdit.AddListener((v) => UpdateMenuPos());
                _inputMenuX.InputField.onValueChanged.AddListener((v) => UpdateMenuPos());
                var adj = _inputMenuX.gameObject.AddComponent<InputNumberAdjuster>();
                adj.InputField = _inputMenuX.InputField;
            }
            if (_inputMenuY != null) {
                _inputMenuY.InputField.onEndEdit.AddListener((v) => UpdateMenuPos());
                _inputMenuY.InputField.onValueChanged.AddListener((v) => UpdateMenuPos());
                var adj = _inputMenuY.gameObject.AddComponent<InputNumberAdjuster>();
                adj.InputField = _inputMenuY.InputField;
            }

            // Menu Anchor Inputs
            _inputAnchorX = builder.Get<UITextInput>("MenuAnchorX");
            _inputAnchorY = builder.Get<UITextInput>("MenuAnchorY");

            if (_inputAnchorX != null) {
                _inputAnchorX.InputField.onEndEdit.AddListener((v) => UpdateMenuAnchor());
                _inputAnchorX.InputField.onValueChanged.AddListener((v) => UpdateMenuAnchor());
                var adj = _inputAnchorX.gameObject.AddComponent<InputNumberAdjuster>();
                adj.InputField = _inputAnchorX.InputField;
            }
            if (_inputAnchorY != null) {
                _inputAnchorY.InputField.onEndEdit.AddListener((v) => UpdateMenuAnchor());
                _inputAnchorY.InputField.onValueChanged.AddListener((v) => UpdateMenuAnchor());
                var adj = _inputAnchorY.gameObject.AddComponent<InputNumberAdjuster>();
                adj.InputField = _inputAnchorY.InputField;
            }

            // Inspector Actions
            if (builder.GetObject("DeleteElement") != null) builder.Get<Button>("DeleteElement").onClick.AddListener(DeleteSelectedElement);
            if (builder.GetObject("CopyElement") != null) builder.Get<Button>("CopyElement").onClick.AddListener(CopySelectedElement);

            // Inspector Inputs
            _inputName = BindInspectorInput(builder, "Name", false);
            _inputText = BindInspectorInput(builder, "Text", false);
            _inputX = BindInspectorInput(builder, "PosX", true); // JSON Name is PosX_I
            _inputY = BindInspectorInput(builder, "PosY", true);
            _inputW = BindInspectorInput(builder, "SizeW", true);
            _inputH = BindInspectorInput(builder, "SizeH", true);
            _inputFontSize = BindInspectorInput(builder, "FontSize", true);
        }

        private UITextInput BindInspectorInput(UILayoutBuilder builder, string prefix, bool numeric)
        {
            var input = builder.Get<UITextInput>(prefix + "_I");
            if (input != null)
            {
                // Use onValueChanged for real-time updates
                input.InputField.onValueChanged.AddListener((val) => UpdateSelectedElement());
                input.InputField.onEndEdit.AddListener((val) => UpdateSelectedElement());
                
                if (numeric)
                {
                    var adj = input.gameObject.AddComponent<InputNumberAdjuster>();
                    adj.InputField = input.InputField;
                }
            }
            return input;
        }

        private void UpdateMenuSize()
        {
            if (_previewMenuBg == null) return;
            if (_inputMenuW != null && _inputMenuW.InputField != null && 
                float.TryParse(_inputMenuW.InputField.text, out float w) && 
                _inputMenuH != null && _inputMenuH.InputField != null && 
                float.TryParse(_inputMenuH.InputField.text, out float h))
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(w, h);
            }
        }

        private void UpdateMenuPos()
        {
            if (_previewMenuBg == null) return;
            if (_inputMenuX != null && _inputMenuX.InputField != null && 
                float.TryParse(_inputMenuX.InputField.text, out float x) && 
                _inputMenuY != null && _inputMenuY.InputField != null && 
                float.TryParse(_inputMenuY.InputField.text, out float y))
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
            }
        }

        private void UpdateMenuAnchor()
        {
            if (_previewMenuBg == null) return;
            if (_inputAnchorX != null && _inputAnchorX.InputField != null && 
                float.TryParse(_inputAnchorX.InputField.text, out float ax) && 
                _inputAnchorY != null && _inputAnchorY.InputField != null && 
                float.TryParse(_inputAnchorY.InputField.text, out float ay))
            {
                var rt = _previewMenuBg.GetComponent<RectTransform>();
                // Assuming Point Anchor (Min=Max)
                rt.anchorMin = new Vector2(ax, ay);
                rt.anchorMax = new Vector2(ax, ay);
                // Keep pivot as is (usually 0.5, 0.5 from MoveTransform) to avoid unexpected shifts
            }
        }



        private void CreatePreviewContainer()
        {
            var canvas = GetCanvas();
            if (canvas == null)
            {
                Debug.LogError("DesignerController: Canvas not found for PreviewContainer");
                return;
            }
            _previewContainer = new GameObject("PreviewContainer", typeof(RectTransform));
            _previewContainer.transform.SetParent(canvas.transform, false);
            // Make PreviewContainer full screen so anchors work relative to screen
            var pcRt = _previewContainer.GetComponent<RectTransform>();
            pcRt.anchorMin = Vector2.zero;
            pcRt.anchorMax = Vector2.one;
            pcRt.sizeDelta = Vector2.zero;
            pcRt.anchoredPosition = Vector2.zero;

            // Create the Menu Background
            _previewMenuBg = new GameObject("PreviewMenu");
            _previewMenuBg.transform.SetParent(_previewContainer.transform, false);
            Ui.AttachImage(_previewMenuBg, new Color(0.24f, 0.24f, 0.24f));
            Ui.MoveTransform(_previewMenuBg.transform, 250, 190, 0.5f, 0.5f, 70, 180);
            
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
            if (_elements == null) _elements = new List<ElementData>();

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

            if (_inputName != null && _inputName.InputField != null) _inputName.InputField.SetTextWithoutNotify(data.Name);
            if (_inputText != null && _inputText.InputField != null) _inputText.InputField.SetTextWithoutNotify(data.Text);
            if (_inputX != null && _inputX.InputField != null) _inputX.InputField.SetTextWithoutNotify(data.AnchorPosX.ToString());
            if (_inputY != null && _inputY.InputField != null) _inputY.InputField.SetTextWithoutNotify(data.AnchorPosY.ToString());
            if (_inputW != null && _inputW.InputField != null) _inputW.InputField.SetTextWithoutNotify(data.SizeX.ToString());
            if (_inputH != null && _inputH.InputField != null) _inputH.InputField.SetTextWithoutNotify(data.SizeY.ToString());
            if (_inputFontSize != null && _inputFontSize.InputField != null) _inputFontSize.InputField.SetTextWithoutNotify(data.FontSize.ToString());
        }

        private void UpdateSelectedElement()
        {
            if (_selectedElement == null) return;

            if (_inputName != null && _inputName.InputField != null) _selectedElement.Name = _inputName.InputField.text;
            if (_inputText != null && _inputText.InputField != null) _selectedElement.Text = _inputText.InputField.text;

            if (_inputX != null && float.TryParse(_inputX.InputField.text, out float x)) _selectedElement.AnchorPosX = x;
            if (_inputY != null && float.TryParse(_inputY.InputField.text, out float y)) _selectedElement.AnchorPosY = y;
            if (_inputW != null && float.TryParse(_inputW.InputField.text, out float w)) _selectedElement.SizeX = w;
            if (_inputH != null && float.TryParse(_inputH.InputField.text, out float h)) _selectedElement.SizeY = h;
            if (_inputFontSize != null && float.TryParse(_inputFontSize.InputField.text, out float f)) _selectedElement.FontSize = f;

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (_previewMenuBg == null)
            {
                Debug.LogError("DesignerController: _previewMenuBg is null in RefreshPreview");
                return;
            }

            foreach (Transform child in _previewMenuBg.transform)
            {
                Destroy(child.gameObject);
            }

            var canvas = GetCanvas();

            foreach (var el in _elements)
            {
                GameObject obj = null;
                // ... (rest of the loop)
                switch (el.Type)
                {
                    case ElementType.Button:
                        var btn = Ui.AddButton(_previewMenuBg.transform, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, () => SelectElement(el));
                        obj = btn.gameObject;
                        break;
                    case ElementType.Label:
                        var lbl = Ui.AddLabel(_previewMenuBg.transform, el.Name, el.Text, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                        obj = lbl.Item1.gameObject;
                        break;
                    case ElementType.TextInput:
                        var inp = Ui.AddTextInput(_previewMenuBg.transform, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { }, 0.5f, 0.5f);
                        obj = inp.gameObject;
                        break;
                    case ElementType.Dropdown:
                        var dd = Ui.AddDropdown(_previewMenuBg.transform, new List<string> { "Option A" }, 0, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
                        obj = dd.gameObject;
                        break;
                    case ElementType.Checkbox:
                        var tgl = Ui.AddCheckbox(_previewMenuBg.transform, true, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, (v) => { });
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

            if (_previewMenuBg == null)
            {
                return;
            }

            var rt = _previewMenuBg.GetComponent<RectTransform>();

            var root = new JSONObject();
            root["PanelWidth"] = rt.rect.width;
            root["PanelHeight"] = rt.rect.height;
            root["PanelAnchorX"] = rt.anchorMin.x; // Assuming anchorMin == anchorMax for point anchor
            root["PanelAnchorY"] = rt.anchorMin.y;
            root["PanelPosX"] = rt.anchoredPosition.x;
            root["PanelPosY"] = rt.anchoredPosition.y;

            var arr = new JSONArray();
            foreach (var el in _elements)
            {
                arr.Add(el.ToJSON());
            }
            root["Elements"] = arr;

            File.WriteAllText(path, root.ToString(4));
            PersistentUI.Instance.DisplayMessage("Layout saved!", PersistentUI.DisplayMessageType.Bottom);
        }

        private void LoadLayout()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Load UI Layout", "", "json", false);
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

            string json = File.ReadAllText(paths[0]);
            var root = JSON.Parse(json);

            if (root != null)
            {
                _elements = new List<ElementData>();
                var arr = root["Elements"].AsArray;
                foreach (JSONNode n in arr)
                {
                    _elements.Add(ElementData.FromJSON(n));
                }

                // Load Panel Settings (Support both new and legacy keys if possible, or just new)
                float w = root["PanelWidth"] != null ? root["PanelWidth"].AsFloat : (root["MenuW"] != null ? root["MenuW"].AsFloat : 250);
                float h = root["PanelHeight"] != null ? root["PanelHeight"].AsFloat : (root["MenuH"] != null ? root["MenuH"].AsFloat : 190);
                
                float ax = root["PanelAnchorX"] != null ? root["PanelAnchorX"].AsFloat : 0.5f;
                float ay = root["PanelAnchorY"] != null ? root["PanelAnchorY"].AsFloat : 0.5f;
                float px = root["PanelPosX"] != null ? root["PanelPosX"].AsFloat : 0;
                float py = root["PanelPosY"] != null ? root["PanelPosY"].AsFloat : 0;

                // Update Preview BG
                // Ui.MoveTransform handles anchoring and position
                Ui.MoveTransform(_previewMenuBg.transform, w, h, ax, ay, px, py);

                // Update Input Fields (Check for nulls)
                if (_inputMenuW != null && _inputMenuW.InputField != null) _inputMenuW.InputField.SetTextWithoutNotify(w.ToString());
                if (_inputMenuH != null && _inputMenuH.InputField != null) _inputMenuH.InputField.SetTextWithoutNotify(h.ToString());
                if (_inputMenuX != null && _inputMenuX.InputField != null) _inputMenuX.InputField.SetTextWithoutNotify(px.ToString());
                if (_inputMenuY != null && _inputMenuY.InputField != null) _inputMenuY.InputField.SetTextWithoutNotify(py.ToString());
                if (_inputAnchorX != null && _inputAnchorX.InputField != null) _inputAnchorX.InputField.SetTextWithoutNotify(ax.ToString());
                if (_inputAnchorY != null && _inputAnchorY.InputField != null) _inputAnchorY.InputField.SetTextWithoutNotify(ay.ToString());

                UpdateMenuSize(); // Redundant since MoveTransform set it? But ensures logic consistency
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
                        sb.AppendLine($"    ui.AddButton(menu.transform, \"{el.Name}\", \"{el.Text}\", {f}, {w}, {h}, 0.5f, 0.5f, {x}, {y}, () => {{}}); // Note: Click handler is a placeholder");
                        break;
                    case ElementType.Label:
                        sb.AppendLine($"    ui.AddLabel(menu.transform, \"{el.Name}\", \"{el.Text}\", {w}, {h}, 0.5f, 0.5f, {x}, {y}, TextAlignmentOptions.Center, {f}");
                        break;
                    case ElementType.TextInput:
                        sb.AppendLine($"    ui.AddTextInput(menu.transform, \"{el.Name}\", \"{el.Text}\", TextAlignmentOptions.Left, {f}, {w}, {h}, 0.5f, 0.5f, {x}, {y}, (val) => {{}}); // Note: OnChange handler is a placeholder");
                        break;
                    case ElementType.Dropdown:
                        sb.AppendLine($"    ui.AddDropdown(menu.transform, new List<string>(), 0, {w}, {h}, 0.5f, 0.5f, {x}, {y}, (val) => {{}}); // Note: OnChange handler is a placeholder");
                        break;
                    case ElementType.Checkbox:
                        sb.AppendLine($"    ui.AddCheckbox(menu.transform, true, {w}, {h}, 0.5f, 0.5f, {x}, {y}, (val) => {{}}); // Note: OnValueChanged handler is a placeholder");
                        break;
                }
            }
            sb.AppendLine("}");
            
            File.WriteAllText(path, sb.ToString());
            PersistentUI.Instance.DisplayMessage("Code exported to file!", PersistentUI.DisplayMessageType.Bottom);
        }


    }
}
