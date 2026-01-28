using System;
using SimpleJSON;

namespace UIPluginDesigner
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
            if (Enum.TryParse(n["Type"].Value, out ElementType t))
                el.Type = t;
            else
                el.Type = ElementType.Button;

            el.Name = n["Name"].Value;
            el.Text = n["Text"].Value;
            el.AnchorPosX = n["AnchorPosX"].AsFloat;
            el.AnchorPosY = n["AnchorPosY"].AsFloat;
            el.SizeX = n["SizeX"].AsFloat;
            el.SizeY = n["SizeY"].AsFloat;
            el.FontSize = n["FontSize"].AsFloat;
            return el;
        }

        public JSONObject ToJSON()
        {
            var n = new JSONObject();
            n["Type"] = Type.ToString();
            n["Name"] = Name;
            n["Text"] = Text;
            n["AnchorPosX"] = AnchorPosX;
            n["AnchorPosY"] = AnchorPosY;
            n["SizeX"] = SizeX;
            n["SizeY"] = SizeY;
            n["FontSize"] = FontSize;
            return n;
        }
    }
}
