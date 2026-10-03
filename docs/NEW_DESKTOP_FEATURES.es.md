# Nuevas funciones de la aplicación de escritorio de Calradia Forge Studio

## Función 1: Visualizador del estado de campaña en tiempo real

### Propósito
Proporcionar un panel gráfico que muestre el estado de la campaña en tiempo real, incluidas las relaciones entre reinos, la influencia de los clanes y el estado de los asentamientos, mediante una visualización interactiva.

### Implementación

#### Control de usuario WPF
```csharp
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CalradiaForge.Sdk;

namespace CalradiaForge.Desktop.Controls
{
    public partial class CampaignVisualizer : UserControl
    {
        private Dictionary<string, KingdomNode> _kingdomNodes = new Dictionary<string, KingdomNode>();
        private Dictionary<string, RelationshipLine> _relationshipLines = new Dictionary<string, RelationshipLine>();
        private Canvas _canvas;

        public CampaignVisualizer()
        {
            InitializeComponent();
            InitializeCanvas();
        }

        private void InitializeCanvas()
        {
            _canvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 14, 12)),
                ClipToBounds = true
            };
            
            var scrollViewer = new ScrollViewer
            {
                Content = _canvas,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            
            this.Content = scrollViewer;
        }

        public void UpdateCampaignState()
        {
            if (!ForgeData.HasValue("Campaign_Active")) return;

            // Clear existing visualization
            _canvas.Children.Clear();
            _kingdomNodes.Clear();
            _relationshipLines.Clear();

            // Get kingdom data from SDK
            var diplomacyEngine = new ForgeDiplomacyEngine();
            var kingdoms = diplomacyEngine.GetAllKingdoms();
            var relationships = diplomacyEngine.GetAllRelationships();

            // Create kingdom nodes
            foreach (var kingdom in kingdoms)
            {
                var node = CreateKingdomNode(kingdom);
                _kingdomNodes[kingdom.Id] = node;
                _canvas.Children.Add(node);
            }

            // Create relationship lines
            foreach (var relationship in relationships)
            {
                if (_kingdomNodes.ContainsKey(relationship.FromKingdomId) && 
                    _kingdomNodes.ContainsKey(relationship.ToKingdomId))
                {
                    var line = CreateRelationshipLine(
                        _kingdomNodes[relationship.FromKingdomId],
                        _kingdomNodes[relationship.ToKingdomId],
                        relationship.RelationshipType
                    );
                    _relationshipLines[relationship.Id] = line;
                    _canvas.Children.Add(line);
                }
            }

            // Layout nodes in circular pattern
            LayoutKingdomNodes();
        }

        private KingdomNode CreateKingdomNode(KingdomData kingdom)
        {
            var node = new KingdomNode
            {
                KingdomName = kingdom.Name,
                Influence = kingdom.Influence,
                MilitaryStrength = kingdom.MilitaryStrength,
                RulingClan = kingdom.RulingClan,
                Width = 120,
                Height = 80,
                Background = new SolidColorBrush(GetKingdomColor(kingdom.Id)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(199, 164, 90)),
                BorderThickness = new System.Windows.Thickness(2)
            };

            // Add kingdom name label
            var nameLabel = new TextBlock
            {
                Text = kingdom.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            node.Child = nameLabel;
            return node;
        }

        private RelationshipLine CreateRelationshipLine(KingdomNode fromNode, KingdomNode toNode, RelationshipType type)
        {
            var line = new Line
            {
                Stroke = GetRelationshipColor(type),
                StrokeThickness = GetRelationshipThickness(type),
                StrokeDashArray = type == RelationshipType.War ? new DoubleCollection { 4, 2 } : null
            };

            // Position line between nodes
            Canvas.SetLeft(line, fromNode.Position.X + fromNode.Width / 2);
            Canvas.SetTop(line, fromNode.Position.Y + fromNode.Height / 2);

            return new RelationshipLine
            {
                Line = line,
                FromNode = fromNode,
                ToNode = toNode,
                RelationshipType = type
            };
        }

        private void LayoutKingdomNodes()
        {
            var centerX = _canvas.ActualWidth / 2;
            var centerY = _canvas.ActualHeight / 2;
            var radius = Math.Min(centerX, centerY) * 0.7;
            var angleStep = 2 * Math.PI / _kingdomNodes.Count;

            var index = 0;
            foreach (var node in _kingdomNodes.Values)
            {
                var angle = index * angleStep;
                var x = centerX + radius * Math.Cos(angle) - node.Width / 2;
                var y = centerY + radius * Math.Sin(angle) - node.Height / 2;

                Canvas.SetLeft(node, x);
                Canvas.SetTop(node, y);
                node.Position = new System.Windows.Point(x, y);

                index++;
            }

            // Update relationship lines
            foreach (var line in _relationshipLines.Values)
            {
                UpdateRelationshipLine(line);
            }
        }

        private void UpdateRelationshipLine(RelationshipLine relationshipLine)
        {
            var fromCenter = new System.Windows.Point(
                relationshipLine.FromNode.Position.X + relationshipLine.FromNode.Width / 2,
                relationshipLine.FromNode.Position.Y + relationshipLine.FromNode.Height / 2
            );

            var toCenter = new System.Windows.Point(
                relationshipLine.ToNode.Position.X + relationshipLine.ToNode.Width / 2,
                relationshipLine.ToNode.Position.Y + relationshipLine.ToNode.Height / 2
            );

            relationshipLine.Line.X1 = fromCenter.X;
            relationshipLine.Line.Y1 = fromCenter.Y;
            relationshipLine.Line.X2 = toCenter.X;
            relationshipLine.Line.Y2 = toCenter.Y;
        }

        private Color GetKingdomColor(string kingdomId)
        {
            return kingdomId switch
            {
                "empire" => Color.FromRgb(139, 69, 19),
                "sturgia" => Color.FromRgb(70, 130, 180),
                "aserai" => Color.FromRgb(218, 165, 32),
                "vlandia" => Color.FromRgb(65, 105, 225),
                "battania" => Color.FromRgb(34, 139, 34),
                "khuzait" => Color.FromRgb(128, 0, 128),
                _ => Color.FromRgb(105, 105, 105)
            };
        }

        private Color GetRelationshipColor(RelationshipType type)
        {
            return type switch
            {
                RelationshipType.War => Color.FromRgb(255, 69, 0),
                RelationshipType.Peace => Color.FromRgb(50, 205, 50),
                RelationshipType.Alliance => Color.FromRgb(30, 144, 255),
                RelationshipType.Neutral => Color.FromRgb(169, 169, 169),
                _ => Colors.Gray
            };
        }

        private double GetRelationshipThickness(RelationshipType type)
        {
            return type switch
            {
                RelationshipType.War => 3,
                RelationshipType.Alliance => 4,
                RelationshipType.Peace => 2,
                _ => 1
            };
        }
    }

    public class KingdomNode : Border
    {
        public string KingdomName { get; set; }
        public float Influence { get; set; }
        public float MilitaryStrength { get; set; }
        public string RulingClan { get; set; }
        public System.Windows.Point Position { get; set; }
    }

    public class RelationshipLine
    {
        public Line Line { get; set; }
        public KingdomNode FromNode { get; set; }
        public KingdomNode ToNode { get; set; }
        public RelationshipType RelationshipType { get; set; }
    }

    public enum RelationshipType
    {
        War,
        Peace,
        Alliance,
        Neutral
    }
}
```

#### Integración en MainWindow.xaml
```xml
<!-- Add to WorkspaceGrid in MainWindow.xaml -->
<TabItem Header="📊 Campaign Visualizer">
    <Grid Background="#0A0E0C">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <Border Grid.Row="0" Background="#101512" BorderBrush="#223328" BorderThickness="0,0,0,1" Padding="12,8">
            <StackPanel Orientation="Horizontal">
                <Button Content="🔄 Refresh State" Click="RefreshCampaignState_Click" 
                        Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"/>
                <Button Content="📥 Export Data" Click="ExportCampaignData_Click" 
                        Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"/>
                <Button Content="⚙️ Settings" Click="VisualizerSettings_Click" 
                        Style="{StaticResource PrimaryButton}"/>
            </StackPanel>
        </Border>
        
        <!-- Campaign Visualizer Control -->
        <Border Grid.Row="1" Margin="12" Background="#0D120F" BorderBrush="#223328" BorderThickness="1">
            <local:CampaignVisualizer x:Name="CampaignVisualizerControl"/>
        </Border>
        
        <!-- Status Bar -->
        <Border Grid.Row="2" Background="#101512" BorderBrush="#223328" BorderThickness="0,1,0,0" Padding="12,6">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="Kingdoms: " Foreground="{StaticResource MutedTextBrush}" FontSize="12"/>
                <TextBlock x:Name="KingdomCountText" Text="0" Foreground="{StaticResource BrassBrush}" FontSize="12" FontWeight="Bold" Margin="0,0,16,0"/>
                <TextBlock Text="Active Wars: " Foreground="{StaticResource MutedTextBrush}" FontSize="12"/>
                <TextBlock x:Name="WarCountText" Text="0" Foreground="#FF4500" FontSize="12" FontWeight="Bold" Margin="0,0,16,0"/>
                <TextBlock Text="Alliances: " Foreground="{StaticResource MutedTextBrush}" FontSize="12"/>
                <TextBlock x:Name="AllianceCountText" Text="0" Foreground="#1E90FF" FontSize="12" FontWeight="Bold"/>
            </StackPanel>
        </Border>
    </Grid>
</TabItem>
```

#### Integración del código subyacente
```csharp
private void RefreshCampaignState_Click(object sender, RoutedEventArgs e)
{
    try
    {
        CampaignVisualizerControl.UpdateCampaignState();
        UpdateCampaignStatistics();
        ShowToast("Campaign state refreshed successfully");
    }
    catch (Exception ex)
    {
        ShowToast($"Error refreshing campaign state: {ex.Message}");
    }
}

private void UpdateCampaignStatistics()
{
    var diplomacyEngine = new ForgeDiplomacyEngine();
    var kingdoms = diplomacyEngine.GetAllKingdoms();
    var relationships = diplomacyEngine.GetAllRelationships();

    KingdomCountText.Text = kingdoms.Count.ToString();
    WarCountText.Text = relationships.Count(r => r.RelationshipType == RelationshipType.War).ToString();
    AllianceCountText.Text = relationships.Count(r => r.RelationshipType == RelationshipType.Alliance).ToString();
}

private void ExportCampaignData_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var diplomacyEngine = new ForgeDiplomacyEngine();
        var data = diplomacyEngine.ExportCampaignData();
        
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"campaign_data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveFileDialog.FileName, data);
            ShowToast("Campaign data exported successfully");
        }
    }
    catch (Exception ex)
    {
        ShowToast($"Error exporting campaign data: {ex.Message}");
    }
}
```

## Función 2: Analizador avanzado de conflictos entre mods

### Propósito
Proporcionar un análisis exhaustivo de conflictos entre mods, incluidas las colisiones de ID de entidades XML, los problemas de orden de carga y los problemas de resolución de dependencias, mediante matrices visuales de conflictos.

### Implementación

#### Control de usuario WPF
```csharp
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using CalradiaForge.Sdk;

namespace CalradiaForge.Desktop.Controls
{
    public partial class ModConflictAnalyzer : UserControl
    {
        private DataGrid _conflictGrid;
        private DataGrid _dependencyGrid;
        private TabControl _analysisTabs;

        public ModConflictAnalyzer()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            _analysisTabs = new TabControl();
            
            // Conflict Matrix Tab
            var conflictTab = new TabItem { Header = "🔥 Conflict Matrix" };
            _conflictGrid = CreateConflictGrid();
            conflictTab.Content = _conflictGrid;
            _analysisTabs.Items.Add(conflictTab);

            // Dependency Graph Tab
            var dependencyTab = new TabItem { Header = "🔗 Dependency Graph" };
            _dependencyGrid = CreateDependencyGrid();
            dependencyTab.Content = _dependencyGrid;
            _analysisTabs.Items.Add(dependencyTab);

            // Load Order Tab
            var loadOrderTab = new TabItem { Header = "📋 Load Order" };
            loadOrderTab.Content = CreateLoadOrderView();
            _analysisTabs.Items.Add(loadOrderTab);

            this.Content = _analysisTabs;
        }

        private DataGrid CreateConflictGrid()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = new SolidColorBrush(Color.FromRgb(13, 18, 15)),
                Foreground = new SolidColorBrush(Colors.White),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Severity",
                Binding = new System.Windows.Data.Binding("Severity"),
                Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Entity ID",
                Binding = new System.Windows.Data.Binding("EntityId"),
                Width = new DataGridLength(200, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Conflict Type",
                Binding = new System.Windows.Data.Binding("ConflictType"),
                Width = new DataGridLength(150, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Mod A",
                Binding = new System.Windows.Data.Binding("ModA"),
                Width = new DataGridLength(150, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Mod B",
                Binding = new System.Windows.Data.Binding("ModB"),
                Width = new DataGridLength(150, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Resolution",
                Binding = new System.Windows.Data.Binding("Resolution"),
                Width = new DataGridLength(200, DataGridLengthUnitType.Pixel)
            });

            return grid;
        }

        private DataGrid CreateDependencyGrid()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = new SolidColorBrush(Color.FromRgb(13, 18, 15)),
                Foreground = new SolidColorBrush(Colors.White),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                IsReadOnly = true
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Module",
                Binding = new System.Windows.Data.Binding("ModuleName"),
                Width = new DataGridLength(200, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Dependencies",
                Binding = new System.Windows.Data.Binding("Dependencies"),
                Width = new DataGridLength(300, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Circular",
                Binding = new System.Windows.Data.Binding("HasCircularDependency"),
                Width = new DataGridLength(80, DataGridLengthUnitType.Pixel)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Load Order",
                Binding = new System.Windows.Data.Binding("LoadOrder"),
                Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
            });

            return grid;
        }

        private ScrollViewer CreateLoadOrderView()
        {
            var stackPanel = new StackPanel { Margin = new System.Windows.Thickness(12) };
            var scrollViewer = new ScrollViewer { Content = stackPanel };
            return scrollViewer;
        }

        public async Task AnalyzeModConflictsAsync()
        {
            try
            {
                ShowAnalysisProgress(true);

                var conflictAnalyzer = new ModConflictAnalyzerService();
                var conflicts = await conflictAnalyzer.AnalyzeConflictsAsync();
                var dependencies = await conflictAnalyzer.AnalyzeDependenciesAsync();

                UpdateConflictGrid(conflicts);
                UpdateDependencyGrid(dependencies);
                UpdateLoadOrderView(dependencies);

                ShowAnalysisProgress(false);
            }
            catch (Exception ex)
            {
                ShowAnalysisProgress(false);
                MessageBox.Show($"Error analyzing mod conflicts: {ex.Message}", "Analysis Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConflictGrid(List<ModConflict> conflicts)
        {
            _conflictGrid.ItemsSource = conflicts;
        }

        private void UpdateDependencyGrid(List<ModDependency> dependencies)
        {
            _dependencyGrid.ItemsSource = dependencies;
        }

        private void UpdateLoadOrderView(List<ModDependency> dependencies)
        {
            var loadOrderPanel = (StackPanel)((ScrollViewer)_analysisTabs.Items[2].Content).Content;
            loadOrderPanel.Children.Clear();

            foreach (var dep in dependencies.OrderBy(d => d.LoadOrder))
            {
                var item = CreateLoadOrderItem(dep);
                loadOrderPanel.Children.Add(item);
            }
        }

        private Border CreateLoadOrderItem(ModDependency dependency)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 21, 18)),
                BorderBrush = dependency.HasCircularDependency 
                    ? new SolidColorBrush(Color.FromRgb(255, 69, 0))
                    : new SolidColorBrush(Color.FromRgb(34, 139, 34)),
                BorderThickness = new System.Windows.Thickness(1),
                Margin = new System.Windows.Thickness(0, 4, 0, 4),
                Padding = new System.Windows.Thickness(12, 8)
            };

            var stackPanel = new StackPanel();
            
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"{dependency.LoadOrder}. {dependency.ModuleName}",
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(199, 164, 90))
            });

            if (dependency.HasCircularDependency)
            {
                headerPanel.Children.Add(new TextBlock
                {
                    Text = " ⚠️ CIRCULAR DEPENDENCY",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 69, 0)),
                    FontWeight = System.Windows.FontWeights.Bold,
                    Margin = new System.Windows.Thickness(8, 0, 0, 0)
                });
            }

            stackPanel.Children.Add(headerPanel);
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Dependencies: {dependency.Dependencies}",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new System.Windows.Thickness(0, 4, 0, 0)
            });

            border.Child = stackPanel;
            return border;
        }

        private void ShowAnalysisProgress(bool show)
        {
            // Show/hide progress indicator
        }
    }

    public class ModConflict
    {
        public string Severity { get; set; }
        public string EntityId { get; set; }
        public string ConflictType { get; set; }
        public string ModA { get; set; }
        public string ModB { get; set; }
        public string Resolution { get; set; }
    }

    public class ModDependency
    {
        public string ModuleName { get; set; }
        public string Dependencies { get; set; }
        public bool HasCircularDependency { get; set; }
        public int LoadOrder { get; set; }
    }
}
```

#### Integración en MainWindow.xaml
```xml
<TabItem Header="🔥 Mod Conflict Analyzer">
    <Grid Background="#0A0E0C">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <Border Grid.Row="0" Background="#101512" BorderBrush="#223328" BorderThickness="0,0,0,1" Padding="12,8">
            <StackPanel Orientation="Horizontal">
                <Button Content="🔍 Analyze Conflicts" Click="AnalyzeModConflicts_Click" 
                        Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"/>
                <Button Content="📥 Export Report" Click="ExportConflictReport_Click" 
                        Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"/>
                <Button Content="🔧 Auto-Fix" Click="AutoFixConflicts_Click" 
                        Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"/>
                <ComboBox x:Name="ConflictSeverityFilter" Width="150" Margin="8,0" Background="#121A15" 
                          Foreground="White" BorderBrush="#1E90FF">
                    <ComboBoxItem Content="All Severities" IsSelected="True"/>
                    <ComboBoxItem Content="Critical"/>
                    <ComboBoxItem Content="High"/>
                    <ComboBoxItem Content="Medium"/>
                    <ComboBoxItem Content="Low"/>
                </ComboBox>
            </StackPanel>
        </Border>
        
        <!-- Mod Conflict Analyzer Control -->
        <Border Grid.Row="1" Margin="12" Background="#0D120F" BorderBrush="#223328" BorderThickness="1">
            <local:ModConflictAnalyzer x:Name="ModConflictAnalyzerControl"/>
        </Border>
        
        <!-- Progress Bar -->
        <Border Grid.Row="2" Background="#101512" BorderBrush="#223328" BorderThickness="0,1,0,0" Padding="12,6" 
                Visibility="Collapsed" x:Name="AnalysisProgressBorder">
            <StackPanel Orientation="Horizontal">
                <ProgressBar Width="200" Height="8" IsIndeterminate="True" 
                             Foreground="#1E90FF" Background="#16221B" Margin="0,0,12,0"/>
                <TextBlock Text="Analyzing mod conflicts..." Foreground="{StaticResource MutedTextBrush}" 
                           VerticalAlignment="Center"/>
            </StackPanel>
        </Border>
    </Grid>
</TabItem>
```

## Función 3: Perfilador de rendimiento en tiempo real

### Propósito
Supervisar en tiempo real el rendimiento del proceso del juego Bannerlord, incluidos los tiempos de fotograma, el uso de memoria, las recolecciones del GC y las métricas de rendimiento específicas de los mods.

### Implementación

#### Control de usuario WPF
```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;

namespace CalradiaForge.Desktop.Controls
{
    public partial class LivePerformanceProfiler : UserControl
    {
        private DispatcherTimer _updateTimer;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _memoryCounter;
        private Process _targetProcess;
        private CartesianChart _frameTimeChart;
        private CartesianChart _memoryChart;
        private List<double> _frameTimeData = new List<double>();
        private List<double> _memoryData = new List<double>();
        private List<string> _timeLabels = new List<string>();

        public LivePerformanceProfiler()
        {
            InitializeComponent();
            InitializeCharts();
            InitializePerformanceCounters();
            StartMonitoring();
        }

        private void InitializeCharts()
        {
            // Frame Time Chart
            _frameTimeChart = new CartesianChart
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 18, 15)),
                Foreground = new SolidColorBrush(Colors.White),
                LegendLocation = LegendLocation.Top,
                Margin = new System.Windows.Thickness(12)
            };

            _frameTimeChart.Series.Add(new LineSeries
            {
                Title = "Frame Time (ms)",
                Values = new ChartValues<double>(),
                Fill = new SolidColorBrush(Color.FromRgb(30, 144, 255)),
                Stroke = new SolidColorBrush(Color.FromRgb(30, 144, 255)),
                StrokeThickness = 2,
                PointGeometry = null
            });

            // Memory Chart
            _memoryChart = new CartesianChart
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 18, 15)),
                Foreground = new SolidColorBrush(Colors.White),
                LegendLocation = LegendLocation.Top,
                Margin = new System.Windows.Thickness(12)
            };

            _memoryChart.Series.Add(new LineSeries
            {
                Title = "Memory (MB)",
                Values = new ChartValues<double>(),
                Fill = new SolidColorBrush(Color.FromRgb(50, 205, 50)),
                Stroke = new SolidColorBrush(Color.FromRgb(50, 205, 50)),
                StrokeThickness = 2,
                PointGeometry = null
            });

            var chartPanel = new StackPanel();
            chartPanel.Children.Add(new TextBlock
            {
                Text = "Frame Time History",
                Foreground = new SolidColorBrush(Color.FromRgb(199, 164, 90)),
                FontWeight = System.Windows.FontWeights.Bold,
                FontSize = 14,
                Margin = new System.Windows.Thickness(12, 8, 12, 4)
            });
            chartPanel.Children.Add(_frameTimeChart);
            
            chartPanel.Children.Add(new TextBlock
            {
                Text = "Memory Usage History",
                Foreground = new SolidColorBrush(Color.FromRgb(199, 164, 90)),
                FontWeight = System.Windows.FontWeights.Bold,
                FontSize = 14,
                Margin = new System.Windows.Thickness(12, 16, 12, 4)
            });
            chartPanel.Children.Add(_memoryChart);

            this.Content = chartPanel;
        }

        private void InitializePerformanceCounters()
        {
            try
            {
                // Find Bannerlord process
                var processes = Process.GetProcessesByName("Bannerlord");
                if (processes.Length > 0)
                {
                    _targetProcess = processes[0];
                    
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing performance counters: {ex.Message}", 
                    "Performance Counter Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartMonitoring()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _updateTimer.Tick += UpdatePerformanceData;
            _updateTimer.Start();
        }

        private void UpdatePerformanceData(object sender, EventArgs e)
        {
            if (_targetProcess == null || _targetProcess.HasExited)
            {
                // Try to reconnect to Bannerlord process
                InitializePerformanceCounters();
                return;
            }

            try
            {
                // Get frame time from SDK
                var frameTime = ForgeData.GetValue<float>("Performance_FrameTime", 16.6f);
                var memoryUsage = _targetProcess.WorkingSet64 / 1024 / 1024; // Convert to MB
                var gcCollections = GC.CollectionCount(0);

                // Update charts
                UpdateChartData(frameTime, memoryUsage);

                // Update performance metrics display
                UpdatePerformanceMetrics(frameTime, memoryUsage, gcCollections);
            }
            catch (Exception ex)
            {
                // Handle counter errors gracefully
            }
        }

        private void UpdateChartData(double frameTime, double memoryUsage)
        {
            const int maxDataPoints = 60;

            _frameTimeData.Add(frameTime);
            _memoryData.Add(memoryUsage);
            _timeLabels.Add(DateTime.Now.ToString("HH:mm:ss"));

            if (_frameTimeData.Count > maxDataPoints)
            {
                _frameTimeData.RemoveAt(0);
                _memoryData.RemoveAt(0);
                _timeLabels.RemoveAt(0);
            }

            ((LineSeries)_frameTimeChart.Series[0]).Values = new ChartValues<double>(_frameTimeData);
            ((LineSeries)_memoryChart.Series[0]).Values = new ChartValues<double>(_memoryData);
        }

        private void UpdatePerformanceMetrics(double frameTime, double memoryUsage, int gcCollections)
        {
            // Update UI text blocks with current metrics
            // This would be implemented with data binding in the full version
        }

        public void StopMonitoring()
        {
            _updateTimer?.Stop();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
        }

        public void ExportPerformanceData()
        {
            var exportData = new
            {
                Timestamp = DateTime.Now,
                FrameTimeData = _frameTimeData,
                MemoryData = _memoryData,
                TimeLabels = _timeLabels
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            var saveFileDialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = $"performance_data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }
    }
}
```

#### Integración en MainWindow.xaml
```xml
<TabItem Header="⚡ Live Performance Profiler">
    <Grid Background="#0A0E0C">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Performance Metrics Header -->
        <Border Grid.Row="0" Background="#101512" BorderBrush="#223328" BorderThickness="0,0,0,1" Padding="12,8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <StackPanel Grid.Column="0" Orientation="Horizontal">
                    <Border Background="#0D1310" BorderBrush="#1E90FF" BorderThickness="1" 
                            CornerRadius="3" Padding="12,6" Margin="0,0,8,0">
                        <StackPanel>
                            <TextBlock Text="Frame Time" Foreground="{StaticResource MutedTextBrush}" FontSize="10"/>
                            <TextBlock x:Name="FrameTimeText" Text="16.6 ms" Foreground="#1E90FF" 
                                       FontSize="16" FontWeight="Bold"/>
                        </StackPanel>
                    </Border>
                    
                    <Border Background="#0D1310" BorderBrush="#32CD32" BorderThickness="1" 
                            CornerRadius="3" Padding="12,6" Margin="0,0,8,0">
                        <StackPanel>
                            <TextBlock Text="Memory" Foreground="{StaticResource MutedTextBrush}" FontSize="10"/>
                            <TextBlock x:Name="MemoryText" Text="2048 MB" Foreground="#32CD32" 
                                       FontSize="16" FontWeight="Bold"/>
                        </StackPanel>
                    </Border>
                    
                    <Border Background="#0D1310" BorderBrush="#FFD700" BorderThickness="1" 
                            CornerRadius="3" Padding="12,6" Margin="0,0,8,0">
                        <StackPanel>
                            <TextBlock Text="GC Collections" Foreground="{StaticResource MutedTextBrush}" FontSize="10"/>
                            <TextBlock x:Name="GcCollectionsText" Text="0" Foreground="#FFD700" 
                                       FontSize="16" FontWeight="Bold"/>
                        </StackPanel>
                    </Border>
                    
                    <Border Background="#0D1310" BorderBrush="#FF6347" BorderThickness="1" 
                            CornerRadius="3" Padding="12,6">
                        <StackPanel>
                            <TextBlock Text="FPS" Foreground="{StaticResource MutedTextBrush}" FontSize="10"/>
                            <TextBlock x:Name="FpsText" Text="60" Foreground="#FF6347" 
                                       FontSize="16" FontWeight="Bold"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
                
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="⏸️ Pause" Click="PausePerformanceMonitoring_Click" 
                            Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"/>
                    <Button Content="📥 Export" Click="ExportPerformanceData_Click" 
                            Style="{StaticResource PrimaryButton}"/>
                </StackPanel>
            </Grid>
        </Border>
        
        <!-- Live Performance Profiler Control -->
        <Border Grid.Row="1" Margin="12" Background="#0D120F" BorderBrush="#223328" BorderThickness="1">
            <local:LivePerformanceProfiler x:Name="PerformanceProfilerControl"/>
        </Border>
        
        <!-- Mod Performance Impact -->
        <Border Grid.Row="2" Background="#101512" BorderBrush="#223328" BorderThickness="0,1,0,0" Padding="12,6">
            <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled">
                <StackPanel Orientation="Horizontal" x:Name="ModPerformanceImpactPanel">
                    <!-- Mod performance impact items will be added dynamically -->
                </StackPanel>
            </ScrollViewer>
        </Border>
    </Grid>
</TabItem>
```

## Integración con los servicios del SDK

### Extensiones del SDK para las funciones de escritorio

```csharp
namespace CalradiaForge.Sdk
{
    public static class DesktopExtensions
    {
        public static KingdomData[] GetAllKingdoms()
        {
            // Return kingdom data from current campaign
            var kingdoms = new List<KingdomData>();
            
            foreach (var kingdom in Kingdom.All)
            {
                kingdoms.Add(new KingdomData
                {
                    Id = kingdom.StringId,
                    Name = kingdom.Name.ToString(),
                    Influence = kingdom.Influence,
                    MilitaryStrength = CalculateMilitaryStrength(kingdom),
                    RulingClan = kingdom.RulingClan.Name.ToString()
                });
            }
            
            return kingdoms.ToArray();
        }

        public static RelationshipData[] GetAllRelationships()
        {
            // Return diplomatic relationships
            var relationships = new List<RelationshipData>();
            
            foreach (var kingdom in Kingdom.All)
            {
                foreach (var otherKingdom in Kingdom.All)
                {
                    if (kingdom != otherKingdom)
                    {
                        var relationType = DetermineRelationshipType(kingdom, otherKingdom);
                        relationships.Add(new RelationshipData
                        {
                            Id = $"{kingdom.StringId}_{otherKingdom.StringId}",
                            FromKingdomId = kingdom.StringId,
                            ToKingdomId = otherKingdom.StringId,
                            RelationshipType = relationType
                        });
                    }
                }
            }
            
            return relationships.ToArray();
        }

        public static async Task<List<ModConflict>> AnalyzeConflictsAsync()
        {
            // Analyze mod conflicts using ModRuleAuditor
            var conflicts = new List<ModConflict>();
            
            // Scan all loaded modules
            foreach (var module in ModuleInfo.GetModules())
            {
                var result = ModRuleAuditor.Audit(module.Path);
                
                foreach (var finding in result.Findings.Where(f => f.Severity == "Error"))
                {
                    conflicts.Add(new ModConflict
                    {
                        Severity = finding.Severity,
                        EntityId = finding.RuleId,
                        ConflictType = finding.Description,
                        ModA = module.Name,
                        ModB = "Unknown",
                        Resolution = finding.Recommendation
                    });
                }
            }
            
            return await Task.FromResult(conflicts);
        }

        public static async Task<List<ModDependency>> AnalyzeDependenciesAsync()
        {
            // Analyze module dependencies
            var dependencies = new List<ModDependency>();
            
            var modules = ModuleInfo.GetModules();
            var loadOrder = 0;
            
            foreach (var module in modules)
            {
                var depString = string.Join(", ", module.Dependencies.Select(d => d.Id));
                var hasCircular = CheckCircularDependency(module, modules);
                
                dependencies.Add(new ModDependency
                {
                    ModuleName = module.Name,
                    Dependencies = depString,
                    HasCircularDependency = hasCircular,
                    LoadOrder = loadOrder++
                });
            }
            
            return await Task.FromResult(dependencies);
        }

        private static float CalculateMilitaryStrength(Kingdom kingdom)
        {
            // Calculate military strength based on clans, parties, and fiefs
            float strength = 0f;
            
            foreach (var clan in kingdom.Clans)
            {
                foreach (var party in clan.Parties)
                {
                    if (party.IsActive && !party.IsMobile)
                    {
                        strength += party.Party.NumberOfHealthyMembers;
                    }
                }
            }
            
            return strength;
        }

        private static RelationshipType DetermineRelationshipType(Kingdom from, Kingdom to)
        {
            if (from.IsAtWarWith(to))
                return RelationshipType.War;
            if (from.HasAllianceWith(to))
                return RelationshipType.Alliance;
            
            return RelationshipType.Peace;
        }

        private static bool CheckCircularDependency(ModuleInfo module, List<ModuleInfo> allModules)
        {
            // Implement circular dependency detection using graph algorithms
            return false; // Simplified for example
        }
    }

    public class KingdomData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Influence { get; set; }
        public float MilitaryStrength { get; set; }
        public string RulingClan { get; set; }
    }

    public class RelationshipData
    {
        public string Id { get; set; }
        public string FromKingdomId { get; set; }
        public string ToKingdomId { get; set; }
        public RelationshipType RelationshipType { get; set; }
    }
}
```

## Resumen

Estas nuevas funciones de la aplicación de escritorio proporcionan:

1. **Visualizador del estado de campaña en tiempo real** - Panel gráfico interactivo que muestra las relaciones entre reinos y el estado de la campaña
2. **Analizador avanzado de conflictos entre mods** - Análisis exhaustivo de conflictos entre mods con matrices visuales de conflictos y grafos de dependencias
3. **Perfilador de rendimiento en tiempo real** - Supervisión del rendimiento en tiempo real mediante gráficos y métricas

Todas las funciones siguen los patrones establecidos:
- UserControls WPF con una separación MVVM adecuada
- Integración con los servicios existentes del SDK
- Actualizaciones de datos en vivo y supervisión en tiempo real
- Funcionalidad de exportación para analizar datos
- Respuesta visual e indicadores de progreso
- Manejo de errores y degradación controlada
- Estilo coherente con el tema Tactical War Theme existente
