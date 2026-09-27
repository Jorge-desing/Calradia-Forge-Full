xml_content = '''<Prefab>
<Window>
  <!-- ROOT TACTICAL DARK BACKGROUND -->
  <!-- A dark-green cinematic background, using #0A0D0BFF -->
  <Widget Color="#0A0D0BFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9">
    <Children>
      
      <!-- TOP HEADER ROW -->
      <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="80" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="HorizontalLeftToRight" MarginTop="15" MarginLeft="30" MarginRight="30">
        <Children>
          
          <!-- LOGO TEXT (Extra large, highly visible) -->
          <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" Brush.FontSize="60" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="FORGE" VerticalAlignment="Center" MarginRight="20" />
          
          <!-- VERTICAL SEPARATOR AFTER LOGO -->
          <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="50" WidthSizePolicy="Fixed" SuggestedWidth="2" VerticalAlignment="Center" MarginRight="20" AlphaFactor="0.6" />
          
          <!-- LEFT HEADINGS -->
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Center">
            <Children>
              <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@Title" HorizontalAlignment="Left" MarginBottom="2" />
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@WorkbenchLabel" HorizontalAlignment="Left" />
            </Children>
          </ListPanel>
          
          <!-- SPACER -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" />
          
          <!-- RIGHT ACTIONS -->
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" StackLayout.LayoutMethod="HorizontalLeftToRight" VerticalAlignment="Center">
            <Children>
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@EnableLabel" VerticalAlignment="Center" MarginRight="25" />
              
              <ButtonWidget Command.Click="ExecuteClose" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="Fixed" SuggestedWidth="130" VerticalAlignment="Center">
                <Children>
                  <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.8">
                    <Children>
                      <Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="2" MarginRight="2" MarginTop="2" MarginBottom="2">
                        <Children>
                          <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@CloseLabel" />
                        </Children>
                      </Widget>
                    </Children>
                  </Widget>
                </Children>
              </ButtonWidget>
            </Children>
          </ListPanel>

        </Children>
      </ListPanel>

      <!-- DECORATIVE TOP DIVIDER: Double Line with Motif feel -->
      <Widget HeightSizePolicy="Fixed" SuggestedHeight="6" WidthSizePolicy="StretchToParent" MarginLeft="20" MarginRight="20" MarginTop="100">
        <Children>
          <Widget Sprite="Divider\horizontal_line" HeightSizePolicy="Fixed" SuggestedHeight="6" WidthSizePolicy="StretchToParent" AlphaFactor="0.8" />
          <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="Fixed" SuggestedWidth="60" HorizontalAlignment="Center" VerticalAlignment="Center" />
          <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="Fixed" SuggestedWidth="60" HorizontalAlignment="Left" VerticalAlignment="Center" MarginLeft="40" />
        </Children>
      </Widget>

      <!-- MAIN 2-COLUMN LAYOUT -->
      <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="120" MarginBottom="50" MarginLeft="20" MarginRight="20">
        <Children>
          
          <!-- LEFT SIDEBAR -->
          <!-- Inner background box with slight distinction -->
          <Widget Color="#101512FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="240" HorizontalAlignment="Left">
            <Children>
              
              <!-- Sidebar Header -->
              <Widget HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginTop="15">
                <Children>
                   <!-- Decorative motif -->
                   <TextWidget Brush="GameTip.Text" Brush.FontColor="#486151FF" Text="— ◇ —" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Center" HorizontalAlignment="Left" MarginLeft="10" />
                   <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@SectionsLabel" HorizontalAlignment="Center" VerticalAlignment="Center" />
                   <TextWidget Brush="GameTip.Text" Brush.FontColor="#486151FF" Text="— ◇ —" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Center" HorizontalAlignment="Right" MarginRight="10" />
                </Children>
              </Widget>
              
              <ListPanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="65" MarginBottom="70" MarginLeft="10" MarginRight="10" StackLayout.LayoutMethod="VerticalTopToBottom">
                <Children>
                  <!-- BUTTON MACRO EQUIVALENTS -->
                  <ButtonWidget Command.Click="ExecuteSummary" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@SummaryLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteModules" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@ModulesLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteLogs" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@LogsLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteInspector" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@InspectorLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteTests" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@TestsLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteMetrics" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@MetricsLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteSnapshots" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@SnapshotsLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteDependencies" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@DependenciesLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                </Children>
              </ListPanel>

              <!-- Export Button -->
              <ButtonWidget Command.Click="ExecuteExport" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="48" WidthSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10" VerticalAlignment="Bottom" MarginBottom="15">
                <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.9"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="2" MarginRight="2" MarginTop="2" MarginBottom="2"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@ExportLabel" /></Children></Widget></Children></Widget></Children>
              </ButtonWidget>
            </Children>
          </Widget>

          <!-- VERTICAL DIVIDER (Thicker, more decorative) -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="6" MarginLeft="250" HorizontalAlignment="Left">
            <Children>
              <Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="2" HorizontalAlignment="Center" />
              <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="1" HorizontalAlignment="Left" AlphaFactor="0.4" />
              <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="1" HorizontalAlignment="Right" AlphaFactor="0.4" />
            </Children>
          </Widget>

          <!-- RIGHT CONTENT PANEL -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="275">
            <Children>
              
              <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Top">
                <Children>
                  <!-- Section Title with Motif -->
                  <Widget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children>
                       <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@CurrentSectionLabel" HorizontalAlignment="Left" />
                       <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="StretchToParent" MarginLeft="150" VerticalAlignment="Center" AlphaFactor="0.3" />
                    </Children>
                  </Widget>
                  
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@SectionHelpLabel" HorizontalAlignment="Left" MarginBottom="25" />
                  
                  <!-- TOOLS ROW -->
                  <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="HorizontalLeftToRight" MarginBottom="10">
                    <Children>
                      <!-- Wider to prevent wrap -->
                      <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="170" Text="@InputLabel" HorizontalAlignment="Left" Brush.TextVerticalAlignment="Center" MarginRight="10" />
                      
                      <!-- INPUT BOX with decorative border -->
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="300" MarginRight="20" AlphaFactor="0.5">
                        <Children>
                          <Widget Color="#0B0F0DFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                             <Children>
                               <EditableTextWidget Brush="Standard.InputText" Brush.TextVerticalAlignment="Center" Text="@Argument" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10" />
                             </Children>
                          </Widget>
                        </Children>
                      </Widget>

                      <!-- ACTION BUTTONS: Increased to 160 width -->
                      <ButtonWidget Command.Click="ExecuteRefresh" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130" MarginRight="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RefreshLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                      <ButtonWidget Command.Click="ExecuteRun" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="160" MarginRight="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RunLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                      <ButtonWidget Command.Click="ExecutePin" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130" MarginRight="10"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@PinLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                      <ButtonWidget Command.Click="ExecuteCompare" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130"><Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.6"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@CompareLabel" /></Children></Widget></Children></Widget></Children></ButtonWidget>
                    </Children>
                  </ListPanel>

                  <!-- ARGUMENT SUGGESTIONS -->
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@ArgumentSuggestions" HorizontalAlignment="Left" MarginLeft="200" MarginBottom="15" />
                </Children>
              </ListPanel>

              <!-- CONTENT DISPLAY BOX WITH DECORATIVE CORNERS -->
              <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="150" AlphaFactor="0.4">
                <Children>
                  <!-- Content Box background -->
                  <Widget Color="#070A08FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                    <Children>
                      <!-- Top-left corner deco -->
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="4" WidthSizePolicy="Fixed" SuggestedWidth="4" HorizontalAlignment="Left" VerticalAlignment="Top" MarginLeft="2" MarginTop="2" />
                      <!-- Top-right corner deco -->
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="4" WidthSizePolicy="Fixed" SuggestedWidth="4" HorizontalAlignment="Right" VerticalAlignment="Top" MarginRight="2" MarginTop="2" />
                      <!-- Bottom-left corner deco -->
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="4" WidthSizePolicy="Fixed" SuggestedWidth="4" HorizontalAlignment="Left" VerticalAlignment="Bottom" MarginLeft="2" MarginBottom="2" />
                      <!-- Bottom-right corner deco -->
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="4" WidthSizePolicy="Fixed" SuggestedWidth="4" HorizontalAlignment="Right" VerticalAlignment="Bottom" MarginRight="2" MarginBottom="2" />

                      <TextWidget Brush="GameTip.Text" Brush.FontColor="#D1D5DBFF" Brush.TextHorizontalAlignment="Left" Brush.TextVerticalAlignment="Top" Text="@Content" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="15" MarginRight="15" MarginTop="15" MarginBottom="15" ClipContents="true" />
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
          <Widget Sprite="Divider\horizontal_line" HeightSizePolicy="Fixed" SuggestedHeight="6" WidthSizePolicy="StretchToParent" VerticalAlignment="Top" AlphaFactor="0.8" MarginLeft="20" MarginRight="20" />
          <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@NavigationLabel" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Bottom" HorizontalAlignment="Center" VerticalAlignment="Bottom" MarginBottom="10" />
        </Children>
      </Widget>

    </Children>
  </Widget>
</Window>
</Prefab>'''

open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(xml_content)
print("Updated XML with logo, thicker dividers, fixed wrapping, decorative corners, and motifs.")
