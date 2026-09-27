import re
content = '''<Prefab>
<Window>
  <!-- ===== ROOT ===== -->
  <Widget Color="#0A0D0BFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9">
    <Children>
      <!-- Background Texture overlay (tactical parchment feel) -->
      <Widget Sprite="TownManagement\menugradient" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" AlphaFactor="0.1" />

      <!-- ===== HEADER ===== -->
      <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="80" WidthSizePolicy="StretchToParent"
                 StackLayout.LayoutMethod="HorizontalLeftToRight" MarginTop="15" MarginLeft="30" MarginRight="30">
        <Children>
          <!-- Tactical Logo Element -->
          <Widget HeightSizePolicy="Fixed" SuggestedHeight="60" WidthSizePolicy="Fixed" SuggestedWidth="60" MarginRight="15" VerticalAlignment="Center">
            <Children>
               <Widget Sprite="SPGeneral\MapArrow" Color="#C7A45AFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" AlphaFactor="0.8" />
               <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#0A0D0BFF" Text="CF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Brush.FontSize="30" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" MarginTop="5"/>
            </Children>
          </Widget>

          <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" Brush.FontSize="60"
                      HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" Text="FORGE" VerticalAlignment="Center" MarginRight="20" />
          <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="50"
                  WidthSizePolicy="Fixed" SuggestedWidth="2" VerticalAlignment="Center" MarginRight="20" AlphaFactor="0.6" />
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren"
                     StackLayout.LayoutMethod="VerticalTopToBottom" VerticalAlignment="Center">
            <Children>
              <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren"
                          WidthSizePolicy="CoverChildren" Text="@Title" HorizontalAlignment="Left" MarginBottom="2" />
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren"
                          WidthSizePolicy="CoverChildren" Text="@WorkbenchLabel" HorizontalAlignment="Left" />
            </Children>
          </ListPanel>
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" />
          
          <!-- Decorative Top Right stats -->
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren"
                     StackLayout.LayoutMethod="VerticalBottomToTop" VerticalAlignment="Center" MarginRight="30" AlphaFactor="0.5">
            <Children>
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" Text="SECURE TERMINAL ACTIVE" HorizontalAlignment="Right"
                          HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" MarginBottom="4" />
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#486151FF" Text="TACTICAL OVERVIEW - ONLINE" HorizontalAlignment="Right"
                          HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" />
            </Children>
          </ListPanel>
          <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren"
                     StackLayout.LayoutMethod="HorizontalLeftToRight" VerticalAlignment="Center">
            <Children>
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren"
                          WidthSizePolicy="CoverChildren" Text="@EnableLabel" VerticalAlignment="Center" MarginRight="25" />
              <!-- CLOSE BUTTON using TacticalButton brush for hover -->
              <ButtonWidget Command.Click="ExecuteClose" DoNotPassEventsToChildren="true"
                            Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="45"
                            WidthSizePolicy="Fixed" SuggestedWidth="130" VerticalAlignment="Center">
                <Children>
                  <TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center"
                              Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent"
                              WidthSizePolicy="StretchToParent" Text="@CloseLabel" />
                </Children>
              </ButtonWidget>
            </Children>
          </ListPanel>
        </Children>
      </ListPanel>

      <!-- ===== TOP DIVIDER ===== -->
      <Widget Sprite="Divider\horizontal_line" Color="#C7A45AFF" HeightSizePolicy="Fixed" SuggestedHeight="6"
              WidthSizePolicy="StretchToParent" MarginLeft="20" MarginRight="20" MarginTop="100" AlphaFactor="0.6" />
      <Widget Sprite="Divider\horizontal_line" HeightSizePolicy="Fixed" SuggestedHeight="6"
              WidthSizePolicy="StretchToParent" MarginLeft="20" MarginRight="20" MarginTop="104" AlphaFactor="0.2" />

      <!-- ===== MAIN 2-COLUMN ===== -->
      <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
              MarginTop="115" MarginBottom="50" MarginLeft="20" MarginRight="20">
        <Children>

          <!-- LEFT SIDEBAR -->
          <Widget Color="#101512FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                  WidthSizePolicy="Fixed" SuggestedWidth="240" HorizontalAlignment="Left">
            <Children>
              <!-- Sidebar header motif -->
              <Widget HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" MarginTop="15">
                <Children>
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#486151FF" Text="— ◇ —" HeightSizePolicy="CoverChildren"
                              WidthSizePolicy="CoverChildren" VerticalAlignment="Center" HorizontalAlignment="Left" MarginLeft="10" />
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren"
                              WidthSizePolicy="CoverChildren" Text="@SectionsLabel" HorizontalAlignment="Center" VerticalAlignment="Center" />
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#486151FF" Text="— ◇ —" HeightSizePolicy="CoverChildren"
                              WidthSizePolicy="CoverChildren" VerticalAlignment="Center" HorizontalAlignment="Right" MarginRight="10" />
                </Children>
              </Widget>

              <!-- NAV BUTTONS using TacticalButton for hover -->
              <ListPanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
                         MarginTop="65" MarginBottom="200" MarginLeft="10" MarginRight="10"
                         StackLayout.LayoutMethod="VerticalTopToBottom">
                <Children>
                  <ButtonWidget Command.Click="ExecuteSummary" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@SummaryLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteModules" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@ModulesLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteLogs" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@LogsLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteInspector" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@InspectorLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteTests" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@TestsLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteMetrics" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@MetricsLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteSnapshots" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@SnapshotsLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteDependencies" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@DependenciesLabel" /></Children></ButtonWidget>
                </Children>
              </ListPanel>

              <!-- Left Sidebar Background Decoration -->
              <Widget Sprite="Encyclopedia\enc_home_bg" HeightSizePolicy="Fixed" SuggestedHeight="240" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" MarginBottom="80" AlphaFactor="0.05" />

              <!-- Watermark -->
              <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
                      MarginTop="500" MarginBottom="80" AlphaFactor="0.1">
                <Children>
                  <TextWidget Brush="SPOptions.Title.Text" Brush.FontSize="120" Text="CF"
                              HorizontalAlignment="Center" VerticalAlignment="Center" Brush.FontColor="#486151FF" />
                </Children>
              </Widget>

              <!-- Export button -->
              <ButtonWidget Command.Click="ExecuteExport" DoNotPassEventsToChildren="true"
                            Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="48"
                            WidthSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10"
                            VerticalAlignment="Bottom" MarginBottom="15">
                <Children>
                  <TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center"
                              Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent"
                              WidthSizePolicy="StretchToParent" Text="@ExportLabel" />
                </Children>
              </ButtonWidget>
            </Children>
          </Widget>

          <!-- VERTICAL DIVIDER -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="6"
                  MarginLeft="250" HorizontalAlignment="Left">
            <Children>
              <Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                      WidthSizePolicy="Fixed" SuggestedWidth="2" HorizontalAlignment="Center" />
              <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                      WidthSizePolicy="Fixed" SuggestedWidth="1" HorizontalAlignment="Left" AlphaFactor="0.4" />
              <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                      WidthSizePolicy="Fixed" SuggestedWidth="1" HorizontalAlignment="Right" AlphaFactor="0.4" />
            </Children>
          </Widget>

          <!-- RIGHT CONTENT: ListPanel -->
          <ListPanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
                     MarginLeft="275" StackLayout.LayoutMethod="VerticalTopToBottom">
            <Children>

              <!-- Section title row -->
              <Widget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginBottom="8">
                <Children>
                  <TextWidget Brush="SPOptions.Title.Text" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren"
                              WidthSizePolicy="CoverChildren" Text="@CurrentSectionLabel" HorizontalAlignment="Left" />
                  <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1"
                          WidthSizePolicy="StretchToParent" MarginLeft="150" VerticalAlignment="Center" AlphaFactor="0.3" />
                </Children>
              </Widget>

              <!-- Help label -->
              <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren"
                          WidthSizePolicy="CoverChildren" Text="@SectionHelpLabel" HorizontalAlignment="Left" MarginBottom="14" />

              <!-- TOOL BUTTONS ROW (fixed 45px height) -->
              <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="45" WidthSizePolicy="StretchToParent"
                         StackLayout.LayoutMethod="HorizontalLeftToRight" MarginBottom="6">
                <Children>
                  <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="StretchToParent"
                              WidthSizePolicy="Fixed" SuggestedWidth="170" Text="@InputLabel" HorizontalAlignment="Left"
                              Brush.TextVerticalAlignment="Center" MarginRight="10" />
                  <!-- Input box -->
                  <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                          WidthSizePolicy="Fixed" SuggestedWidth="310" MarginRight="20" AlphaFactor="0.6">
                    <Children>
                      <Widget Color="#070A08FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                              WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                        <Children>
                          <!-- FIXED: UpdateTextOnTyping="true" so that click events get the updated argument instantly -->
                          <EditableTextWidget Brush="Standard.InputText" Brush.TextVerticalAlignment="Center"
                                             Text="@Argument" UpdateTextOnTyping="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
                                             MarginLeft="10" MarginRight="10" />
                        </Children>
                      </Widget>
                    </Children>
                  </Widget>
                  <!-- Action buttons -->
                  <ButtonWidget Command.Click="ExecuteRefresh" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130" MarginRight="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RefreshLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteRun" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="160" MarginRight="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RunLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecutePin" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130" MarginRight="10"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@PinLabel" /></Children></ButtonWidget>
                  <ButtonWidget Command.Click="ExecuteCompare" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@CompareLabel" /></Children></ButtonWidget>
                </Children>
              </ListPanel>

              <!-- ARGUMENT SUGGESTIONS -->
              <!-- FIXED: Wrapping the suggestion pill properly so it doesn't stretch past the edge and crop -->
              <Widget HeightSizePolicy="Fixed" SuggestedHeight="32" WidthSizePolicy="StretchToParent" MarginBottom="8">
                <Children>
                  <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="170">
                     <Children>
                        <Widget Color="#1A2921FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" AlphaFactor="0.7" />
                        <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" AlphaFactor="0.3" />
                        <!-- Subtle decorative corners for the suggestion bar -->
                        <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="4" WidthSizePolicy="Fixed" SuggestedWidth="1" HorizontalAlignment="Left" VerticalAlignment="Top" AlphaFactor="0.8"/>
                        <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="4" WidthSizePolicy="Fixed" SuggestedWidth="1" HorizontalAlignment="Right" VerticalAlignment="Top" AlphaFactor="0.8"/>
                        
                        <TextWidget Brush="CalradiaForge.Gold" Brush.FontColor="#C7A45AFF" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" Text="@ArgumentSuggestions" HorizontalAlignment="Left" VerticalAlignment="Center" MarginLeft="10" />
                     </Children>
                  </Widget>
                </Children>
              </Widget>

              <!-- CONTENT BOX -->
              <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                      WidthSizePolicy="StretchToParent" AlphaFactor="0.4">
                <Children>
                  <Widget Color="#070A08FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="StretchToParent"
                          WidthSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1">
                    <Children>
                      <!-- Subtle scan lines and large watermark for the main display -->
                      <Widget Sprite="Encyclopedia\enc_home_bg" HeightSizePolicy="Fixed" SuggestedHeight="500" WidthSizePolicy="Fixed" SuggestedWidth="500" HorizontalAlignment="Center" VerticalAlignment="Center" AlphaFactor="0.03" />
                      
                      <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="StretchToParent" MarginTop="100" AlphaFactor="0.1" />
                      <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="StretchToParent" MarginTop="200" AlphaFactor="0.1" />
                      <Widget Color="#486151FF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="StretchToParent" MarginTop="300" AlphaFactor="0.1" />
                      
                      <!-- Corner brackets -->
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="8" WidthSizePolicy="Fixed" SuggestedWidth="2" HorizontalAlignment="Left" VerticalAlignment="Top" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="Fixed" SuggestedWidth="8" HorizontalAlignment="Left" VerticalAlignment="Top" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="8" WidthSizePolicy="Fixed" SuggestedWidth="2" HorizontalAlignment="Right" VerticalAlignment="Top" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="Fixed" SuggestedWidth="8" HorizontalAlignment="Right" VerticalAlignment="Top" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="8" WidthSizePolicy="Fixed" SuggestedWidth="2" HorizontalAlignment="Left" VerticalAlignment="Bottom" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="Fixed" SuggestedWidth="8" HorizontalAlignment="Left" VerticalAlignment="Bottom" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="8" WidthSizePolicy="Fixed" SuggestedWidth="2" HorizontalAlignment="Right" VerticalAlignment="Bottom" />
                      <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="Fixed" SuggestedWidth="8" HorizontalAlignment="Right" VerticalAlignment="Bottom" />
                      
                      <ScrollablePanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent"
                                      MarginLeft="4" MarginRight="4" MarginTop="4" MarginBottom="4"
                                      InnerPanel.WidthSizePolicy="StretchToParent"
                                      InnerPanel.HeightSizePolicy="CoverChildren" AutoHideScrollBars="true">
                        <ScrollablePanel.InnerPanel>
                          <Widget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent">
                            <Children>
                              <TextWidget Brush="GameTip.Text" Brush.FontColor="#D1D5DBFF"
                                          Brush.TextHorizontalAlignment="Left"
                                          Brush.TextVerticalAlignment="Top"
                                          Text="@Content"
                                          HeightSizePolicy="CoverChildren"
                                          WidthSizePolicy="StretchToParent"
                                          MarginLeft="16" MarginRight="16"
                                          MarginTop="16" MarginBottom="16"
                                          ClipContents="false" />
                            </Children>
                          </Widget>
                        </ScrollablePanel.InnerPanel>
                        <ScrollablePanel.VerticalScrollbar>
                            <ScrollbarWidget HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="8" HorizontalAlignment="Right" MarginTop="2" MarginBottom="2" MarginRight="2">
                                <ScrollbarWidget.Handle>
                                    <Widget Color="#C7A45AFF" Sprite="BlankWhiteSquare_9" WidthSizePolicy="Fixed" SuggestedWidth="6" HeightSizePolicy="Fixed" SuggestedHeight="40" HorizontalAlignment="Center" AlphaFactor="0.6"/>
                                </ScrollbarWidget.Handle>
                            </ScrollbarWidget>
                        </ScrollablePanel.VerticalScrollbar>
                      </ScrollablePanel>
                    </Children>
                  </Widget>
                </Children>
              </Widget>

            </Children>
          </ListPanel>

        </Children>
      </Widget>

      <!-- FOOTER -->
      <Widget HeightSizePolicy="Fixed" SuggestedHeight="40" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom">
        <Children>
          <Widget Sprite="Divider\horizontal_line" Color="#C7A45AFF" HeightSizePolicy="Fixed" SuggestedHeight="6"
                  WidthSizePolicy="StretchToParent" VerticalAlignment="Top" AlphaFactor="0.8"
                  MarginLeft="20" MarginRight="20" />
          <TextWidget Brush="GameTip.Text" Brush.FontColor="#9CA3AFFF" HeightSizePolicy="CoverChildren"
                      WidthSizePolicy="StretchToParent" Text="@NavigationLabel" Brush.TextHorizontalAlignment="Center"
                      Brush.TextVerticalAlignment="Bottom" HorizontalAlignment="Center" VerticalAlignment="Bottom" MarginBottom="10" />
        </Children>
      </Widget>

    </Children>
  </Widget>
</Window>
</Prefab>'''

open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(content)
