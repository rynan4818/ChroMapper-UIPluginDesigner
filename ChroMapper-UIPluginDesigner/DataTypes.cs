using System;
using SimpleJSON;

namespace ChroMapper_UIPluginDesigner
{
    public enum ElementType { Button, Label, TextInput, Dropdown, Checkbox }

    [Serializable]
    public class ElementData
    {
        public ElementType Type;
        public string Name;
        public string Text;
        public float AnchorPosX, AnchorPosY;
        public float SizeX, SizeY;
        public float FontSize;

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
            return n;
        }
    }
}
