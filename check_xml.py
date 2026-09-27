import xml.etree.ElementTree as ET

try:
    ET.parse('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml')
    print('XML is VALID')
except Exception as e:
    print('XML is INVALID:', e)
