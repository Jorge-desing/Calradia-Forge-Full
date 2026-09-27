import re

f = 'modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml'
content = open(f, encoding='utf-8').read()

# 1. Remove all those old tiny squares (MarginTop="72")
content = re.sub(r'<Widget Color="#BA9654FF" HeightSizePolicy="Fixed" MarginLeft="\d+" MarginTop="72"[^>]+/>', '', content)

# 2. Add the new tactical expanding divider right after the ListPanel (MarginTop="150" or similar)
# We already have one at MarginTop="70" from fix_gauntlet_divider.py:
# <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="10" MarginTop="70" MarginLeft="24" MarginRight="24" HorizontalAlignment="Left">
# We can add another one for the Sections line if needed.
# But wait, the user said "debajo de la navegación principal y de las secciones en el juego".
# So they want one under the title (MarginTop="70") and one under the buttons (MarginTop="160").

new_divider_buttons = '''<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="10" MarginTop="160" MarginLeft="24" MarginRight="24" HorizontalAlignment="Left">
    <Children>
        <Widget Color="#C7A45A80" WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="2" VerticalAlignment="Center" MarginLeft="40" MarginRight="40" Sprite="BlankWhiteSquare_9" />
        <Widget Color="#1A2921FF" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="20" SuggestedHeight="20" HorizontalAlignment="Center" VerticalAlignment="Center" Sprite="General\\compass_marker" />
        <Widget Color="#C7A45AFF" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="14" SuggestedHeight="14" HorizontalAlignment="Center" VerticalAlignment="Center" Sprite="General\\compass_marker" />
        <Widget Color="#C7A45A80" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="6" SuggestedHeight="6" HorizontalAlignment="Left" VerticalAlignment="Center" Sprite="General\\compass_marker" />
        <Widget Color="#C7A45A80" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="6" SuggestedHeight="6" HorizontalAlignment="Right" VerticalAlignment="Center" Sprite="General\\compass_marker" />
    </Children>
</Widget>'''

# Add SectionHelpLabel text
section_help_text = '''<TextWidget Brush="GameTip.Text" HeightSizePolicy="Fixed" MarginLeft="34" MarginTop="180" SuggestedHeight="32" SuggestedWidth="1100" Text="@SectionHelpLabel" WidthSizePolicy="Fixed" HorizontalAlignment="Left" />'''

# Insert them before the first ListPanel to just add them to the root Window
content = content.replace('<ListPanel', new_divider_buttons + section_help_text + '<ListPanel', 1)

open(f, 'w', encoding='utf-8').write(content)
print('Done Gauntlet')
