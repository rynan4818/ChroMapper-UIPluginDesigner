using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using System.IO;
using System;
using System.Reflection;
using SimpleJSON;
using ChroMapper_UIPluginDesigner.Components;
using ChroMapper_UIPluginDesigner.UserResources;
using ChroMapper_UIPluginDesigner.Constants;

namespace ChroMapper_UIPluginDesigner.Controllers
{
    // --- Main Designer Logic (Mimicking MenuUI behavior logic but for editing) ---

    public class DesignerController : MonoBehaviour
    {
        // 参照用に Plugin.ui を使用
        private HelperUI Ui => Plugin.ui;

        private GameObject _editorPanel;
        private PreviewManager _previewManager;
        private LayoutFileManager _fileManager;
        private InspectorController _inspector;

        private List<ElementData> _elements = new List<ElementData>();
        private ElementData _selectedElement;

        // Menu Settings Inputs
        private UITextInput _inputMenuW, _inputMenuH, _inputMenuX, _inputMenuY;
        private UITextInput _inputAnchorX, _inputAnchorY;

        private TextMeshProUGUI _pathLabel;

                private UIDropdown _hierarchyDropdown;
                private List<ElementData> _flatHierarchyList = new List<ElementData>();
                private bool _ignoreDropdownChange = false;
        
                        public void Start()
        
                        {
        
                            if (Ui == null) Debug.LogError("DesignerController: Plugin.ui is null in Start!");
        
                            
        
                            _fileManager = new LayoutFileManager();
        
                            _previewManager = new PreviewManager(Ui, GetCanvas());
        
                            _inspector = new InspectorController();
        
                
        
                            CreateEditorPanel();
        
                            _previewManager.CreateContainer((pos) => {
        
                
                        if (_inputMenuX != null && _inputMenuX.InputField != null) _inputMenuX.InputField.SetTextWithoutNotify(pos.x.ToString("F0"));
                        if (_inputMenuY != null && _inputMenuY.InputField != null) _inputMenuY.InputField.SetTextWithoutNotify(pos.y.ToString("F0"));
                    });
                }
                
        public void OnDestroy()
        {
            if (_previewManager != null) _previewManager.Destroy();
        }

        private Canvas GetCanvas()
        {
            return GameObject.Find("Canvas")?.GetComponent<Canvas>();
        }

        private void CreateEditorPanel()
        {
            var canvas = GetCanvas();
            if (canvas == null) return;

            // 埋め込みリソースからエディタ自体のレイアウトJSONを読み込む
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ChroMapper_UIPluginDesigner.Resources.editor_layout.json";
            string jsonString = "";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) { Debug.LogError("Resource not found: " + resourceName); return; }
                using (StreamReader reader = new StreamReader(stream))
                {
                    jsonString = reader.ReadToEnd();
                }
            }

            var root = JSON.Parse(jsonString);

            // エディタパネルのベース
            _editorPanel = new GameObject("UIPluginDesigner_Editor", typeof(RectTransform));
            _editorPanel.transform.SetParent(canvas.transform, false);
            Ui.AttachImage(_editorPanel, new Color(0.12f, 0.12f, 0.12f, 0.98f));

            float w = root[UILayoutMap.KeyPanelWidth].AsFloat;
            float h = root[UILayoutMap.KeyPanelHeight].AsFloat;
            float ax = root[UILayoutMap.KeyPanelAnchorX].AsFloat;
            float ay = root[UILayoutMap.KeyPanelAnchorY].AsFloat;
            float px = root[UILayoutMap.KeyPanelPosX].AsFloat;
            float py = root[UILayoutMap.KeyPanelPosY].AsFloat;

            Ui.MoveTransform(_editorPanel.transform, w, h, ax, ay, px, py);
            _editorPanel.AddComponent<SimpleDrag>(); // パネル自体をドラッグ可能にする

            // UILayoutBuilderを使ってエディタUIを構築
            var builder = new UILayoutBuilder(Ui, _editorPanel.transform);
            builder.Build(root);

            BindEditorEvents(builder);
        }
        
        private void BindEditorEvents(UILayoutBuilder builder)
        {
            // Palette
            if (builder.GetObject(DesignerConstants.NameAddButton) != null) builder.Get<Button>(DesignerConstants.NameAddButton).onClick.AddListener(() => AddElement(ElementType.Button));
            if (builder.GetObject(DesignerConstants.NameAddLabel) != null) builder.Get<Button>(DesignerConstants.NameAddLabel).onClick.AddListener(() => AddElement(ElementType.Label));
            if (builder.GetObject(DesignerConstants.NameAddInput) != null) builder.Get<Button>(DesignerConstants.NameAddInput).onClick.AddListener(() => AddElement(ElementType.TextInput));
            if (builder.GetObject(DesignerConstants.NameAddDropdown) != null) builder.Get<Button>(DesignerConstants.NameAddDropdown).onClick.AddListener(() => AddElement(ElementType.Dropdown));
            if (builder.GetObject(DesignerConstants.NameAddCheckbox) != null) builder.Get<Button>(DesignerConstants.NameAddCheckbox).onClick.AddListener(() => AddElement(ElementType.Checkbox));
            if (builder.GetObject(DesignerConstants.NameAddSlider) != null) builder.Get<Button>(DesignerConstants.NameAddSlider).onClick.AddListener(() => AddElement(ElementType.Slider));
            if (builder.GetObject(DesignerConstants.NameAddImage) != null) builder.Get<Button>(DesignerConstants.NameAddImage).onClick.AddListener(() => AddElement(ElementType.Image));
            if (builder.GetObject(DesignerConstants.NameAddRadioButton) != null) builder.Get<Button>(DesignerConstants.NameAddRadioButton).onClick.AddListener(() => AddElement(ElementType.RadioButton));
            if (builder.GetObject(DesignerConstants.NameAddVerticalLayout) != null) builder.Get<Button>(DesignerConstants.NameAddVerticalLayout).onClick.AddListener(() => AddElement(ElementType.VerticalLayout));
            if (builder.GetObject(DesignerConstants.NameAddHorizontalLayout) != null) builder.Get<Button>(DesignerConstants.NameAddHorizontalLayout).onClick.AddListener(() => AddElement(ElementType.HorizontalLayout));
            if (builder.GetObject(DesignerConstants.NameAddScrollRect) != null) builder.Get<Button>(DesignerConstants.NameAddScrollRect).onClick.AddListener(() => AddElement(ElementType.ScrollRect));

            // Actions
            if (builder.GetObject(DesignerConstants.NameSave) != null) builder.Get<Button>(DesignerConstants.NameSave).onClick.AddListener(SaveLayout);
            if (builder.GetObject(DesignerConstants.NameLoad) != null) builder.Get<Button>(DesignerConstants.NameLoad).onClick.AddListener(LoadLayout);
            if (builder.GetObject(DesignerConstants.NameExport) != null) builder.Get<Button>(DesignerConstants.NameExport).onClick.AddListener(ExportCode);
            if (builder.GetObject(DesignerConstants.NameClose) != null) builder.Get<Button>(DesignerConstants.NameClose).onClick.AddListener(() => Destroy(gameObject));

            // Hierarchy & Path
            if (builder.GetObject(DesignerConstants.NamePathLabel) != null)
            {
                var lblObj = builder.GetObject(DesignerConstants.NamePathLabel);
                _pathLabel = lblObj.GetComponent<TextMeshProUGUI>();
            }

            if (builder.GetObject(DesignerConstants.NameHierarchyDropdown) != null)
            {
                _hierarchyDropdown = builder.Get<UIDropdown>(DesignerConstants.NameHierarchyDropdown);
                _hierarchyDropdown.Dropdown.onValueChanged.AddListener((index) => {
                    if (_ignoreDropdownChange) return;
                    if (index >= 0 && index < _flatHierarchyList.Count)
                    {
                        SelectElement(_flatHierarchyList[index]);
                    }
                });
            }

            // Menu Size & Pos Inputs (Same as before)
            _inputMenuW = BindMenuInput(builder, DesignerConstants.NameMenuW, UpdateMenuSize);
            _inputMenuH = BindMenuInput(builder, DesignerConstants.NameMenuH, UpdateMenuSize);
            _inputMenuX = BindMenuInput(builder, DesignerConstants.NameMenuX, UpdateMenuPos);
            _inputMenuY = BindMenuInput(builder, DesignerConstants.NameMenuY, UpdateMenuPos);
            _inputAnchorX = BindMenuInput(builder, DesignerConstants.NameMenuAnchorX, UpdateMenuAnchor);
            _inputAnchorY = BindMenuInput(builder, DesignerConstants.NameMenuAnchorY, UpdateMenuAnchor);

            // Inspector Actions
            if (builder.GetObject(DesignerConstants.NameDeleteElement) != null) builder.Get<Button>(DesignerConstants.NameDeleteElement).onClick.AddListener(DeleteSelectedElement);
            if (builder.GetObject(DesignerConstants.NameCopyElement) != null) builder.Get<Button>(DesignerConstants.NameCopyElement).onClick.AddListener(CopySelectedElement);

            // Initialize Inspector
            _inspector.Initialize(builder, OnElementUpdated);
        }
                    
                            private void OnElementUpdated()
                            {
                                RefreshPreview();
                                // Name change updates hierarchy and path
                                UpdateHierarchyDropdown();
                                UpdatePathDisplay(_selectedElement);
                            }
                                    private UITextInput BindMenuInput(UILayoutBuilder builder, string name, UnityEngine.Events.UnityAction action)
                {
                    var input = builder.Get<UITextInput>(name);
                    if (input != null) {
                        input.InputField.onEndEdit.AddListener((v) => action());
                        input.InputField.onValueChanged.AddListener((v) => action());
                        var adj = input.gameObject.AddComponent<InputNumberAdjuster>();
                        adj.InputField = input.InputField;
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





        private void UpdatePathDisplay(ElementData target)
        {
            if (_pathLabel == null) return;
            if (target == null)
            {
                _pathLabel.text = "Root";
                return;
            }

            var path = new List<string>();
            var current = target;
            path.Add(current.Name);

            // Find parents recursively from root
            while (true)
            {
                var parent = FindParent(_elements, current);
                if (parent != null)
                {
                    path.Add(parent.Name);
                    current = parent;
                }
                else
                {
                    path.Add("Root");
                    break;
                }
            }

            path.Reverse();
            _pathLabel.text = string.Join(" > ", path);
        }

        private void UpdateHierarchyDropdown()
        {
            if (_hierarchyDropdown == null) return;

            _flatHierarchyList.Clear();
            var options = new List<string>();

            // Recursive function to flatten hierarchy
            void ProcessList(List<ElementData> list, int depth)
            {
                foreach (var el in list)
                {
                    _flatHierarchyList.Add(el);
                    options.Add(new string(' ', depth * 2) + el.Name); // Indent
                    if (el.Children.Count > 0)
                    {
                        ProcessList(el.Children, depth + 1);
                    }
                }
            }

            ProcessList(_elements, 0);

            _ignoreDropdownChange = true;
            _hierarchyDropdown.SetOptions(options);
            _ignoreDropdownChange = false;

            // Sync selection if possible
            if (_selectedElement != null)
            {
                int index = _flatHierarchyList.IndexOf(_selectedElement);
                if (index != -1)
                {
                    _hierarchyDropdown.Dropdown.SetValueWithoutNotify(index);
                }
            }
        }

        private void CopySelectedElement()
        {
            if (_selectedElement == null) return;
            var newEl = new ElementData
            {
                Type = _selectedElement.Type,
                Name = _selectedElement.Type.ToString() + UnityEngine.Random.Range(0, 1000),
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
            
            // Try remove from root
            if (_elements.Contains(_selectedElement))
            {
                _elements.Remove(_selectedElement);
            }
            else
            {
                // Try remove from children
                var parent = FindParent(_elements, _selectedElement);
                if (parent != null)
                {
                    parent.Children.Remove(_selectedElement);
                }
            }

            _selectedElement = null;
            _inspector.ClearSelection();
            RefreshPreview();
        }

        private ElementData FindParent(List<ElementData> nodes, ElementData target)
        {
            foreach (var node in nodes)
            {
                if (node.Children.Contains(target)) return node;
                var res = FindParent(node.Children, target);
                if (res != null) return res;
            }
            return null;
        }

        private void ReparentElement(ElementData child, ElementData newParent)
        {
            if (child == newParent) return; // Cannot parent to self
            
            // Check for circular dependency
            if (IsDescendant(child, newParent)) return;

            // Remove from old parent
            if (_elements.Contains(child))
            {
                _elements.Remove(child);
            }
            else
            {
                var oldParent = FindParent(_elements, child);
                if (oldParent != null)
                {
                    oldParent.Children.Remove(child);
                }
            }

            // Add to new parent
            if (newParent == null)
            {
                _elements.Add(child);
            }
            else
            {
                newParent.Children.Add(child);
            }

            RefreshPreview();
            
            // Re-select to update inspector input states (enabled/disabled) based on new parent
            if (_selectedElement == child)
            {
                SelectElement(child);
            }
        }

        private bool IsDescendant(ElementData node, ElementData potentialDescendant)
        {
            if (potentialDescendant == null) return false;
            foreach (var child in node.Children)
            {
                if (child == potentialDescendant) return true;
                if (IsDescendant(child, potentialDescendant)) return true;
            }
            return false;
        }

        private void AddElement(ElementType type)
        {
            if (_elements == null) _elements = new List<ElementData>();

            var data = new ElementData
            {
                Type = type,
                Name = type.ToString() + UnityEngine.Random.Range(0, 1000), // Unique name
                Text = type.ToString(),
                SizeX = 160,
                SizeY = 40,
                AnchorPosX = 0,
                AnchorPosY = 0,
                FontSize = 14
            };

            if (type == ElementType.Label) { data.SizeX = 200; data.SizeY = 30; }
            if (type == ElementType.Button) { data.SizeX = 100; data.SizeY = 30; }
            if (type == ElementType.Slider) { data.SizeX = 160; data.SizeY = 20; data.Text = ""; }
            if (type == ElementType.Image) { data.SizeX = 100; data.SizeY = 100; data.Text = ""; data.HexColor = "#FFFFFF"; }
            if (type == ElementType.RadioButton) { data.SizeX = 100; data.SizeY = 25; data.Text = "Radio"; }
            if (type == ElementType.VerticalLayout || type == ElementType.HorizontalLayout || type == ElementType.ScrollRect)
            {
                data.SizeX = 200;
                data.SizeY = 200;
                data.Text = "";
            }
            if (type == ElementType.ScrollRect)
            {
                data.ChildControlWidth = true; 
                data.ChildForceExpandWidth = true;
            }

            _elements.Add(data);
            SelectElement(data);
            RefreshPreview();
        }

                private void SelectElement(ElementData data)
                {
                    _selectedElement = data;
                    _inspector.SelectElement(data, _elements);
                    UpdatePathDisplay(data);
                    
                    // Sync dropdown
                    if (_hierarchyDropdown != null && !_ignoreDropdownChange)
                    {
                        _ignoreDropdownChange = true;
                        int index = _flatHierarchyList.IndexOf(data);
                        if (index != -1)
                        {
                            _hierarchyDropdown.Dropdown.SetValueWithoutNotify(index);
                        }
                        _ignoreDropdownChange = false;
                    }
                }
                        private void DeleteSelectedElement()
                        {
                            if (_selectedElement == null) return;
                            
                            // Try remove from root
                            if (_elements.Contains(_selectedElement))
                            {
                                _elements.Remove(_selectedElement);
                            }
                            else
                            {
                                // Try remove from children
                                var parent = FindParent(_elements, _selectedElement);
                                if (parent != null)
                                {
                                    parent.Children.Remove(_selectedElement);
                                }
                            }
                
            _selectedElement = null;
            _inspector.ClearSelection();
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            UpdateHierarchyDropdown();
            _previewManager.Refresh(_elements, _selectedElement, SelectElement, SelectElement, ReparentElement);
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
                var arr = root[UILayoutMap.KeyElements].AsArray;
                foreach (JSONNode n in arr)
                {
                    _elements.Add(ElementData.FromJSON(n));
                }

                // Load Panel Settings
                float w = root[UILayoutMap.KeyPanelWidth] != null ? root[UILayoutMap.KeyPanelWidth].AsFloat : (root["MenuW"] != null ? root["MenuW"].AsFloat : 250);
                float h = root[UILayoutMap.KeyPanelHeight] != null ? root[UILayoutMap.KeyPanelHeight].AsFloat : (root["MenuH"] != null ? root["MenuH"].AsFloat : 190);
                
                float ax = root[UILayoutMap.KeyPanelAnchorX] != null ? root[UILayoutMap.KeyPanelAnchorX].AsFloat : 0.5f;
                float ay = root[UILayoutMap.KeyPanelAnchorY] != null ? root[UILayoutMap.KeyPanelAnchorY].AsFloat : 0.5f;
                float px = root[UILayoutMap.KeyPanelPosX] != null ? root[UILayoutMap.KeyPanelPosX].AsFloat : 0;
                float py = root[UILayoutMap.KeyPanelPosY] != null ? root[UILayoutMap.KeyPanelPosY].AsFloat : 0;

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
