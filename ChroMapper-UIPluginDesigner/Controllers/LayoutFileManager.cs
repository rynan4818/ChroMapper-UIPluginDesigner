using System.Collections.Generic;
using System.IO;
using System.Text;
using SFB;
using SimpleJSON;
using UnityEngine;
using ChroMapper_UIPluginDesigner.UserResources;

namespace ChroMapper_UIPluginDesigner.Controllers
{
    public class LayoutFileManager
    {
        public void SaveLayout(RectTransform panelRect, List<ElementData> elements)
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Save UI Layout", "", "layout", "json");
            if (string.IsNullOrEmpty(path)) return;

            var root = new JSONObject();
            root[UILayoutMap.KeyPanelWidth] = panelRect.rect.width;
            root[UILayoutMap.KeyPanelHeight] = panelRect.rect.height;
            root[UILayoutMap.KeyPanelAnchorX] = panelRect.anchorMin.x;
            root[UILayoutMap.KeyPanelAnchorY] = panelRect.anchorMin.y;
            root[UILayoutMap.KeyPanelPosX] = panelRect.anchoredPosition.x;
            root[UILayoutMap.KeyPanelPosY] = panelRect.anchoredPosition.y;

            var arr = new JSONArray();
            foreach (var el in elements)
            {
                arr.Add(el.ToJSON());
            }
            root[UILayoutMap.KeyElements] = arr;

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
            string safeName = el.Name.Replace(" ", "_").Replace("-", "_");

            string code = "";

            switch (el.Type)
            {
                case ElementType.Button:
                    code = TemplateManager.GetTemplate("Button")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{Text}}", el.Text)
                        .Replace("{{FontSize}}", f)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y);
                    break;
                case ElementType.Label:
                    code = TemplateManager.GetTemplate("Label")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{Text}}", el.Text)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y)
                        .Replace("{{FontSize}}", f);
                    break;
                case ElementType.TextInput:
                    code = TemplateManager.GetTemplate("TextInput")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{Text}}", el.Text)
                        .Replace("{{FontSize}}", f)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y);
                    break;
                case ElementType.Dropdown:
                    code = TemplateManager.GetTemplate("Dropdown")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y);
                    break;
                case ElementType.Checkbox:
                    code = TemplateManager.GetTemplate("Checkbox")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y);
                    break;
                case ElementType.RadioButton:
                    code = TemplateManager.GetTemplate("RadioButton")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{SafeName}}", safeName)
                        .Replace("{{Text}}", el.Text)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y)
                        .Replace("{{LabelWidth}}", (el.SizeX - 20).ToString());
                    break;
                case ElementType.Slider:
                    code = TemplateManager.GetTemplate("Slider")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{Min}}", el.MinValue.ToString())
                        .Replace("{{Max}}", el.MaxValue.ToString())
                        .Replace("{{IsInt}}", el.IsInteger ? "true" : "false")
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y);
                    break;
                case ElementType.Image:
                    code = TemplateManager.GetTemplate("Image")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{SafeName}}", safeName)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y)
                        .Replace("{{HexColor}}", el.HexColor);
                    break;
                case ElementType.VerticalLayout:
                case ElementType.HorizontalLayout:
                    string groupType = (el.Type == ElementType.VerticalLayout) ? "VerticalLayoutGroup" : "HorizontalLayoutGroup";
                    code = TemplateManager.GetTemplate("LayoutGroup")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{SafeName}}", safeName)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y)
                        .Replace("{{GroupType}}", groupType)
                        .Replace("{{PaddingLeft}}", el.PaddingLeft.ToString())
                        .Replace("{{PaddingRight}}", el.PaddingRight.ToString())
                        .Replace("{{PaddingTop}}", el.PaddingTop.ToString())
                        .Replace("{{PaddingBottom}}", el.PaddingBottom.ToString())
                        .Replace("{{Spacing}}", el.Spacing.ToString())
                        .Replace("{{Alignment}}", el.Alignment.ToString())
                        .Replace("{{ChildControlWidth}}", el.ChildControlWidth ? "true" : "false")
                        .Replace("{{ChildControlHeight}}", el.ChildControlHeight ? "true" : "false")
                        .Replace("{{ChildForceExpandWidth}}", el.ChildForceExpandWidth ? "true" : "false")
                        .Replace("{{ChildForceExpandHeight}}", el.ChildForceExpandHeight ? "true" : "false");
                    
                    sb.AppendLine(code);
                    foreach (var child in el.Children)
                    {
                        ExportElement(sb, child, $"go_{safeName}.transform");
                    }
                    return; // Early return because layout group handles children recursion
                case ElementType.ScrollRect:
                    code = TemplateManager.GetTemplate("ScrollRect")
                        .Replace("{{Parent}}", parentVar)
                        .Replace("{{Name}}", el.Name)
                        .Replace("{{SafeName}}", safeName)
                        .Replace("{{Width}}", w)
                        .Replace("{{Height}}", h)
                        .Replace("{{PosX}}", x)
                        .Replace("{{PosY}}", y)
                        .Replace("{{ScrollSensitivity}}", el.ScrollSensitivity.ToString())
                        .Replace("{{PaddingLeft}}", el.PaddingLeft.ToString())
                        .Replace("{{PaddingRight}}", el.PaddingRight.ToString())
                        .Replace("{{PaddingTop}}", el.PaddingTop.ToString())
                        .Replace("{{PaddingBottom}}", el.PaddingBottom.ToString())
                        .Replace("{{Spacing}}", el.Spacing.ToString())
                        .Replace("{{Alignment}}", el.Alignment.ToString())
                        .Replace("{{ChildControlWidth}}", el.ChildControlWidth ? "true" : "false")
                        .Replace("{{ChildControlHeight}}", el.ChildControlHeight ? "true" : "false")
                        .Replace("{{ChildForceExpandWidth}}", el.ChildForceExpandWidth ? "true" : "false")
                        .Replace("{{ChildForceExpandHeight}}", el.ChildForceExpandHeight ? "true" : "false")
                        .Replace("{{ScrollVisibility}}", el.ScrollVisibility.ToString());

                    sb.AppendLine(code);
                    foreach (var child in el.Children)
                    {
                        ExportElement(sb, child, $"ct_{safeName}.transform");
                    }
                    return;
            }

            if (!string.IsNullOrEmpty(code))
            {
                sb.AppendLine(code);
            }
        }
    }
}
