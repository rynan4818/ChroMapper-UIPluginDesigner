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

namespace ChroMapper_UIPluginDesigner
{
    // --- Main Designer Logic (Mimicking MenuUI behavior logic but for editing) ---

    public class DesignerController : MonoBehaviour
    {
        // 参照用に Plugin.ui を使用
        private HelperUI Ui => Plugin.ui;

        private GameObject _editorPanel;
        private PreviewManager _previewManager;
        private LayoutFileManager _fileManager;

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
            
            _fileManager = new LayoutFileManager();
            _previewManager = new PreviewManager(Ui, GetCanvas());

            CreateEditorPanel();
            _previewManager.CreateContainer((pos) => {
                if (_inputMenuX != null && _inputMenuX.InputField != null) _inputMenuX.InputField.SetTextWithoutNotify(pos.x.ToString("F0"));
                if (_inputMenuY != null && _inputMenuY.InputField != null) _inputMenuY.InputField.SetTextWithoutNotify(pos.y.ToString("F0"));
            });
        }

        public void OnDestroy()
        {
            if (_editorPanel != null) Destroy(_editorPanel);
            _previewManager.Destroy();
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
            var resourceName = "ChroMapper_UIPluginDesigner.editor_layout.json";

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
                float pW = root[UIConstants.KeyPanelWidth] != null ? root[UIConstants.KeyPanelWidth].AsFloat : 140;
                float pH = root[UIConstants.KeyPanelHeight] != null ? root[UIConstants.KeyPanelHeight].AsFloat : 300;
                float pAX = root[UIConstants.KeyPanelAnchorX] != null ? root[UIConstants.KeyPanelAnchorX].AsFloat : 0;
                float pAY = root[UIConstants.KeyPanelAnchorY] != null ? root[UIConstants.KeyPanelAnchorY].AsFloat : 0.5f;
                float pX = root[UIConstants.KeyPanelPosX] != null ? root[UIConstants.KeyPanelPosX].AsFloat : 70;
                float pY = root[UIConstants.KeyPanelPosY] != null ? root[UIConstants.KeyPanelPosY].AsFloat : 0;
                
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
            if (builder.GetObject(UIConstants.NameAddButton) != null) builder.Get<Button>(UIConstants.NameAddButton).onClick.AddListener(() => AddElement(ElementType.Button));
            if (builder.GetObject(UIConstants.NameAddLabel) != null) builder.Get<Button>(UIConstants.NameAddLabel).onClick.AddListener(() => AddElement(ElementType.Label));
            if (builder.GetObject(UIConstants.NameAddInput) != null) builder.Get<Button>(UIConstants.NameAddInput).onClick.AddListener(() => AddElement(ElementType.TextInput));
            if (builder.GetObject(UIConstants.NameAddDropdown) != null) builder.Get<Button>(UIConstants.NameAddDropdown).onClick.AddListener(() => AddElement(ElementType.Dropdown));
            if (builder.GetObject(UIConstants.NameAddCheckbox) != null) builder.Get<Button>(UIConstants.NameAddCheckbox).onClick.AddListener(() => AddElement(ElementType.Checkbox));

            // Actions
            if (builder.GetObject(UIConstants.NameSave) != null) builder.Get<Button>(UIConstants.NameSave).onClick.AddListener(SaveLayout);
            if (builder.GetObject(UIConstants.NameLoad) != null) builder.Get<Button>(UIConstants.NameLoad).onClick.AddListener(LoadLayout);
            if (builder.GetObject(UIConstants.NameExport) != null) builder.Get<Button>(UIConstants.NameExport).onClick.AddListener(ExportCode);
            if (builder.GetObject(UIConstants.NameClose) != null) builder.Get<Button>(UIConstants.NameClose).onClick.AddListener(() => Destroy(gameObject));

            // Menu Size Inputs
            _inputMenuW = builder.Get<UITextInput>(UIConstants.NameMenuW);
            _inputMenuH = builder.Get<UITextInput>(UIConstants.NameMenuH);
            
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
            _inputMenuX = builder.Get<UITextInput>(UIConstants.NameMenuX);
            _inputMenuY = builder.Get<UITextInput>(UIConstants.NameMenuY);

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
            _inputAnchorX = builder.Get<UITextInput>(UIConstants.NameMenuAnchorX);
            _inputAnchorY = builder.Get<UITextInput>(UIConstants.NameMenuAnchorY);

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
            if (builder.GetObject(UIConstants.NameDeleteElement) != null) builder.Get<Button>(UIConstants.NameDeleteElement).onClick.AddListener(DeleteSelectedElement);
            if (builder.GetObject(UIConstants.NameCopyElement) != null) builder.Get<Button>(UIConstants.NameCopyElement).onClick.AddListener(CopySelectedElement);

            // Inspector Inputs
            _inputName = BindInspectorInput(builder, UIConstants.PrefixName, false);
            _inputText = BindInspectorInput(builder, UIConstants.PrefixText, false);
            _inputX = BindInspectorInput(builder, UIConstants.PrefixPosX, true);
            _inputY = BindInspectorInput(builder, UIConstants.PrefixPosY, true);
            _inputW = BindInspectorInput(builder, UIConstants.PrefixSizeW, true);
            _inputH = BindInspectorInput(builder, UIConstants.PrefixSizeH, true);
            _inputFontSize = BindInspectorInput(builder, UIConstants.PrefixFontSize, true);
        }

        private UITextInput BindInspectorInput(UILayoutBuilder builder, string prefix, bool numeric)
        {
            var input = builder.Get<UITextInput>(prefix + UIConstants.SuffixInput);
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
            if (_inputMenuW != null && _inputMenuW.InputField != null && 
                float.TryParse(_inputMenuW.InputField.text, out float w) && 
                _inputMenuH != null && _inputMenuH.InputField != null && 
                float.TryParse(_inputMenuH.InputField.text, out float h))
            {
                _previewManager.UpdateSize(w, h);
            }
        }

        private void UpdateMenuPos()
        {
            if (_inputMenuX != null && _inputMenuX.InputField != null && 
                float.TryParse(_inputMenuX.InputField.text, out float x) && 
                _inputMenuY != null && _inputMenuY.InputField != null && 
                float.TryParse(_inputMenuY.InputField.text, out float y))
            {
                _previewManager.UpdatePosition(x, y);
            }
        }

        private void UpdateMenuAnchor()
        {
            if (_inputAnchorX != null && _inputAnchorX.InputField != null && 
                float.TryParse(_inputAnchorX.InputField.text, out float ax) && 
                _inputAnchorY != null && _inputAnchorY.InputField != null && 
                float.TryParse(_inputAnchorY.InputField.text, out float ay))
            {
                _previewManager.UpdateAnchor(ax, ay);
            }
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
            _previewManager.Refresh(_elements, _selectedElement, SelectElement, SelectElement);
        }

        private void SaveLayout()
        {
            if (_previewManager.PreviewMenuBg == null) return;
            var rt = _previewManager.PreviewMenuBg.GetComponent<RectTransform>();
            _fileManager.SaveLayout(rt, _elements);
        }

        private void LoadLayout()
        {
            var root = _fileManager.LoadLayout(out string path);
            if (root != null)
            {
                _elements = new List<ElementData>();
                var arr = root[UIConstants.KeyElements].AsArray;
                foreach (JSONNode n in arr)
                {
                    _elements.Add(ElementData.FromJSON(n));
                }

                // Load Panel Settings
                float w = root[UIConstants.KeyPanelWidth] != null ? root[UIConstants.KeyPanelWidth].AsFloat : (root["MenuW"] != null ? root["MenuW"].AsFloat : 250);
                float h = root[UIConstants.KeyPanelHeight] != null ? root[UIConstants.KeyPanelHeight].AsFloat : (root["MenuH"] != null ? root["MenuH"].AsFloat : 190);
                
                float ax = root[UIConstants.KeyPanelAnchorX] != null ? root[UIConstants.KeyPanelAnchorX].AsFloat : 0.5f;
                float ay = root[UIConstants.KeyPanelAnchorY] != null ? root[UIConstants.KeyPanelAnchorY].AsFloat : 0.5f;
                float px = root[UIConstants.KeyPanelPosX] != null ? root[UIConstants.KeyPanelPosX].AsFloat : 0;
                float py = root[UIConstants.KeyPanelPosY] != null ? root[UIConstants.KeyPanelPosY].AsFloat : 0;

                _previewManager.ApplyLayoutSettings(w, h, ax, ay, px, py);

                // Update Input Fields (Check for nulls)
                if (_inputMenuW != null && _inputMenuW.InputField != null) _inputMenuW.InputField.SetTextWithoutNotify(w.ToString());
                if (_inputMenuH != null && _inputMenuH.InputField != null) _inputMenuH.InputField.SetTextWithoutNotify(h.ToString());
                if (_inputMenuX != null && _inputMenuX.InputField != null) _inputMenuX.InputField.SetTextWithoutNotify(px.ToString());
                if (_inputMenuY != null && _inputMenuY.InputField != null) _inputMenuY.InputField.SetTextWithoutNotify(py.ToString());
                if (_inputAnchorX != null && _inputAnchorX.InputField != null) _inputAnchorX.InputField.SetTextWithoutNotify(ax.ToString());
                if (_inputAnchorY != null && _inputAnchorY.InputField != null) _inputAnchorY.InputField.SetTextWithoutNotify(ay.ToString());

                UpdateMenuSize();
                RefreshPreview();
                PersistentUI.Instance.DisplayMessage("Layout loaded!", PersistentUI.DisplayMessageType.Bottom);
            }
        }

        private void ExportCode()
        {
            _fileManager.ExportCode(_elements);
        }


    }
}
