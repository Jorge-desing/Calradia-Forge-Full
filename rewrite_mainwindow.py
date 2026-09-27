import re

f = 'src/CalradiaForge.Desktop/MainWindow.xaml'
content = open(f, encoding='utf-8').read()

grid_start = r'<Grid Grid.Column="2">'
grid_replacement = '''<ScrollViewer Grid.Column="2" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" Padding="0,0,14,0">
   <Grid>
    <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>'''
content = content.replace(grid_start + '\n    <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>', grid_replacement)

content = re.sub(r'<ScrollViewer x:Name="ActionScroller" Grid.Row="2" [^>]+>', '<Grid Grid.Row="2" Margin="0,0,0,10">', content)
content = re.sub(r'</ScrollViewer>\s*<Border Grid.Row="3"', '</Grid>\n    <Border Grid.Row="3"', content)

content = re.sub(r'   </Grid>\n  </Grid>\n </Grid>', '   </Grid>\n  </ScrollViewer>\n  </Grid>\n </Grid>', content)

old_motif = r'<Canvas Width="300" Height="14" HorizontalAlignment="Left" Margin="0,0,0,4">.*?</Canvas>'
new_motif = '''<Grid Margin="0,4,0,8" Height="20">
      <Grid.ColumnDefinitions>
       <ColumnDefinition Width="Auto"/>
       <ColumnDefinition Width="*"/>
       <ColumnDefinition Width="Auto"/>
       <ColumnDefinition Width="*"/>
       <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Path Grid.Column="0" Data="M 0,10 L 6,4 L 12,10 L 6,16 Z M 4,10 L 8,10" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.5" Fill="{StaticResource CoalBrush}" VerticalAlignment="Center"/>
      <Rectangle Grid.Column="1" Height="2" Margin="8,0" VerticalAlignment="Center" Opacity="0.5">
       <Rectangle.Fill>
        <VisualBrush TileMode="Tile" Viewport="0,0,16,2" ViewportUnits="Absolute">
         <VisualBrush.Visual>
          <Canvas Width="16" Height="2">
           <Line X1="0" Y1="1" X2="14" Y2="1" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.5"/>
           <Line X1="14" Y1="0" X2="14" Y2="2" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.5"/>
          </Canvas>
         </VisualBrush.Visual>
        </VisualBrush>
       </Rectangle.Fill>
      </Rectangle>
      <Grid Grid.Column="2" VerticalAlignment="Center">
       <Path Data="M 15,0 L 30,10 L 15,20 L 0,10 Z M 25,0 L 40,10 L 25,20 L 10,10 Z" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.2" Fill="#101512"/>
       <Path Data="M 15,10 L 25,10 M 20,5 L 20,15" Stroke="{StaticResource BrassBrush}" StrokeThickness="1"/>
      </Grid>
      <Rectangle Grid.Column="3" Height="2" Margin="8,0" VerticalAlignment="Center" Opacity="0.5">
       <Rectangle.Fill>
        <VisualBrush TileMode="Tile" Viewport="0,0,16,2" ViewportUnits="Absolute">
         <VisualBrush.Visual>
          <Canvas Width="16" Height="2">
           <Line X1="0" Y1="1" X2="14" Y2="1" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.5"/>
           <Line X1="14" Y1="0" X2="14" Y2="2" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.5"/>
          </Canvas>
         </VisualBrush.Visual>
        </VisualBrush>
       </Rectangle.Fill>
      </Rectangle>
      <Path Grid.Column="4" Data="M 0,10 L 6,4 L 12,10 L 6,16 Z M 4,10 L 8,10" Stroke="{StaticResource BrassBrush}" StrokeThickness="1.5" Fill="{StaticResource CoalBrush}" VerticalAlignment="Center"/>
     </Grid>'''
content = re.sub(old_motif, new_motif, content, flags=re.DOTALL)

open(f, 'w', encoding='utf-8').write(content)
print('Done WPF')
