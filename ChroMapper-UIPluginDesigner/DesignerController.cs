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
        
                                        // Layout Inspector Inputs
        
                                        private UITextInput _inputPadL, _inputPadR, _inputPadT, _inputPadB;
        
                                        private UITextInput _inputSpacing;
        
                                        private UIDropdown _inputChildAlignment;
        
                                        private Toggle _tglChCW, _tglChCH, _tglChFW, _tglChFH;
        
                                        
        
                                                        // ScrollRect Inputs
        
                                        
        
                                                        private UITextInput _inputScrollSens;
        
                                        
        
                                                        private UIDropdown _inputScrollVis;
        
                                        
        
                                                        private List<GameObject> _scrollInspectorObjects = new List<GameObject>();
        
                                        
        
                                        
        
                                        
        
                                                        // Slider Inputs
        
                                        
        
                                                        private UITextInput _inputMinV, _inputMaxV;
        
                                        
        
                                                        private Toggle _tglIsInt;
        
                                        
        
                                                        private List<GameObject> _sliderInspectorObjects = new List<GameObject>();
        
                                        
        
                                        
        
                                        
        
                                                        // Image Inputs
        
                                        
        
                                                        private UITextInput _inputHexColor;
        
                                        
        
                                                        private List<GameObject> _imageInspectorObjects = new List<GameObject>();
        
                                        
        
                                        
        
                                        
        
                                                        private List<GameObject> _layoutInspectorObjects = new List<GameObject>();
        
                                        
        
                                        
        
                                        private List<GameObject> _normalInspectorObjects = new List<GameObject>();        
        
                                
                private TextMeshProUGUI _pathLabel;
                private UIDropdown _hierarchyDropdown;
                private List<ElementData> _flatHierarchyList = new List<ElementData>();
                private bool _ignoreDropdownChange = false;
        
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
                
                // ... (OnDestroy, GetCanvas, CreateEditorPanel are same) ...
        
                private void BindEditorEvents(UILayoutBuilder builder)
                {
                    // Palette
                    if (builder.GetObject(UIConstants.NameAddButton) != null) builder.Get<Button>(UIConstants.NameAddButton).onClick.AddListener(() => AddElement(ElementType.Button));
                    if (builder.GetObject(UIConstants.NameAddLabel) != null) builder.Get<Button>(UIConstants.NameAddLabel).onClick.AddListener(() => AddElement(ElementType.Label));
                    if (builder.GetObject(UIConstants.NameAddInput) != null) builder.Get<Button>(UIConstants.NameAddInput).onClick.AddListener(() => AddElement(ElementType.TextInput));
                                        if (builder.GetObject(UIConstants.NameAddDropdown) != null) builder.Get<Button>(UIConstants.NameAddDropdown).onClick.AddListener(() => AddElement(ElementType.Dropdown));
                                        if (builder.GetObject(UIConstants.NameAddCheckbox) != null) builder.Get<Button>(UIConstants.NameAddCheckbox).onClick.AddListener(() => AddElement(ElementType.Checkbox));
                                        if (builder.GetObject(UIConstants.NameAddSlider) != null) builder.Get<Button>(UIConstants.NameAddSlider).onClick.AddListener(() => AddElement(ElementType.Slider));
                                        if (builder.GetObject(UIConstants.NameAddImage) != null) builder.Get<Button>(UIConstants.NameAddImage).onClick.AddListener(() => AddElement(ElementType.Image));
                                        if (builder.GetObject(UIConstants.NameAddRadioButton) != null) builder.Get<Button>(UIConstants.NameAddRadioButton).onClick.AddListener(() => AddElement(ElementType.RadioButton));
                                        if (builder.GetObject(UIConstants.NameAddVerticalLayout) != null) builder.Get<Button>(UIConstants.NameAddVerticalLayout).onClick.AddListener(() => AddElement(ElementType.VerticalLayout));
                    
                                if (builder.GetObject(UIConstants.NameAddHorizontalLayout) != null) builder.Get<Button>(UIConstants.NameAddHorizontalLayout).onClick.AddListener(() => AddElement(ElementType.HorizontalLayout));
                                if (builder.GetObject(UIConstants.NameAddScrollRect) != null) builder.Get<Button>(UIConstants.NameAddScrollRect).onClick.AddListener(() => AddElement(ElementType.ScrollRect));
                    
                                // Actions
                                if (builder.GetObject(UIConstants.NameSave) != null) builder.Get<Button>(UIConstants.NameSave).onClick.AddListener(SaveLayout);
                    
                    if (builder.GetObject(UIConstants.NameLoad) != null) builder.Get<Button>(UIConstants.NameLoad).onClick.AddListener(LoadLayout);
                    if (builder.GetObject(UIConstants.NameExport) != null) builder.Get<Button>(UIConstants.NameExport).onClick.AddListener(ExportCode);
                    if (builder.GetObject(UIConstants.NameClose) != null) builder.Get<Button>(UIConstants.NameClose).onClick.AddListener(() => Destroy(gameObject));
        
                    // Hierarchy & Path
                    if (builder.GetObject(UIConstants.NamePathLabel) != null)
                    {
                        var lblObj = builder.GetObject(UIConstants.NamePathLabel);
                        _pathLabel = lblObj.GetComponent<TextMeshProUGUI>();
                    }
        
                    if (builder.GetObject(UIConstants.NameHierarchyDropdown) != null)
                    {
                        _hierarchyDropdown = builder.Get<UIDropdown>(UIConstants.NameHierarchyDropdown);
                        _hierarchyDropdown.Dropdown.onValueChanged.AddListener((index) => {
                            if (_ignoreDropdownChange) return;
                            if (index >= 0 && index < _flatHierarchyList.Count)
                            {
                                SelectElement(_flatHierarchyList[index]);
                            }
                        });
                    }
        
                    // Menu Size & Pos Inputs (Same as before)
                    _inputMenuW = BindMenuInput(builder, UIConstants.NameMenuW, UpdateMenuSize);
                    _inputMenuH = BindMenuInput(builder, UIConstants.NameMenuH, UpdateMenuSize);
                    _inputMenuX = BindMenuInput(builder, UIConstants.NameMenuX, UpdateMenuPos);
                    _inputMenuY = BindMenuInput(builder, UIConstants.NameMenuY, UpdateMenuPos);
                    _inputAnchorX = BindMenuInput(builder, UIConstants.NameMenuAnchorX, UpdateMenuAnchor);
                    _inputAnchorY = BindMenuInput(builder, UIConstants.NameMenuAnchorY, UpdateMenuAnchor);
        
                    // Inspector Actions
                    if (builder.GetObject(UIConstants.NameDeleteElement) != null) builder.Get<Button>(UIConstants.NameDeleteElement).onClick.AddListener(DeleteSelectedElement);
                    if (builder.GetObject(UIConstants.NameCopyElement) != null) builder.Get<Button>(UIConstants.NameCopyElement).onClick.AddListener(CopySelectedElement);
        
                    // Normal Inspector Inputs
                    _inputName = BindInspectorInput(builder, UIConstants.PrefixName, false);
                    _inputText = BindInspectorInput(builder, UIConstants.PrefixText, false, true); // true = normal prop
                    _inputX = BindInspectorInput(builder, UIConstants.PrefixPosX, true);
                    _inputY = BindInspectorInput(builder, UIConstants.PrefixPosY, true);
                    _inputW = BindInspectorInput(builder, UIConstants.PrefixSizeW, true);
                    _inputH = BindInspectorInput(builder, UIConstants.PrefixSizeH, true);
                    _inputFontSize = BindInspectorInput(builder, UIConstants.PrefixFontSize, true, true); // true = normal prop
        
                    RegisterNormalObject(builder, "Text_L");
                    RegisterNormalObject(builder, "FontSize_L");
        
                    // Layout Inspector Inputs
                    _inputPadL = BindInspectorInput(builder, UIConstants.NamePaddingL, true, false, true);
                    _inputPadR = BindInspectorInput(builder, UIConstants.NamePaddingR, true, false, true);
                    _inputPadT = BindInspectorInput(builder, UIConstants.NamePaddingT, true, false, true);
                    _inputPadB = BindInspectorInput(builder, UIConstants.NamePaddingB, true, false, true);
                                _inputSpacing = BindInspectorInput(builder, UIConstants.NameSpacing, true, false, true);
                    
                                _inputChildAlignment = builder.Get<UIDropdown>(UIConstants.NameChildAlignment);
                                if (_inputChildAlignment != null)
                                {
                                    var options = new List<string>(Enum.GetNames(typeof(TextAnchor)));
                                    _inputChildAlignment.SetOptions(options);
                                    _inputChildAlignment.Dropdown.onValueChanged.AddListener((v) => UpdateSelectedElement());
                                    _layoutInspectorObjects.Add(_inputChildAlignment.gameObject);
                                }
                    
                                _tglChCW = BindInspectorToggle(builder, UIConstants.NameChC_W);
                                _tglChCH = BindInspectorToggle(builder, UIConstants.NameChC_H);
                                _tglChFW = BindInspectorToggle(builder, UIConstants.NameChF_W);
                                _tglChFH = BindInspectorToggle(builder, UIConstants.NameChF_H);
                                
                                // Register layout specific objects for visibility toggling
                                RegisterLayoutObject(builder, UIConstants.NameLayoutTitle);
                                RegisterLayoutObject(builder, "LabelPadL");
                                RegisterLayoutObject(builder, "LabelPadR");
                                RegisterLayoutObject(builder, "LabelPadT");
                                RegisterLayoutObject(builder, "LabelPadB");
                                RegisterLayoutObject(builder, "LabelSpacing");
                                RegisterLayoutObject(builder, "LabelAlignment");
                                RegisterLayoutObject(builder, "ChildAlignment");
                                RegisterLayoutObject(builder, "LabelChControl");                    RegisterLayoutObject(builder, "LblChCW");
                    RegisterLayoutObject(builder, "LblChCH");
                    RegisterLayoutObject(builder, "LabelForceExp");
                    RegisterLayoutObject(builder, "LblFExpW");
                    RegisterLayoutObject(builder, "LblFExpH");

                    // ScrollRect Inspector Inputs
                    _inputScrollSens = BindInspectorInput(builder, UIConstants.NameScrollSensitivity, true, false, false);
                    if (_inputScrollSens != null) _scrollInspectorObjects.Add(_inputScrollSens.gameObject);

                    _inputScrollVis = builder.Get<UIDropdown>(UIConstants.NameScrollVisibility);
                    if (_inputScrollVis != null)
                    {
                        var options = new List<string>(Enum.GetNames(typeof(ScrollRect.ScrollbarVisibility)));
                        _inputScrollVis.SetOptions(options);
                        _inputScrollVis.Dropdown.onValueChanged.AddListener((v) => UpdateSelectedElement());
                        _scrollInspectorObjects.Add(_inputScrollVis.gameObject);
                    }
                    
                    RegisterScrollObject(builder, "LabelScrollSens");
                    RegisterScrollObject(builder, "LabelScrollVis");

                    // Slider Inspector
                    _inputMinV = BindInspectorInput(builder, UIConstants.NameMinValue, true);
                    if (_inputMinV != null) _sliderInspectorObjects.Add(_inputMinV.gameObject);
                    _inputMaxV = BindInspectorInput(builder, UIConstants.NameMaxValue, true);
                    if (_inputMaxV != null) _sliderInspectorObjects.Add(_inputMaxV.gameObject);
                    _tglIsInt = BindInspectorToggle(builder, UIConstants.NameIsInteger);
                    if (_tglIsInt != null) _sliderInspectorObjects.Add(_tglIsInt.gameObject);
                    RegisterSliderObject(builder, "LabelMinV");
                    RegisterSliderObject(builder, "LabelMaxV");
                    RegisterSliderObject(builder, "LabelIsInt");

                    // Image Inspector
                    _inputHexColor = BindInspectorInput(builder, UIConstants.NameHexColor, false);
                    if (_inputHexColor != null) _imageInspectorObjects.Add(_inputHexColor.gameObject);
                    RegisterImageObject(builder, "LabelHexColor");
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
        
                private UITextInput BindInspectorInput(UILayoutBuilder builder, string name, bool numeric, bool isNormalProp = false, bool isLayoutProp = false)
                {
                    // For Prefix names, append SuffixInput. For exact names, use as is.
                    string key = name.EndsWith("_I") ? name : name + UIConstants.SuffixInput;
                    if (name == UIConstants.NameSpacing || name == UIConstants.NamePaddingL || name == UIConstants.NamePaddingR || name == UIConstants.NamePaddingT || name == UIConstants.NamePaddingB) key = name;
        
                    var input = builder.Get<UITextInput>(key);
                    if (input != null)
                    {
                        input.InputField.onValueChanged.AddListener((val) => UpdateSelectedElement());
                        input.InputField.onEndEdit.AddListener((val) => UpdateSelectedElement());
                        
                        if (numeric)
                        {
                            var adj = input.gameObject.AddComponent<InputNumberAdjuster>();
                            adj.InputField = input.InputField;
                        }
        
                        if (isNormalProp) _normalInspectorObjects.Add(input.gameObject);
                        if (isLayoutProp) _layoutInspectorObjects.Add(input.gameObject);
                    }
                    return input;
                }
        
                private Toggle BindInspectorToggle(UILayoutBuilder builder, string name)
                {
                    var tgl = builder.Get<Toggle>(name);
                    if (tgl != null)
                    {
                        tgl.onValueChanged.AddListener((val) => UpdateSelectedElement());
                        _layoutInspectorObjects.Add(tgl.gameObject);
                    }
                    return tgl;
                }
        
                private void RegisterNormalObject(UILayoutBuilder builder, string name)
                {
                    var obj = builder.GetObject(name);
                    if (obj != null) _normalInspectorObjects.Add(obj);
                }
        
                private void RegisterLayoutObject(UILayoutBuilder builder, string name)
                {
                    var obj = builder.GetObject(name);
                    if (obj != null) _layoutInspectorObjects.Add(obj);
                }

                private void RegisterScrollObject(UILayoutBuilder builder, string name)
                {
                    var obj = builder.GetObject(name);
                    if (obj != null) _scrollInspectorObjects.Add(obj);
                }

                private void RegisterSliderObject(UILayoutBuilder builder, string name)
                {
                    var obj = builder.GetObject(name);
                    if (obj != null) _sliderInspectorObjects.Add(obj);
                }

                private void RegisterImageObject(UILayoutBuilder builder, string name)
                {
                    var obj = builder.GetObject(name);
                    if (obj != null) _imageInspectorObjects.Add(obj);
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
            
            _inputName.InputField.text = "";
            _inputText.InputField.text = "";
            _inputX.InputField.text = "";
            _inputY.InputField.text = "";
            _inputW.InputField.text = "";
            _inputH.InputField.text = "";
            _inputFontSize.InputField.text = "";

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

            // Common Props
            if (_inputName != null && _inputName.InputField != null) _inputName.InputField.SetTextWithoutNotify(data.Name);
            if (_inputX != null && _inputX.InputField != null) _inputX.InputField.SetTextWithoutNotify(data.AnchorPosX.ToString());
            if (_inputY != null && _inputY.InputField != null) _inputY.InputField.SetTextWithoutNotify(data.AnchorPosY.ToString());
            if (_inputW != null && _inputW.InputField != null) _inputW.InputField.SetTextWithoutNotify(data.SizeX.ToString());
            if (_inputH != null && _inputH.InputField != null) _inputH.InputField.SetTextWithoutNotify(data.SizeY.ToString());

            // Check if parent is a Layout Group
            var parent = FindParent(_elements, data);
            bool isChildOfLayout = parent != null && (parent.Type == ElementType.VerticalLayout || parent.Type == ElementType.HorizontalLayout || parent.Type == ElementType.ScrollRect);
            
            if (_inputX != null && _inputX.InputField != null) _inputX.InputField.interactable = !isChildOfLayout;
            if (_inputY != null && _inputY.InputField != null) _inputY.InputField.interactable = !isChildOfLayout;

            if (_inputW != null && _inputW.InputField != null) 
                _inputW.InputField.interactable = !(isChildOfLayout && parent.ChildControlWidth);
            if (_inputH != null && _inputH.InputField != null) 
                _inputH.InputField.interactable = !(isChildOfLayout && parent.ChildControlHeight);

            bool isLayout = (data.Type == ElementType.VerticalLayout || data.Type == ElementType.HorizontalLayout || data.Type == ElementType.ScrollRect);
            bool isScroll = (data.Type == ElementType.ScrollRect);
            bool isSlider = (data.Type == ElementType.Slider);
            bool isImage = (data.Type == ElementType.Image);
            
            ToggleLayoutInspector(isLayout, isScroll, isSlider, isImage);

            if (isLayout)
            {
                // ... (Layout Group props) ...
                if (_inputPadL != null) _inputPadL.InputField.SetTextWithoutNotify(data.PaddingLeft.ToString());
                if (_inputPadR != null) _inputPadR.InputField.SetTextWithoutNotify(data.PaddingRight.ToString());
                if (_inputPadT != null) _inputPadT.InputField.SetTextWithoutNotify(data.PaddingTop.ToString());
                if (_inputPadB != null) _inputPadB.InputField.SetTextWithoutNotify(data.PaddingBottom.ToString());
                if (_inputSpacing != null) _inputSpacing.InputField.SetTextWithoutNotify(data.Spacing.ToString());
                
                if (_inputChildAlignment != null) _inputChildAlignment.Dropdown.SetValueWithoutNotify((int)data.Alignment);

                if (_tglChCW != null) _tglChCW.SetIsOnWithoutNotify(data.ChildControlWidth);
                if (_tglChCH != null) _tglChCH.SetIsOnWithoutNotify(data.ChildControlHeight);
                if (_tglChFW != null) _tglChFW.SetIsOnWithoutNotify(data.ChildForceExpandWidth);
                if (_tglChFH != null) _tglChFH.SetIsOnWithoutNotify(data.ChildForceExpandHeight);

                if (isScroll)
                {
                    if (_inputScrollSens != null) _inputScrollSens.InputField.SetTextWithoutNotify(data.ScrollSensitivity.ToString());
                    if (_inputScrollVis != null) _inputScrollVis.Dropdown.SetValueWithoutNotify((int)data.ScrollVisibility);
                }
            }
            
            if (isSlider)
            {
                if (_inputMinV != null) _inputMinV.InputField.SetTextWithoutNotify(data.MinValue.ToString());
                if (_inputMaxV != null) _inputMaxV.InputField.SetTextWithoutNotify(data.MaxValue.ToString());
                if (_tglIsInt != null) _tglIsInt.SetIsOnWithoutNotify(data.IsInteger);
            }

            if (isImage)
            {
                if (_inputHexColor != null) _inputHexColor.InputField.SetTextWithoutNotify(data.HexColor);
            }

            if (!isLayout && !isSlider && !isImage)
            {
                // Normal Props
                if (_inputText != null && _inputText.InputField != null) _inputText.InputField.SetTextWithoutNotify(data.Text);
                if (_inputFontSize != null && _inputFontSize.InputField != null) _inputFontSize.InputField.SetTextWithoutNotify(data.FontSize.ToString());
            }

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

        private void ToggleLayoutInspector(bool showLayout, bool showScroll, bool showSlider, bool showImage)
        {
            foreach (var obj in _layoutInspectorObjects)
            {
                obj.SetActive(showLayout);
            }
            foreach (var obj in _scrollInspectorObjects)
            {
                obj.SetActive(showScroll);
            }
            foreach (var obj in _sliderInspectorObjects)
            {
                obj.SetActive(showSlider);
            }
            foreach (var obj in _imageInspectorObjects)
            {
                obj.SetActive(showImage);
            }
            foreach (var obj in _normalInspectorObjects)
            {
                obj.SetActive(!showLayout && !showSlider && !showImage);
            }
        }

        private void UpdateSelectedElement()
        {
            if (_selectedElement == null) return;

            // Common Props
            if (_inputName != null && _inputName.InputField != null) _selectedElement.Name = _inputName.InputField.text;
            
            if (_inputX != null && float.TryParse(_inputX.InputField.text, out float x)) _selectedElement.AnchorPosX = x;
            if (_inputY != null && float.TryParse(_inputY.InputField.text, out float y)) _selectedElement.AnchorPosY = y;
            if (_inputW != null && float.TryParse(_inputW.InputField.text, out float w)) _selectedElement.SizeX = w;
            if (_inputH != null && float.TryParse(_inputH.InputField.text, out float h)) _selectedElement.SizeY = h;

            bool isLayout = (_selectedElement.Type == ElementType.VerticalLayout || _selectedElement.Type == ElementType.HorizontalLayout || _selectedElement.Type == ElementType.ScrollRect);
            bool isScroll = (_selectedElement.Type == ElementType.ScrollRect);
            bool isSlider = (_selectedElement.Type == ElementType.Slider);
            bool isImage = (_selectedElement.Type == ElementType.Image);

            if (isLayout)
            {
                if (_inputPadL != null && int.TryParse(_inputPadL.InputField.text, out int pl)) _selectedElement.PaddingLeft = pl;
                if (_inputPadR != null && int.TryParse(_inputPadR.InputField.text, out int pr)) _selectedElement.PaddingRight = pr;
                if (_inputPadT != null && int.TryParse(_inputPadT.InputField.text, out int pt)) _selectedElement.PaddingTop = pt;
                if (_inputPadB != null && int.TryParse(_inputPadB.InputField.text, out int pb)) _selectedElement.PaddingBottom = pb;
                if (_inputSpacing != null && float.TryParse(_inputSpacing.InputField.text, out float sp)) _selectedElement.Spacing = sp;

                if (_inputChildAlignment != null) _selectedElement.Alignment = (TextAnchor)_inputChildAlignment.Dropdown.value;

                if (_tglChCW != null) _selectedElement.ChildControlWidth = _tglChCW.isOn;
                if (_tglChCH != null) _selectedElement.ChildControlHeight = _tglChCH.isOn;
                if (_tglChFW != null) _selectedElement.ChildForceExpandWidth = _tglChFW.isOn;
                if (_tglChFH != null) _selectedElement.ChildForceExpandHeight = _tglChFH.isOn;

                if (isScroll)
                {
                    if (_inputScrollSens != null && float.TryParse(_inputScrollSens.InputField.text, out float sens)) _selectedElement.ScrollSensitivity = sens;
                    if (_inputScrollVis != null) _selectedElement.ScrollVisibility = (ScrollRect.ScrollbarVisibility)_inputScrollVis.Dropdown.value;
                }
            }
            
            if (isSlider)
            {
                if (_inputMinV != null && float.TryParse(_inputMinV.InputField.text, out float min)) _selectedElement.MinValue = min;
                if (_inputMaxV != null && float.TryParse(_inputMaxV.InputField.text, out float max)) _selectedElement.MaxValue = max;
                if (_tglIsInt != null) _selectedElement.IsInteger = _tglIsInt.isOn;
            }

            if (isImage)
            {
                if (_inputHexColor != null) _selectedElement.HexColor = _inputHexColor.InputField.text;
            }

            if (!isLayout && !isSlider && !isImage)
            {
                if (_inputText != null && _inputText.InputField != null) _selectedElement.Text = _inputText.InputField.text;
                if (_inputFontSize != null && float.TryParse(_inputFontSize.InputField.text, out float f)) _selectedElement.FontSize = f;
            }

            RefreshPreview();
            
            // Name change updates hierarchy and path
            UpdateHierarchyDropdown();
            UpdatePathDisplay(_selectedElement);
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
