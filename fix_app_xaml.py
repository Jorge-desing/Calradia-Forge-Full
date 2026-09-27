with open(r'src\CalradiaForge.Desktop\App.xaml', 'r', encoding='utf-8') as f:
    c = f.read()

correct_block = """          </ControlTemplate.Triggers>
         </ControlTemplate>
        </ToggleButton.Template>
       </ToggleButton>
       
       <Border x:Name="Bd" Grid.Column="1" Background="{TemplateBinding Background}" BorderBrush="Transparent" BorderThickness="3,0,0,0" Padding="{TemplateBinding Padding}" SnapsToDevicePixels="true">
        <ContentPresenter x:Name="PART_Header" ContentSource="Header" HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" SnapsToDevicePixels="{TemplateBinding SnapsToDevicePixels}"/>
       </Border>
       <ItemsPresenter x:Name="ItemsHost" Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="2"/>
      </Grid>
      <ControlTemplate.Triggers>
       <Trigger Property="IsExpanded" Value="false">
        <Setter TargetName="ItemsHost" Property="Visibility" Value="Collapsed"/>
       </Trigger>
       <Trigger Property="HasItems" Value="false">
        <Setter TargetName="Expander" Property="Visibility" Value="Hidden"/>
       </Trigger>
       <Trigger SourceName="Bd" Property="IsMouseOver" Value="True">
        <Setter TargetName="Bd" Property="Background" Value="#18261E"/>
        <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource VerdigrisBrush}"/>
       </Trigger>
       <Trigger Property="IsSelected" Value="true">
        <Setter TargetName="Bd" Property="Background" Value="#1C2B22"/>
        <Setter Property="Foreground" Value="{StaticResource BrassBrush}"/>
        <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource BrassBrush}"/>
        <Setter Property="FontWeight" Value="Bold"/>
       </Trigger>
       <MultiTrigger>
        <MultiTrigger.Conditions>
         <Condition Property="IsSelected" Value="true"/>
         <Condition SourceName="Bd" Property="IsMouseOver" Value="true"/>
        </MultiTrigger.Conditions>
        <Setter TargetName="Bd" Property="Background" Value="#22382D"/>
        <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource BrassBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource BrassBrush}"/>
       </MultiTrigger>
       <MultiTrigger>
        <MultiTrigger.Conditions>
         <Condition Property="IsSelected" Value="true"/>
         <Condition Property="IsSelectionActive" Value="false"/>
        </MultiTrigger.Conditions>
        <Setter TargetName="Bd" Property="Background" Value="#101512"/>
        <Setter Property="Foreground" Value="{StaticResource QuietBrassBrush}"/>
       </MultiTrigger>
      </ControlTemplate.Triggers>
     </ControlTemplate>
    </Setter.Value>
   </Setter>
  </Style>
"""

idx = c.find('<Setter TargetName="ExpandPath" Property="Stroke" Value="{StaticResource FocusBrush}"/>')
if idx != -1:
    end_tag = '</Trigger>'
    idx2 = c.find(end_tag, idx) + len(end_tag)
    scroll_idx = c.find('  <Style TargetType="{x:Type ScrollBar}">')
    c = c[:idx2] + '\n' + correct_block + c[scroll_idx:]
    with open(r'src\CalradiaForge.Desktop\App.xaml', 'w', encoding='utf-8') as f:
        f.write(c)
    print('Successfully restored and updated TreeViewItem style in App.xaml!')
else:
    print('Pattern not found')
