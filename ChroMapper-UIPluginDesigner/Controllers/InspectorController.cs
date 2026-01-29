using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChroMapper_UIPluginDesigner.Constants;
using ChroMapper_UIPluginDesigner.UserResources;

namespace ChroMapper_UIPluginDesigner.Controllers
{
    public class InspectorController
    {
        private Action _onElementUpdated;
        private ElementData _selectedElement;

        // Normal Inputs
        private UITextInput _inputName;
        private UITextInput _inputText;
        private UITextInput _inputX, _inputY, _inputW, _inputH;
        private UITextInput _inputFontSize;

        // Layout Inspector Inputs
        private UITextInput _inputPadL, _inputPadR, _inputPadT, _inputPadB;
        private UITextInput _inputSpacing;
        private UIDropdown _inputChildAlignment;
        private Toggle _tglChCW, _tglChCH, _tglChFW, _tglChFH;

        // ScrollRect Inputs
        private UITextInput _inputScrollSens;
        private UIDropdown _inputScrollVis;

        // Slider Inputs
        private UITextInput _inputMinV, _inputMaxV;
        private Toggle _tglIsInt;

        // Image Inputs
        private UITextInput _inputHexColor;

        // Visibility Groups
        private List<GameObject> _normalInspectorObjects = new List<GameObject>();
        private List<GameObject> _layoutInspectorObjects = new List<GameObject>();
        private List<GameObject> _scrollInspectorObjects = new List<GameObject>();
        private List<GameObject> _sliderInspectorObjects = new List<GameObject>();
        private List<GameObject> _imageInspectorObjects = new List<GameObject>();

        public void Initialize(UILayoutBuilder builder, Action onElementUpdated)
        {
            _onElementUpdated = onElementUpdated;
            BindInputs(builder);
        }

        public void SelectElement(ElementData data, List<ElementData> allElements)
        {
            _selectedElement = data;
            if (data == null) return; // Hide all or clear? For now assumes called with valid data.

            // Common Props
            if (_inputName != null) _inputName.InputField.SetTextWithoutNotify(data.Name);
            if (_inputX != null) _inputX.InputField.SetTextWithoutNotify(data.AnchorPosX.ToString());
            if (_inputY != null) _inputY.InputField.SetTextWithoutNotify(data.AnchorPosY.ToString());
            if (_inputW != null) _inputW.InputField.SetTextWithoutNotify(data.SizeX.ToString());
            if (_inputH != null) _inputH.InputField.SetTextWithoutNotify(data.SizeY.ToString());

            // Check if parent controls size/pos
            var parent = FindParent(allElements, data);
            bool isChildOfLayout = parent != null && (parent.Type == ElementType.VerticalLayout || parent.Type == ElementType.HorizontalLayout || parent.Type == ElementType.ScrollRect);
            
            if (_inputX != null) _inputX.InputField.interactable = !isChildOfLayout;
            if (_inputY != null) _inputY.InputField.interactable = !isChildOfLayout;
            if (_inputW != null) _inputW.InputField.interactable = !(isChildOfLayout && parent.ChildControlWidth);
            if (_inputH != null) _inputH.InputField.interactable = !(isChildOfLayout && parent.ChildControlHeight);

            bool isLayout = (data.Type == ElementType.VerticalLayout || data.Type == ElementType.HorizontalLayout || data.Type == ElementType.ScrollRect);
            bool isScroll = (data.Type == ElementType.ScrollRect);
            bool isSlider = (data.Type == ElementType.Slider);
            bool isImage = (data.Type == ElementType.Image);

            ToggleInspectorVisibility(isLayout, isScroll, isSlider, isImage);

            if (isLayout)
            {
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
                if (_inputText != null) _inputText.InputField.SetTextWithoutNotify(data.Text);
                if (_inputFontSize != null) _inputFontSize.InputField.SetTextWithoutNotify(data.FontSize.ToString());
            }
        }

        public void ClearSelection()
        {
            _selectedElement = null;
            // Optionally clear text fields here if needed
        }

        private void BindInputs(UILayoutBuilder builder)
        {
            // Normal Inspector Inputs
            _inputName = BindInput(builder, DesignerConstants.PrefixName, false);
            _inputText = BindInput(builder, DesignerConstants.PrefixText, false, true); // true = normal prop
            _inputX = BindInput(builder, DesignerConstants.PrefixPosX, true);
            _inputY = BindInput(builder, DesignerConstants.PrefixPosY, true);
            _inputW = BindInput(builder, DesignerConstants.PrefixSizeW, true);
            _inputH = BindInput(builder, DesignerConstants.PrefixSizeH, true);
            _inputFontSize = BindInput(builder, DesignerConstants.PrefixFontSize, true, true); // true = normal prop

            RegisterNormalObject(builder, "Text_L");
            RegisterNormalObject(builder, "FontSize_L");

            // Layout Inspector Inputs
            _inputPadL = BindInput(builder, DesignerConstants.NamePaddingL, true, false, true);
            _inputPadR = BindInput(builder, DesignerConstants.NamePaddingR, true, false, true);
            _inputPadT = BindInput(builder, DesignerConstants.NamePaddingT, true, false, true);
            _inputPadB = BindInput(builder, DesignerConstants.NamePaddingB, true, false, true);
            _inputSpacing = BindInput(builder, DesignerConstants.NameSpacing, true, false, true);

            _inputChildAlignment = builder.Get<UIDropdown>(DesignerConstants.NameChildAlignment);
            if (_inputChildAlignment != null)
            {
                var options = new List<string>(Enum.GetNames(typeof(TextAnchor)));
                _inputChildAlignment.SetOptions(options);
                _inputChildAlignment.Dropdown.onValueChanged.AddListener((v) => UpdateSelectedElement());
                _layoutInspectorObjects.Add(_inputChildAlignment.gameObject);
            }

            _tglChCW = BindToggle(builder, DesignerConstants.NameChC_W, true);
            _tglChCH = BindToggle(builder, DesignerConstants.NameChC_H, true);
            _tglChFW = BindToggle(builder, DesignerConstants.NameChF_W, true);
            _tglChFH = BindToggle(builder, DesignerConstants.NameChF_H, true);
            
            RegisterLayoutObject(builder, DesignerConstants.NameLayoutTitle);
            RegisterLayoutObject(builder, "LabelPadL");
            RegisterLayoutObject(builder, "LabelPadR");
            RegisterLayoutObject(builder, "LabelPadT");
            RegisterLayoutObject(builder, "LabelPadB");
            RegisterLayoutObject(builder, "LabelSpacing");
            RegisterLayoutObject(builder, "LabelAlignment");
            RegisterLayoutObject(builder, "ChildAlignment");
            RegisterLayoutObject(builder, "LabelChControl");
            RegisterLayoutObject(builder, "LblChCW");
            RegisterLayoutObject(builder, "LblChCH");
            RegisterLayoutObject(builder, "LabelForceExp");
            RegisterLayoutObject(builder, "LblFExpW");
            RegisterLayoutObject(builder, "LblFExpH");

            // ScrollRect Inspector Inputs
            _inputScrollSens = BindInput(builder, DesignerConstants.NameScrollSensitivity, true, false, false, true);
            
            _inputScrollVis = builder.Get<UIDropdown>(DesignerConstants.NameScrollVisibility);
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
            _inputMinV = BindInput(builder, DesignerConstants.NameMinValue, true, false, false, false, true);
            _inputMaxV = BindInput(builder, DesignerConstants.NameMaxValue, true, false, false, false, true);
            _tglIsInt = BindToggle(builder, DesignerConstants.NameIsInteger, false, true);
            
            RegisterSliderObject(builder, "LabelMinV");
            RegisterSliderObject(builder, "LabelMaxV");
            RegisterSliderObject(builder, "LabelIsInt");

            // Image Inspector
            _inputHexColor = BindInput(builder, DesignerConstants.NameHexColor, false, false, false, false, false, true);
            RegisterImageObject(builder, "LabelHexColor");
        }

        private void UpdateSelectedElement()
        {
            if (_selectedElement == null) return;

            // Common Props
            if (_inputName != null) _selectedElement.Name = _inputName.InputField.text;
            
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
                if (_inputText != null) _selectedElement.Text = _inputText.InputField.text;
                if (_inputFontSize != null && float.TryParse(_inputFontSize.InputField.text, out float f)) _selectedElement.FontSize = f;
            }

            _onElementUpdated?.Invoke();
        }

        private void ToggleInspectorVisibility(bool showLayout, bool showScroll, bool showSlider, bool showImage)
        {
            foreach (var obj in _layoutInspectorObjects) obj.SetActive(showLayout);
            foreach (var obj in _scrollInspectorObjects) obj.SetActive(showScroll);
            foreach (var obj in _sliderInspectorObjects) obj.SetActive(showSlider);
            foreach (var obj in _imageInspectorObjects) obj.SetActive(showImage);
            foreach (var obj in _normalInspectorObjects) obj.SetActive(!showLayout && !showSlider && !showImage);
        }

        private UITextInput BindInput(UILayoutBuilder builder, string name, bool numeric, bool isNormal = false, bool isLayout = false, bool isScroll = false, bool isSlider = false, bool isImage = false)
        {
            string key = name.EndsWith("_I") ? name : name + DesignerConstants.SuffixInput;
            if (name == DesignerConstants.NameSpacing || name == DesignerConstants.NamePaddingL || name == DesignerConstants.NamePaddingR || name == DesignerConstants.NamePaddingT || name == DesignerConstants.NamePaddingB) key = name;
            
            // Special cases where key matches name exactly
            if (isScroll || isSlider || isImage) key = name;

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

                if (isNormal) _normalInspectorObjects.Add(input.gameObject);
                if (isLayout) _layoutInspectorObjects.Add(input.gameObject);
                if (isScroll) _scrollInspectorObjects.Add(input.gameObject);
                if (isSlider) _sliderInspectorObjects.Add(input.gameObject);
                if (isImage) _imageInspectorObjects.Add(input.gameObject);
            }
            return input;
        }

        private Toggle BindToggle(UILayoutBuilder builder, string name, bool isLayout = false, bool isSlider = false)
        {
            var tgl = builder.Get<Toggle>(name);
            if (tgl != null)
            {
                tgl.onValueChanged.AddListener((val) => UpdateSelectedElement());
                if (isLayout) _layoutInspectorObjects.Add(tgl.gameObject);
                if (isSlider) _sliderInspectorObjects.Add(tgl.gameObject);
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

        // Helper to find parent (Logic duplicated from DesignerController, could be moved to shared utility or ElementManager later)
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
    }
}
