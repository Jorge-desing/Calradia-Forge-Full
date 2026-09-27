import re
content = open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', encoding='utf-8').read()

# Replace the plain TextWidget @Content with a ScrollablePanel wrapping it
old = '''<TextWidget Brush="GameTip.Text" Brush.FontColor="#D1D5DBFF" Brush.TextHorizontalAlignment="Left"
                                  Brush.TextVerticalAlignment="Top" Text="@Content" HeightSizePolicy="StretchToParent"
                                  WidthSizePolicy="StretchToParent" MarginLeft="15" MarginRight="15"
                                  MarginTop="15" MarginBottom="15" ClipContents="true" />'''

new = '''<ScrollablePanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
                                      MarginLeft="4" MarginRight="4" MarginTop="4" MarginBottom="4"
                                      InnerPanel.WidthSizePolicy="StretchToParent"
                                      InnerPanel.HeightSizePolicy="CoverChildren">
                                    <ScrollablePanel.InnerPanel>
                                      <Widget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent">
                                        <Children>
                                          <TextWidget Brush="GameTip.Text" Brush.FontColor="#D1D5DBFF"
                                                      Brush.TextHorizontalAlignment="Left"
                                                      Brush.TextVerticalAlignment="Top"
                                                      Text="@Content"
                                                      HeightSizePolicy="CoverChildren"
                                                      WidthSizePolicy="StretchToParent"
                                                      MarginLeft="12" MarginRight="12"
                                                      MarginTop="12" MarginBottom="12"
                                                      ClipContents="false" />
                                        </Children>
                                      </Widget>
                                    </ScrollablePanel.InnerPanel>
                                  </ScrollablePanel>'''

# Normalize whitespace for matching
def normalize(s):
    return re.sub(r'\s+', ' ', s).strip()

norm_old = normalize(old)
norm_content = normalize(content)
if norm_old in norm_content:
    content = re.sub(re.escape(norm_old), normalize(new), norm_content)
    open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(content)
    print("Scroll added successfully")
else:
    # Try direct string replacement
    old_compact = '<TextWidget Brush="GameTip.Text" Brush.FontColor="#D1D5DBFF" Brush.TextHorizontalAlignment="Left" Brush.TextVerticalAlignment="Top" Text="@Content" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="15" MarginRight="15" MarginTop="15" MarginBottom="15" ClipContents="true" />'
    new_compact = '<ScrollablePanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="4" MarginRight="4" MarginTop="4" MarginBottom="4" InnerPanel.WidthSizePolicy="StretchToParent" InnerPanel.HeightSizePolicy="CoverChildren"><ScrollablePanel.InnerPanel><Widget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#D1D5DBFF" Brush.TextHorizontalAlignment="Left" Brush.TextVerticalAlignment="Top" Text="@Content" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="12" MarginRight="12" MarginTop="12" MarginBottom="12" ClipContents="false" /></Children></Widget></ScrollablePanel.InnerPanel></ScrollablePanel>'
    if old_compact in content:
        content = content.replace(old_compact, new_compact)
        open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(content)
        print("Scroll added (compact match)")
    else:
        print("ERROR: Could not find target. Content snippet:")
        idx = content.find('@Content')
        print(repr(content[max(0,idx-200):idx+200]))
