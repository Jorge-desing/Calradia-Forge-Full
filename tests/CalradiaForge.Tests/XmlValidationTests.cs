using System;
using System.IO;
using System.Xml;
using CalradiaForge.Core;

namespace CalradiaForge.Tests
{
    public static class XmlValidationTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("Gauntlet Prefab XML is strictly valid", () =>
            {
                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml"));
                if (!File.Exists(path))
                {
                    // Fallback to checking from workspace root if running differently
                    path = Path.GetFullPath("../../../../../modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml");
                }
                
                if (!File.Exists(path)) throw new FileNotFoundException("Could not locate Gauntlet XML at " + path);
                
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(path);
                }
                catch (XmlException ex)
                {
                    throw new InvalidOperationException($"XML syntax error at line {ex.LineNumber}, pos {ex.LinePosition}: {ex.Message}");
                }

                if (doc.DocumentElement.Name != "Prefab") throw new InvalidOperationException("Root element must be Prefab");
                if (doc.GetElementsByTagName("Window").Count == 0) throw new InvalidOperationException("Missing Window element");
            });

            test("SubModule XML manifests are strictly valid", () =>
            {
                string[] modules = { "CalradiaForge", "CalradiaForgeExamples", "CalradiaForgePriceProvider", "CalradiaForgePriceConsumer" };
                foreach (var module in modules)
                {
                    string path = Path.GetFullPath($"modules/{module}/SubModule.xml");
                    if (!File.Exists(path)) continue;

                    XmlDocument doc = new XmlDocument();
                    try
                    {
                        doc.Load(path);
                    }
                    catch (XmlException ex)
                    {
                        throw new InvalidOperationException($"Syntax error in {module}/SubModule.xml at line {ex.LineNumber}, pos {ex.LinePosition}: {ex.Message}");
                    }
                    if (doc.DocumentElement.Name != "Module") throw new InvalidOperationException($"Root element of {module}/SubModule.xml must be Module");
                }
            });
        }
    }
}
