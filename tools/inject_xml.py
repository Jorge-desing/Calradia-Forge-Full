import re

with open(r'modules\CalradiaForge\GUI\Prefabs\CalradiaForge.xml', 'r', encoding='utf-8') as f:
    content = f.read()

tab_button = '''                            <ButtonWidget Id="ForgeCategorySdk" Command.Click="ExecuteCategorySdk" Hint.HintText="@CategorySdkHint" IsSelected="@IsCategorySdkActive" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TabButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="142" MarginRight="6"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@CategorySdkLabel" /></Children></ButtonWidget>
'''
content = content.replace('<ButtonWidget Id="ForgeCategoryNovice"', tab_button + '                            <ButtonWidget Id="ForgeCategoryNovice"')

sdk_container = '''
                          <!-- CATEGORY 8: ADVANCED SDK (DYNAMIC) -->
                          <ListPanel IsVisible="@IsCategorySdkActive" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalBottomToTop">
                            <Children>
                              <!-- Search Bar -->
                              <EditableTextWidget Text="@SearchText" Brush="CalradiaForge.TextInput" Hint.HintText="@SearchHint" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="12" />
                              
                              <!-- Dynamic Tools List -->
                              <ListPanel DataSource="{SdkTools}" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalBottomToTop">
                                <ItemTemplate>
                                  <ButtonWidget Command.Click="ExecuteSelect" Hint.HintText="@Hint" IsSelected="@IsSelected" IsVisible="@IsVisible" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="8">
                                    <Children>
                                      <TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@Title" />
                                    </Children>
                                  </ButtonWidget>
                                </ItemTemplate>
                              </ListPanel>
                            </Children>
                          </ListPanel>
'''

match = re.search(r'<ButtonWidget Id="ForgeNoviceCombat"([^>]+)><Children><TextWidget([^>]+)/></Children></ButtonWidget>', content)
if match:
    original = match.group(0)
    content = content.replace(original, original + sdk_container)
else:
    print("Could not find ForgeNoviceCombat")

with open(r'modules\CalradiaForge\GUI\Prefabs\CalradiaForge.xml', 'w', encoding='utf-8') as f:
    f.write(content)
