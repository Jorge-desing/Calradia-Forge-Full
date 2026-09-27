xml_content = '''<Prefab>
<Window>
  <!-- ROOT TACTICAL DARK BACKGROUND -->
  <!-- Using a slightly lighter dark-green so it doesn't blend perfectly into the black void -->
  <Widget Color="#0D120FFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9">
    <Children>
      
      <!-- TOP HEADER ROW -->
      <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="60" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="HorizontalLeftToRight" MarginTop="15" MarginLeft="30" MarginRight="30">
        <Children>
          
          <!-- LEFT HEADINGS -->
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Center">
            <Children>
              <!-- Overriding FontColor to make it distinctly Gold -->
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
              
              <!-- FLAT BUTTON FOR CLOSE -->
              <ButtonWidget Command.Click="ExecuteClose" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="Fixed" SuggestedWidth="120" VerticalAlignment="Center">
                <Children>
                  <!-- Border -->
                  <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.8">
                    <Children>
                      <!-- Inner background -->
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

      <!-- TOP DIVIDER: Thick gold line to be explicitly visible -->
      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="StretchToParent" MarginLeft="20" MarginRight="20" MarginTop="80" AlphaFactor="0.6" />

      <!-- MAIN 2-COLUMN LAYOUT -->
      <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="90" MarginBottom="50" MarginLeft="20" MarginRight="20">
        <Children>
          
          <!-- LEFT SIDEBAR -->
          <!-- Made background slightly distinct from root to stand out -->
          <Widget Color="#131B17FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="240" HorizontalAlignment="Left">
            <Children>
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@SectionsLabel" HorizontalAlignment="Left" VerticalAlignment="Top" MarginTop="15" MarginLeft="15" MarginBottom="15" />
              
              <ListPanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="45" MarginBottom="70" MarginLeft="10" MarginRight="10" StackLayout.LayoutMethod="VerticalTopToBottom">
                <Children>
                  <!-- BUTTON MACRO EQUIVALENTS (Flat Tactical) -->
                  <!-- Summary -->
                  <ButtonWidget Command.Click="ExecuteSummary" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@SummaryLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Modules -->
                  <ButtonWidget Command.Click="ExecuteModules" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@ModulesLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Logs -->
                  <ButtonWidget Command.Click="ExecuteLogs" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@LogsLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Inspector -->
                  <ButtonWidget Command.Click="ExecuteInspector" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@InspectorLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Tests -->
                  <ButtonWidget Command.Click="ExecuteTests" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@TestsLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Metrics -->
                  <ButtonWidget Command.Click="ExecuteMetrics" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@MetricsLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Snapshots -->
                  <ButtonWidget Command.Click="ExecuteSnapshots" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@SnapshotsLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                  <!-- Dependencies -->
                  <ButtonWidget Command.Click="ExecuteDependencies" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginBottom="8">
                    <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@DependenciesLabel" /></Children></Widget></Children></Widget></Children>
                  </ButtonWidget>
                </Children>
              </ListPanel>

              <!-- Export Button at bottom -->
              <ButtonWidget Command.Click="ExecuteExport" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10" VerticalAlignment="Bottom" MarginBottom="15">
                <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.8"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="2" MarginRight="2" MarginTop="2" MarginBottom="2"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@ExportLabel" /></Children></Widget></Children></Widget></Children>
              </ButtonWidget>
            </Children>
          </Widget>

          <!-- VERTICAL DIVIDER -->
          <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="2" MarginLeft="250" HorizontalAlignment="Left" AlphaFactor="0.6" />

          <!-- RIGHT CONTENT PANEL -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="265">
            <Children>
              
              <!-- SECTION HEADERS -->
              <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Top">
                <Children>
                  <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@CurrentSectionLabel" HorizontalAlignment="Left" MarginBottom="8" />
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="@SectionHelpLabel" HorizontalAlignment="Left" MarginBottom="20" />
                  
                  <!-- TOOLS ROW -->
                  <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="HorizontalLeftToRight" MarginBottom="10">
                    <Children>
                      <!-- Using width 140 for "Buscar/argumento" to prevent line wrap -->
                      <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="140" Text="@InputLabel" HorizontalAlignment="Left" VerticalAlignment="Center" Brush.TextVerticalAlignment="Center" MarginRight="10" />
                      
                      <!-- INPUT BOX -->
                      <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="280" MarginRight="20">
                        <Children>
                          <Widget Color="#0B0F0DFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                             <Children>
                               <!-- Ensure text is vertically centered in the input -->
                               <EditableTextWidget Brush="Standard.InputText" Brush.TextVerticalAlignment="Center" Text="@Argument" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10" />
                             </Children>
                          </Widget>
                        </Children>
                      </Widget>

                      <!-- ACTION BUTTONS: Increased width to 140 to avoid wrapping on "Ejecutar prueba" -->
                      <ButtonWidget Command.Click="ExecuteRefresh" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="140" MarginRight="10">
                        <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RefreshLabel" /></Children></Widget></Children></Widget></Children>
                      </ButtonWidget>
                      <ButtonWidget Command.Click="ExecuteRun" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="140" MarginRight="10">
                        <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RunLabel" /></Children></Widget></Children></Widget></Children>
                      </ButtonWidget>
                      <ButtonWidget Command.Click="ExecutePin" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="140" MarginRight="10">
                        <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@PinLabel" /></Children></Widget></Children></Widget></Children>
                      </ButtonWidget>
                      <ButtonWidget Command.Click="ExecuteCompare" DoNotPassEventsToChildren="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="140">
                        <Children><Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" AlphaFactor="0.5"><Children><Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1"><Children><TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@CompareLabel" /></Children></Widget></Children></Widget></Children>
                      </ButtonWidget>
                    </Children>
                  </ListPanel>

                  <!-- ARGUMENT SUGGESTIONS HINT -->
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@ArgumentSuggestions" HorizontalAlignment="Left" MarginLeft="170" MarginBottom="15" />
                </Children>
              </ListPanel>

              <!-- CONTENT DISPLAY BOX -->
              <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="140">
                <Children>
                  <Widget Color="#0B0F0DFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                    <Children>
                      <!-- Forcing TextHorizontalAlignment and TextVerticalAlignment inside the Brush explicitly! -->
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
          <!-- Footer divider -->
          <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="StretchToParent" VerticalAlignment="Top" AlphaFactor="0.6" MarginLeft="20" MarginRight="20" />
          <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@NavigationLabel" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Bottom" HorizontalAlignment="Center" VerticalAlignment="Bottom" MarginBottom="10" />
        </Children>
      </Widget>

    </Children>
  </Widget>
</Window>
</Prefab>'''

open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(xml_content)
print("Updated XML with PERFECT tactical flat buttons, alignment fixes, and visible decorations.")
