xml_content = '''<Prefab>
<Window>
  <!-- ROOT TACTICAL DARK BACKGROUND -->
  <Widget Color="#101512FF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9">
    <Children>
      
      <!-- TOP HEADER ROW -->
      <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="60" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="HorizontalLeftToRight" MarginTop="15" MarginLeft="30" MarginRight="30">
        <Children>
          
          <!-- LEFT HEADINGS -->
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Center">
            <Children>
              <TextWidget Brush="SPOptions.Title.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@Title" HorizontalAlignment="Left" MarginBottom="2" />
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@WorkbenchLabel" HorizontalAlignment="Left" />
            </Children>
          </ListPanel>
          
          <!-- SPACER -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" />
          
          <!-- RIGHT ACTIONS -->
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" StackLayout.LayoutMethod="HorizontalLeftToRight" VerticalAlignment="Center">
            <Children>
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@EnableLabel" VerticalAlignment="Center" MarginRight="25" />
              <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteClose" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="Fixed" SuggestedWidth="120" VerticalAlignment="Center">
                <Children>
                  <TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@CloseLabel" />
                </Children>
              </ButtonWidget>
            </Children>
          </ListPanel>

        </Children>
      </ListPanel>

      <!-- TOP DIVIDER -->
      <Widget Sprite="Divider\horizontal_line" HeightSizePolicy="Fixed" SuggestedHeight="6" WidthSizePolicy="StretchToParent" MarginLeft="20" MarginRight="20" MarginTop="80" AlphaFactor="0.6" />

      <!-- MAIN 2-COLUMN LAYOUT -->
      <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="100" MarginBottom="50" MarginLeft="30" MarginRight="30">
        <Children>
          
          <!-- LEFT SIDEBAR -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="230" HorizontalAlignment="Left">
            <Children>
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@SectionsLabel" HorizontalAlignment="Left" VerticalAlignment="Top" MarginBottom="15" />
              
              <ListPanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="35" MarginBottom="70" StackLayout.LayoutMethod="VerticalTopToBottom">
                <Children>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteSummary" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@SummaryLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteModules" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@ModulesLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteLogs" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@LogsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteInspector" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@InspectorLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteTests" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@TestsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteMetrics" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@MetricsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteSnapshots" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@SnapshotsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteDependencies" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@DependenciesLabel" /></Children>
                  </ButtonWidget>
                </Children>
              </ListPanel>

              <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteExport" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="50" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom">
                <Children>
                  <TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@ExportLabel" />
                </Children>
              </ButtonWidget>
            </Children>
          </Widget>

          <!-- VERTICAL DIVIDER -->
          <Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="2" MarginLeft="255" HorizontalAlignment="Left" />

          <!-- RIGHT CONTENT PANEL -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="280">
            <Children>
              
              <!-- SECTION HEADERS -->
              <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Top">
                <Children>
                  <TextWidget Brush="SPOptions.Title.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@CurrentSectionLabel" HorizontalAlignment="Left" MarginBottom="8" />
                  <TextWidget Brush="GameTip.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@SectionHelpLabel" HorizontalAlignment="Left" MarginBottom="25" />
                  
                  <!-- TOOLS ROW -->
                  <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="HorizontalLeftToRight" MarginBottom="10">
                    <Children>
                      <TextWidget Brush="GameTip.Text" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="160" Text="@InputLabel" HorizontalAlignment="Left" VerticalAlignment="Center" MarginRight="10" />
                      
                      <!-- INPUT BOX -->
                      <Widget Color="#0B0F0DFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="280" MarginRight="20">
                        <Children>
                          <EditableTextWidget Brush="Standard.InputText" Text="@Argument" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10" VerticalAlignment="Center" />
                        </Children>
                      </Widget>

                      <!-- ACTION BUTTONS -->
                      <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteRefresh" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="110" MarginRight="10">
                        <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@RefreshLabel" /></Children>
                      </ButtonWidget>
                      <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteRun" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="110" MarginRight="10">
                        <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@RunLabel" /></Children>
                      </ButtonWidget>
                      <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecutePin" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="110" MarginRight="10">
                        <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@PinLabel" /></Children>
                      </ButtonWidget>
                      <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteCompare" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="110">
                        <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@CompareLabel" /></Children>
                      </ButtonWidget>
                    </Children>
                  </ListPanel>

                  <!-- ARGUMENT SUGGESTIONS HINT -->
                  <TextWidget Brush="GameTip.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@ArgumentSuggestions" HorizontalAlignment="Left" MarginLeft="170" MarginBottom="15" />
                </Children>
              </ListPanel>

              <!-- CONTENT DISPLAY BOX -->
              <!-- We use MarginTop="160" to push it below the headers and tools row -->
              <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="150">
                <Children>
                  <Widget Color="#0B0F0DFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                    <Children>
                      <TextWidget Brush="GameTip.Text" Text="@Content" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Left" VerticalAlignment="Top" MarginLeft="15" MarginRight="15" MarginTop="15" MarginBottom="15" ClipContents="true" />
                    </Children>
                  </Widget>
                </Children>
              </Widget>
              
            </Children>
          </Widget>

        </Children>
      </Widget>

      <!-- BOTTOM FOOTER -->
      <Widget HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom">
        <Children>
          <Widget Sprite="Divider\horizontal_line" HeightSizePolicy="Fixed" SuggestedHeight="6" WidthSizePolicy="StretchToParent" VerticalAlignment="Top" AlphaFactor="0.6" MarginLeft="10" MarginRight="10" />
          <TextWidget Brush="GameTip.Text" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@NavigationLabel" HorizontalAlignment="Center" VerticalAlignment="Bottom" MarginBottom="10" />
        </Children>
      </Widget>

    </Children>
  </Widget>
</Window>
</Prefab>'''

open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(xml_content)
print("Updated XML with correct bounds and properties.")
