content = open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', encoding='utf-8').read()
lines = content.split('\n')
if len(lines) >= 2:
    line2 = lines[1]
    print(line2[max(0, 684-40) : min(len(line2), 684+40)])
