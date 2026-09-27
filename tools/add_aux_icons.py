import sys
from pathlib import Path
from xml.etree import ElementTree

doc = ElementTree.parse("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml")
root = doc.getroot()
shell = next(e for e in root.iter() if e.attrib.get("Id") == "ForgeWorkbenchShell")
children = shell.find("Children")

# Check if already present
if not any(e.attrib.get("Id") == "ForgeAuxiliaryIcons" for e in children):
    aux = ElementTree.SubElement(children, "Widget", Id="ForgeAuxiliaryIcons", IsVisible="false", WidthSizePolicy="Fixed", HeightSizePolicy="Fixed", SuggestedWidth="0", SuggestedHeight="0", DoNotAcceptEvents="true", DoNotPassEventsToChildren="true")
    aux_children = ElementTree.SubElement(aux, "Children")
    for s in ["calradiaforge_gears", "calradiaforge_gear_hammer", "calradiaforge_magnifying_glass", "calradiaforge_stopwatch"]:
        ElementTree.SubElement(aux_children, "ImageWidget", Sprite=s, WidthSizePolicy="Fixed", HeightSizePolicy="Fixed", SuggestedWidth="0", SuggestedHeight="0")

    doc.write("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml", encoding="utf-8", xml_declaration=True)
    print("Injected ForgeAuxiliaryIcons successfully.")
else:
    print("Already present.")
