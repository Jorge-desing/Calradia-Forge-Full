xml_content = '''<Prefab>
<Window>
  <Widget Color="#101512FF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Sprite="BlankWhiteSquare_9">
    <Children>
      
      <!-- Top Header -->
      <Widget HeightSizePolicy="Fixed" SuggestedHeight="80" WidthSizePolicy="StretchToParent" MarginTop="0">
        <Children>
          <Widget HeightSizePolicy="Fixed" SuggestedHeight="60" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="10" MarginRight="30">
            <Children>
              <!-- Left Side: Title and Subtitle -->
              <TextWidget Brush="GameTip.Title.Text" HeightSizePolicy="Fixed" SuggestedHeight="40" Text="@Title" HorizontalAlignment="Left" VerticalAlignment="Top" />
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="Fixed" SuggestedHeight="20" Text="@WorkbenchLabel" HorizontalAlignment="Left" VerticalAlignment="Bottom" MarginTop="40" />

              <!-- Right Side: Version and Close Button -->
              <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="Fixed" SuggestedWidth="400" HorizontalAlignment="Right" VerticalAlignment="Center" StackLayout.LayoutMethod="HorizontalRightToLeft">
                <Children>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteClose" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="Fixed" SuggestedWidth="120" Id="ForgeClose">
                    <Children>
                      <TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@CloseLabel" />
                    </Children>
                  </ButtonWidget>
                  <TextWidget Brush="GameTip.Text" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="250" HorizontalAlignment="Right" VerticalAlignment="Center" Text="@EnableLabel" MarginRight="20" />
                </Children>
              </ListPanel>
            </Children>
          </Widget>
          <!-- Horizontal Divider -->
          <Widget HeightSizePolicy="Fixed" SuggestedHeight="2" WidthSizePolicy="StretchToParent" MarginTop="78" MarginLeft="20" MarginRight="20" Color="#486151FF" Sprite="BlankWhiteSquare_9" />
        </Children>
      </Widget>

      <!-- Main Layout Body -->
      <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="80" MarginBottom="40">
        <Children>
          
          <!-- Left Sidebar -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="260" MarginLeft="20" MarginTop="20" MarginBottom="20">
            <Children>
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="Fixed" SuggestedHeight="25" Text="@SectionsLabel" HorizontalAlignment="Left" MarginLeft="10" />
              
              <ListPanel HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="35" MarginBottom="60" StackLayout.LayoutMethod="VerticalTopToBottom">
                <Children>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteSummary" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@SummaryLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteModules" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@ModulesLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteLogs" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@LogsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteInspector" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@InspectorLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteTests" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@TestsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteMetrics" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@MetricsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteSnapshots" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@SnapshotsLabel" /></Children>
                  </ButtonWidget>
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteDependencies" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="5">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@DependenciesLabel" /></Children>
                  </ButtonWidget>
                </Children>
              </ListPanel>

              <!-- Export Button at the bottom of the sidebar -->
              <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteExport" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="50" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom">
                <Children>
                  <TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@ExportLabel" Color="#C7A45AFF" />
                </Children>
              </ButtonWidget>
            </Children>
          </Widget>

          <!-- Vertical Divider -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="2" MarginLeft="300" MarginTop="20" MarginBottom="20" Color="#486151FF" Sprite="BlankWhiteSquare_9" />

          <!-- Main Content Box -->
          <Widget HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginLeft="320" MarginRight="20" MarginTop="20" MarginBottom="20">
            <Children>
              <TextWidget Brush="GameTip.Title.Text" HeightSizePolicy="Fixed" SuggestedHeight="45" Text="@CurrentSectionLabel" HorizontalAlignment="Left" />
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="Fixed" SuggestedHeight="25" Text="@SectionHelpLabel" MarginTop="45" HorizontalAlignment="Left" />

              <!-- Tool Row: Argument Input & Action Buttons -->
              <ListPanel HeightSizePolicy="Fixed" SuggestedHeight="46" WidthSizePolicy="StretchToParent" MarginTop="80" StackLayout.LayoutMethod="HorizontalLeftToRight">
                <Children>
                  <TextWidget Brush="GameTip.Text" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130" HorizontalAlignment="Left" VerticalAlignment="Center" Text="@InputLabel" />
                  
                  <Widget Color="#0B0F0DFF" WidthSizePolicy="Fixed" SuggestedWidth="300" HeightSizePolicy="Fixed" SuggestedHeight="36" MarginRight="15" Sprite="BlankWhiteSquare_9" VerticalAlignment="Center">
                    <Children>
                      <EditableTextWidget Text="@Argument" Brush="Standard.InputText" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="10" MarginRight="10" MarginTop="5" MarginBottom="5" HorizontalAlignment="Left" VerticalAlignment="Center" />
                    </Children>
                  </Widget>

                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteRefresh" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="Fixed" SuggestedWidth="120" MarginRight="10">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@RefreshLabel" /></Children>
                  </ButtonWidget>
                  
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteRun" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="Fixed" SuggestedWidth="120" MarginRight="10">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@RunLabel" /></Children>
                  </ButtonWidget>
                  
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecutePin" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="Fixed" SuggestedWidth="120" MarginRight="10">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@PinLabel" /></Children>
                  </ButtonWidget>
                  
                  <ButtonWidget Brush="ButtonBrush2" Command.Click="ExecuteCompare" DoNotPassEventsToChildren="true" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="Fixed" SuggestedWidth="120">
                    <Children><TextWidget Brush="GameTip.Text" DoNotAcceptEvents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Text="@CompareLabel" /></Children>
                  </ButtonWidget>
                </Children>
              </ListPanel>
              
              <!-- Suggestions hint below input -->
              <TextWidget Brush="GameTip.Text" HeightSizePolicy="Fixed" SuggestedHeight="20" WidthSizePolicy="StretchToParent" Text="@ArgumentSuggestions" MarginTop="130" MarginLeft="130" HorizontalAlignment="Left" Color="#9CA3AFFF" />

              <!-- Output Text Box -->
              <Widget Color="#486151FF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginTop="160" Sprite="BlankWhiteSquare_9">
                <Children>
                  <Widget Color="#0B0F0DFF" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginBottom="1" MarginLeft="1" MarginRight="1" MarginTop="1" Sprite="BlankWhiteSquare_9">
                    <Children>
                      <!-- Main Scrollable Text Content -->
                      <TextWidget Brush="GameTip.Text" ClipContents="true" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" MarginBottom="14" MarginLeft="18" MarginRight="16" MarginTop="14" Text="@Content" />
                    </Children>
                  </Widget>
                </Children>
              </Widget>
            </Children>
          </Widget>

        </Children>
      </Widget>

      <!-- Footer -->
      <Widget HeightSizePolicy="Fixed" SuggestedHeight="32" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Color="#0B0F0DFF" Sprite="BlankWhiteSquare_9">
        <Children>
          <Widget HeightSizePolicy="Fixed" SuggestedHeight="1" WidthSizePolicy="StretchToParent" Color="#486151FF" Sprite="BlankWhiteSquare_9" VerticalAlignment="Top" />
          <TextWidget Brush="GameTip.Text" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@NavigationLabel" HorizontalAlignment="Center" VerticalAlignment="Center" />
        </Children>
      </Widget>

    </Children>
  </Widget>
</Window>
</Prefab>'''

open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'w', encoding='utf-8').write(xml_content)
print("Gauntlet XML updated.")
