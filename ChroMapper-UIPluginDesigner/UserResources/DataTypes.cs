using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace ChroMapper_UIPluginDesigner.UserResources
{
    public enum ElementType { Button, Label, TextInput, Dropdown, Checkbox, VerticalLayout, HorizontalLayout, ScrollRect, Slider, Image, RadioButton }

    [Serializable]
    public class ElementData
    {
        public ElementType Type;
        public string Name;
        public string Text;
        public float AnchorPosX, AnchorPosY;
        public float SizeX, SizeY;
        public float FontSize;

        // Layout Group Properties
        public int PaddingTop, PaddingBottom, PaddingLeft, PaddingRight;
        public float Spacing;
        public TextAnchor Alignment;
        public bool ChildControlWidth, ChildControlHeight;
        public bool ChildForceExpandWidth, ChildForceExpandHeight;

        // ScrollRect Properties
        public float ScrollSensitivity = 20f;
        public ScrollRect.ScrollbarVisibility ScrollVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Slider Properties
        public float MinValue = 0f;
        public float MaxValue = 1f;
        public bool IsInteger = false;

        // Image Properties
        public string HexColor = "#FFFFFF";

        public List<ElementData> Children = new List<ElementData>();

        public static ElementData FromJSON(JSONNode n)
        {
            var el = new ElementData();
            if (Enum.TryParse(n[UILayoutMap.KeyType].Value, out ElementType t))
                el.Type = t;
            else
                el.Type = ElementType.Button;

            el.Name = n[UILayoutMap.KeyName].Value;
            el.Text = n[UILayoutMap.KeyText].Value;
            el.AnchorPosX = n[UILayoutMap.KeyAnchorPosX].AsFloat;
            el.AnchorPosY = n[UILayoutMap.KeyAnchorPosY].AsFloat;
            el.SizeX = n[UILayoutMap.KeySizeX].AsFloat;
            el.SizeY = n[UILayoutMap.KeySizeY].AsFloat;
            el.FontSize = n[UILayoutMap.KeyFontSize].AsFloat;

            if (el.Type == ElementType.VerticalLayout || el.Type == ElementType.HorizontalLayout || el.Type == ElementType.ScrollRect)
            {
                el.PaddingTop = n[UILayoutMap.KeyPaddingTop].AsInt;
                el.PaddingBottom = n[UILayoutMap.KeyPaddingBottom].AsInt;
                el.PaddingLeft = n[UILayoutMap.KeyPaddingLeft].AsInt;
                el.PaddingRight = n[UILayoutMap.KeyPaddingRight].AsInt;
                el.Spacing = n[UILayoutMap.KeySpacing].AsFloat;
                
                if (Enum.TryParse(n[UILayoutMap.KeyChildAlignment].Value, out TextAnchor align)) 
                    el.Alignment = align; 
                else 
                    el.Alignment = TextAnchor.UpperLeft;

                el.ChildControlWidth = n[UILayoutMap.KeyChildControlWidth].AsBool;
                el.ChildControlHeight = n[UILayoutMap.KeyChildControlHeight].AsBool;
                el.ChildForceExpandWidth = n[UILayoutMap.KeyChildForceExpandWidth].AsBool;
                el.ChildForceExpandHeight = n[UILayoutMap.KeyChildForceExpandHeight].AsBool;

                if (el.Type == ElementType.ScrollRect)
                {
                    if (n[UILayoutMap.KeyScrollSensitivity] != null)
                        el.ScrollSensitivity = n[UILayoutMap.KeyScrollSensitivity].AsFloat;

                    if (Enum.TryParse(n[UILayoutMap.KeyScrollVisibility].Value, out ScrollRect.ScrollbarVisibility vis))
                        el.ScrollVisibility = vis;
                }

                if (el.Type == ElementType.Slider)
                {
                    el.MinValue = n[UILayoutMap.KeyMinValue].AsFloat;
                    el.MaxValue = n[UILayoutMap.KeyMaxValue].AsFloat;
                    el.IsInteger = n[UILayoutMap.KeyIsInteger].AsBool;
                }

                if (el.Type == ElementType.Image)
                {
                    el.HexColor = n[UILayoutMap.KeyHexColor].Value;
                }

                if (n[UILayoutMap.KeyChildren] != null)
                {
                    foreach (JSONNode childNode in n[UILayoutMap.KeyChildren].AsArray)
                    {
                        el.Children.Add(FromJSON(childNode));
                    }
                }
            }

            return el;
        }

        public JSONObject ToJSON()
        {
            var n = new JSONObject();
            n[UILayoutMap.KeyType] = Type.ToString();
            n[UILayoutMap.KeyName] = Name;
            n[UILayoutMap.KeyText] = Text;
            n[UILayoutMap.KeyAnchorPosX] = AnchorPosX;
            n[UILayoutMap.KeyAnchorPosY] = AnchorPosY;
            n[UILayoutMap.KeySizeX] = SizeX;
            n[UILayoutMap.KeySizeY] = SizeY;
            n[UILayoutMap.KeyFontSize] = FontSize;

            if (Type == ElementType.VerticalLayout || Type == ElementType.HorizontalLayout || Type == ElementType.ScrollRect)
            {
                n[UILayoutMap.KeyPaddingTop] = PaddingTop;
                n[UILayoutMap.KeyPaddingBottom] = PaddingBottom;
                n[UILayoutMap.KeyPaddingLeft] = PaddingLeft;
                n[UILayoutMap.KeyPaddingRight] = PaddingRight;
                n[UILayoutMap.KeySpacing] = Spacing;
                n[UILayoutMap.KeyChildAlignment] = Alignment.ToString();
                n[UILayoutMap.KeyChildControlWidth] = ChildControlWidth;
                n[UILayoutMap.KeyChildControlHeight] = ChildControlHeight;
                n[UILayoutMap.KeyChildForceExpandWidth] = ChildForceExpandWidth;
                n[UILayoutMap.KeyChildForceExpandHeight] = ChildForceExpandHeight;

                if (Type == ElementType.ScrollRect)
                {
                    n[UILayoutMap.KeyScrollSensitivity] = ScrollSensitivity;
                    n[UILayoutMap.KeyScrollVisibility] = ScrollVisibility.ToString();
                }

                if (Type == ElementType.Slider)
                {
                    n[UILayoutMap.KeyMinValue] = MinValue;
                    n[UILayoutMap.KeyMaxValue] = MaxValue;
                    n[UILayoutMap.KeyIsInteger] = IsInteger;
                }

                if (Type == ElementType.Image)
                {
                    n[UILayoutMap.KeyHexColor] = HexColor;
                }

                var childrenArr = new JSONArray();
                foreach (var child in Children)
                {
                    childrenArr.Add(child.ToJSON());
                }
                n[UILayoutMap.KeyChildren] = childrenArr;
            }

            return n;
        }
    }
}

