using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace ChroMapper_UIPluginDesigner
{
    public static class TemplateManager
    {
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();
        private const string ResourcePrefix = "ChroMapper_UIPluginDesigner.Templates.";

        public static string GetTemplate(string templateName)
        {
            if (_cache.ContainsKey(templateName)) return _cache[templateName];

            string content = "";
            string fileName = templateName + ".txt";
            
            // 1. Check local file (DLL folder)
            string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            // Check in "UITemplates" folder first, then root if needed (as per typical plugin structure)
            // But per request "DLLと同じフォルダに同名のテンプレートが有る場合は"
            // We will check directly next to DLL first as requested.
            string localPath = Path.Combine(dllPath, "Templates", fileName);
            string legacyPath = Path.Combine(dllPath, fileName); 

            if (File.Exists(legacyPath))
            {
                content = File.ReadAllText(legacyPath);
            }
            else if (File.Exists(localPath))
            {
                content = File.ReadAllText(localPath);
            }
            else
            {
                // 2. Embedded Resource
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = ResourcePrefix + fileName;
                
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            content = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        Debug.LogError($"Template not found: {resourceName}");
                        content = $"// Template not found: {templateName}";
                    }
                }
            }

            _cache[templateName] = content;
            return content;
        }
    }
}
