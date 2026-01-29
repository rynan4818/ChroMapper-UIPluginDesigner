using System.Collections.Generic;
using System.IO;
using System.Text;
using SFB;
using SimpleJSON;
using UnityEngine;

namespace ChroMapper_UIPluginDesigner
{
    public class LayoutFileManager
    {
        public void SaveLayout(RectTransform panelRect, List<ElementData> elements)
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Save UI Layout", "", "layout", "json");
            if (string.IsNullOrEmpty(path)) return;

            var root = new JSONObject();
            root[UIConstants.KeyPanelWidth] = panelRect.rect.width;
            root[UIConstants.KeyPanelHeight] = panelRect.rect.height;
            root[UIConstants.KeyPanelAnchorX] = panelRect.anchorMin.x;
            root[UIConstants.KeyPanelAnchorY] = panelRect.anchorMin.y;
            root[UIConstants.KeyPanelPosX] = panelRect.anchoredPosition.x;
            root[UIConstants.KeyPanelPosY] = panelRect.anchoredPosition.y;

            var arr = new JSONArray();
            foreach (var el in elements)
            {
                arr.Add(el.ToJSON());
            }
            root[UIConstants.KeyElements] = arr;

            File.WriteAllText(path, root.ToString(4));
            PersistentUI.Instance.DisplayMessage("Layout saved!", PersistentUI.DisplayMessageType.Bottom);
        }

        public JSONNode LoadLayout(out string path)
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Load UI Layout", "", "json", false);
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            {
                path = null;
                return null;
            }

            path = paths[0];
            string json = File.ReadAllText(path);
            return JSON.Parse(json);
        }

        public void ExportCode(List<ElementData> elements)
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Export Generated Code", "", "GeneratedUI", "txt");
            if (string.IsNullOrEmpty(path)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// --- Generated Code (UI.cs style) ---");
            sb.AppendLine("// Add this to your MenuUI.cs or equivalent");
            sb.AppendLine("private void CreateUI(UI ui, GameObject menu)");
            sb.AppendLine("{");

            foreach (var el in elements)
            {
                ExportElement(sb, el, "menu.transform");
            }
            sb.AppendLine("}");
            
            File.WriteAllText(path, sb.ToString());
            PersistentUI.Instance.DisplayMessage("Code exported to file!", PersistentUI.DisplayMessageType.Bottom);
        }

        private void ExportElement(StringBuilder sb, ElementData el, string parentVar)
        {
            string x = el.AnchorPosX.ToString("F1") + "f";
            string y = el.AnchorPosY.ToString("F1") + "f";
            string w = el.SizeX.ToString("F0");
            string h = el.SizeY.ToString("F0");
            string f = el.FontSize.ToString("F0");

            switch (el.Type)
            {
                case ElementType.Button:
                    sb.AppendLine($"    ui.AddButton({parentVar}, \"{el.Name}\", \"{el.Text}\", {f}, {w}, {h}, 0.5f, 0.5f, {x}, {y}, () => {{}}); // Note: Click handler is a placeholder");
                    break;
                case ElementType.Label:
                    sb.AppendLine($"    ui.AddLabel({parentVar}, \"{el.Name}\", \"{el.Text}\", {w}, {h}, 0.5f, 0.5f, {x}, {y}, TextAlignmentOptions.Center, {f});");
                    break;
                case ElementType.TextInput:
                    sb.AppendLine($"    ui.AddTextInput({parentVar}, \"{el.Name}\", \"{el.Text}\", TextAlignmentOptions.Left, {f}, {w}, {h}, 0.5f, 0.5f, {x}, {y}, (val) => {{}}); // Note: OnChange handler is a placeholder");
                    break;
                case ElementType.Dropdown:
                    sb.AppendLine($"    ui.AddDropdown({parentVar}, new List<string>(), 0, {w}, {h}, 0.5f, 0.5f, {x}, {y}, (val) => {{}}); // Note: OnChange handler is a placeholder");
                    break;
                case ElementType.Checkbox:
                    sb.AppendLine($"    ui.AddCheckbox({parentVar}, true, {w}, {h}, 0.5f, 0.5f, {x}, {y}, (val) => {{}}); // Note: OnValueChanged handler is a placeholder");
                    break;
                case ElementType.VerticalLayout:
                case ElementType.HorizontalLayout:
                    string varName = "go_" + el.Name.Replace(" ", "_").Replace("-", "_");
                    sb.AppendLine($"    // Layout Group: {el.Name}");
                    sb.AppendLine($"    var {varName} = new GameObject(\"{el.Name}\");");
                    sb.AppendLine($"    {varName}.transform.SetParent({parentVar}, false);");
                    sb.AppendLine($"    var rt_{varName} = {varName}.AddComponent<RectTransform>();");
                    sb.AppendLine($"    ui.MoveTransform(rt_{varName}, {w}, {h}, 0.5f, 0.5f, {x}, {y});");
                    
                    string groupType = (el.Type == ElementType.VerticalLayout) ? "VerticalLayoutGroup" : "HorizontalLayoutGroup";
                    sb.AppendLine($"    var lg_{varName} = {varName}.AddComponent<{groupType}>();");
                    sb.AppendLine($"    lg_{varName}.padding = new RectOffset({el.PaddingLeft}, {el.PaddingRight}, {el.PaddingTop}, {el.PaddingBottom});");
                    sb.AppendLine($"    lg_{varName}.spacing = {el.Spacing}f;");
                    sb.AppendLine($"    lg_{varName}.childAlignment = TextAnchor.{el.Alignment};");
                    sb.AppendLine($"    lg_{varName}.childControlWidth = {(el.ChildControlWidth ? "true" : "false")};");
                    sb.AppendLine($"    lg_{varName}.childControlHeight = {(el.ChildControlHeight ? "true" : "false")};");
                    sb.AppendLine($"    lg_{varName}.childForceExpandWidth = {(el.ChildForceExpandWidth ? "true" : "false")};");
                    sb.AppendLine($"    lg_{varName}.childForceExpandHeight = {(el.ChildForceExpandHeight ? "true" : "false")};");

                    foreach (var child in el.Children)
                    {
                        ExportElement(sb, child, $"{varName}.transform");
                    }
                    break;
            }
        }
    }
}
