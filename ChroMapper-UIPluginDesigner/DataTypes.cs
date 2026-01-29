using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace ChroMapper_UIPluginDesigner
{
    public enum ElementType { Button, Label, TextInput, Dropdown, Checkbox, VerticalLayout, HorizontalLayout }

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

        public List<ElementData> Children = new List<ElementData>();

        public static ElementData FromJSON(JSONNode n)
        {
            var el = new ElementData();
            if (Enum.TryParse(n[UIConstants.KeyType].Value, out ElementType t))
                el.Type = t;
            else
                el.Type = ElementType.Button;

            el.Name = n[UIConstants.KeyName].Value;
            el.Text = n[UIConstants.KeyText].Value;
            el.AnchorPosX = n[UIConstants.KeyAnchorPosX].AsFloat;
            el.AnchorPosY = n[UIConstants.KeyAnchorPosY].AsFloat;
            el.SizeX = n[UIConstants.KeySizeX].AsFloat;
            el.SizeY = n[UIConstants.KeySizeY].AsFloat;
            el.FontSize = n[UIConstants.KeyFontSize].AsFloat;

            if (el.Type == ElementType.VerticalLayout || el.Type == ElementType.HorizontalLayout)
            {
                el.PaddingTop = n[UIConstants.KeyPaddingTop].AsInt;
                el.PaddingBottom = n[UIConstants.KeyPaddingBottom].AsInt;
                el.PaddingLeft = n[UIConstants.KeyPaddingLeft].AsInt;
                el.PaddingRight = n[UIConstants.KeyPaddingRight].AsInt;
                el.Spacing = n[UIConstants.KeySpacing].AsFloat;
                
                if (Enum.TryParse(n[UIConstants.KeyChildAlignment].Value, out TextAnchor align)) 
                    el.Alignment = align; 
                else 
                    el.Alignment = TextAnchor.UpperLeft;

                el.ChildControlWidth = n[UIConstants.KeyChildControlWidth].AsBool;
                el.ChildControlHeight = n[UIConstants.KeyChildControlHeight].AsBool;
                el.ChildForceExpandWidth = n[UIConstants.KeyChildForceExpandWidth].AsBool;
                el.ChildForceExpandHeight = n[UIConstants.KeyChildForceExpandHeight].AsBool;

                if (n[UIConstants.KeyChildren] != null)
                {
                    foreach (JSONNode childNode in n[UIConstants.KeyChildren].AsArray)
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
            n[UIConstants.KeyType] = Type.ToString();
            n[UIConstants.KeyName] = Name;
            n[UIConstants.KeyText] = Text;
            n[UIConstants.KeyAnchorPosX] = AnchorPosX;
            n[UIConstants.KeyAnchorPosY] = AnchorPosY;
            n[UIConstants.KeySizeX] = SizeX;
            n[UIConstants.KeySizeY] = SizeY;
            n[UIConstants.KeyFontSize] = FontSize;

            if (Type == ElementType.VerticalLayout || Type == ElementType.HorizontalLayout)
            {
                n[UIConstants.KeyPaddingTop] = PaddingTop;
                n[UIConstants.KeyPaddingBottom] = PaddingBottom;
                n[UIConstants.KeyPaddingLeft] = PaddingLeft;
                n[UIConstants.KeyPaddingRight] = PaddingRight;
                n[UIConstants.KeySpacing] = Spacing;
                n[UIConstants.KeyChildAlignment] = Alignment.ToString();
                n[UIConstants.KeyChildControlWidth] = ChildControlWidth;
                n[UIConstants.KeyChildControlHeight] = ChildControlHeight;
                n[UIConstants.KeyChildForceExpandWidth] = ChildForceExpandWidth;
                n[UIConstants.KeyChildForceExpandHeight] = ChildForceExpandHeight;

                var childrenArr = new JSONArray();
                foreach (var child in Children)
                {
                    childrenArr.Add(child.ToJSON());
                }
                n[UIConstants.KeyChildren] = childrenArr;
            }

            return n;
        }
    }
}
