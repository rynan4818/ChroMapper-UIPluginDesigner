using System.Collections.Generic;
using System.IO;
using System.Text;
using SFB;
using SimpleJSON;
using UnityEngine;

namespace UIPluginDesigner
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
