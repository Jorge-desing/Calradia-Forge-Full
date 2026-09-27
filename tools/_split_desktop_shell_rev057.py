from __future__ import annotations

import re
from pathlib import Path
from xml.dom import Node, minidom


ROOT = Path(__file__).resolve().parents[1]
MAIN_WINDOW = ROOT / "src/CalradiaForge.Desktop/MainWindow.xaml"
PRESENTATION = ROOT / "src/CalradiaForge.Desktop/Presentation"
VIEW_RESOURCES = ROOT / "src/CalradiaForge.Desktop/Resources/Views"
WPF_NS = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"


def element_children(node):
    return [child for child in node.childNodes if child.nodeType == Node.ELEMENT_NODE]


def descendants(node, name=None):
    for child in element_children(node):
        if name is None or child.localName == name:
            yield child
        yield from descendants(child, name)


def find(node, predicate):
    if node.nodeType == Node.ELEMENT_NODE and predicate(node):
        return node
    for child in element_children(node):
        result = find(child, predicate)
        if result is not None:
            return result
    return None


def required(node, predicate, label):
    result = find(node, predicate)
    if result is None:
        raise RuntimeError(f"Could not find {label}; leaving source unchanged.")
    return result


def xaml(node):
    return node.toxml()


def set_attr(node, name, value):
    node.setAttribute(name, value)


def remove_attr(node, name):
    if node.hasAttribute(name):
        node.removeAttribute(name)


def create_xaml(document, name, attributes=None, text=None):
    node = document.createElementNS(WPF_NS, name)
    for key, value in (attributes or {}).items():
        node.setAttribute(key, value)
    if text is not None:
        node.appendChild(document.createTextNode(text))
    return node


def wrap_user_control(class_name, content):
    return f'''<UserControl x:Class="CalradiaForge.Desktop.Presentation.{class_name}"
 xmlns="{WPF_NS}"
 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
 xmlns:services="clr-namespace:CalradiaForge.Desktop.Services"
 xmlns:presentation="clr-namespace:CalradiaForge.Desktop.Presentation"
 xmlns:shell="clr-namespace:System.Windows.Shell;assembly=PresentationFramework"
 xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
 MinWidth="0" MinHeight="0">
{content}
</UserControl>
'''


def replace_main_region(text, opening, replacement):
    start = text.find(opening)
    if start < 0:
        raise RuntimeError(f"Could not find expected main-window marker {opening!r}.")
    if opening == "<Window.Resources>":
        end_tag = "</Window.Resources>"
        end = text.find(end_tag, start)
        if end < 0:
            raise RuntimeError("Could not find Window.Resources closing tag.")
        return text[:start] + replacement + text[end + len(end_tag):]

    tag_name = opening[1:].split(None, 1)[0].rstrip(">").rstrip("/")
    token = re.compile(r"</?" + re.escape(tag_name) + r"(?=[\s>/])[^>]*>")
    depth = 0
    for match in token.finditer(text, start):
        value = match.group(0)
        if value.startswith("</"):
            depth -= 1
            if depth == 0:
                return text[:start] + replacement + text[match.end():]
        elif not value.endswith("/>"):
            depth += 1
    raise RuntimeError(f"Could not locate matching close tag for {opening!r}.")


def main():
    original = MAIN_WINDOW.read_text(encoding="utf-8-sig")
    document = minidom.parseString(original)
    window = document.documentElement

    resources = required(window, lambda item: item.nodeName == "Window.Resources", "Window.Resources")
    page = required(
        resources,
        lambda item: item.localName == "DataTemplate" and
        item.getAttribute("DataType") == "{x:Type presentation:ToolPageViewModel}",
        "ToolPageViewModel DataTemplate",
    )
    body = required(
        window,
        lambda item: item.localName == "Grid" and item.getAttribute("Grid.Row") == "1" and
        item.getAttribute("Margin") == "14,8,14,12",
        "MainWindow body grid",
    )

    direct_body = element_children(body)
    header = next((item for item in direct_body if item.localName == "Border" and item.getAttribute("Grid.Row") == "0"), None)
    status = next((item for item in direct_body if item.localName == "Border" and item.getAttribute("AutomationProperties.AutomationId") == "WorkOrderCards"), None)
    workspace = next((item for item in direct_body if item.localName == "Grid" and item.getAttribute("Grid.Row") == "2"), None)
    footer = next((item for item in direct_body if item.localName == "TextBlock" and item.getAttribute("Grid.Row") == "3"), None)
    palette = next((item for item in direct_body if item.localName == "Border" and item.getAttribute("AutomationProperties.AutomationId") == "CommandPaletteOverlay"), None)
    if any(item is None for item in (header, status, workspace, footer, palette)):
        raise RuntimeError("Could not resolve all direct shell sections; source left unchanged.")

    # Move the command and hotkey dossier out of every route page into a single
    # optional shell drawer. Its existing copy handler is moved with the view.
    dossier = required(page, lambda item: item.getAttribute("AutomationProperties.AutomationId") == "SectionDossierCard", "section dossier")
    dossier.parentNode.removeChild(dossier)

    # Make each specialist visualizer a separately reusable keyed DataTemplate.
    # The page view-model remains the template data item, preserving all bindings.
    visual_grids = [item for item in descendants(page, "Grid")
                    if item.hasAttribute("Visibility") and re.search(r"\{Binding Is[A-Za-z]+(?:Visualizer|Inspector|Simulator|Validator|Studio|Overview|Auditor)", item.getAttribute("Visibility"))]
    if len(visual_grids) != 9:
        raise RuntimeError(f"Expected nine specialist visualizer grids, found {len(visual_grids)}; source left unchanged.")
    visual_templates = []
    for grid in visual_grids:
        visibility = grid.getAttribute("Visibility")
        property_match = re.search(r"\{Binding (Is[A-Za-z]+)", visibility)
        if property_match is None:
            raise RuntimeError(f"Could not determine visualizer selector from {visibility!r}.")
        selector = property_match.group(1)
        key = selector[2:] + "DashboardTemplate"
        dashboard_grid = grid.cloneNode(True)
        dashboard_grid.removeAttribute("Visibility")
        template = create_xaml(document, "DataTemplate", {"x:Key": key})
        template.appendChild(dashboard_grid)
        visual_templates.append(template)

        content = create_xaml(document, "ContentControl", {
            "Content": "{Binding}",
            "ContentTemplate": "{StaticResource " + key + "}",
            "Visibility": visibility,
            "MinWidth": "0",
            "MinHeight": "0",
        })
        grid.parentNode.replaceChild(content, grid)

    for scroll in descendants(page, "ScrollViewer"):
        if scroll.getAttribute("AutomationProperties.AutomationId") == "WorkbenchPageScrollViewport":
            set_attr(scroll, "MinWidth", "{Binding MinimumPresentationWidth, FallbackValue=360}")
    for grid in descendants(page, "Grid"):
        if grid.getAttribute("MinWidth") == "360":
            set_attr(grid, "MinWidth", "{Binding MinimumPresentationWidth, FallbackValue=360}")

    # Responsive header: wide rows retain the existing one-row shape, while
    # narrow work areas flow groups to an additional row without clipping.
    header_grid = required(header, lambda item: item.localName == "Grid" and item.getAttribute("Grid.Row") == "0", "header groups grid")
    for child in element_children(header_grid):
        if child.localName == "Grid.ColumnDefinitions":
            header_grid.removeChild(child)
    header_grid.tagName = "WrapPanel"
    widths = {"0": "150", "1": "128", "2": "88", "3": "120", "4": "174", "5": "138", "6": "132"}
    for child in element_children(header_grid):
        column = child.getAttribute("Grid.Column")
        if column not in widths:
            continue
        child.removeAttribute("Grid.Column")
        set_attr(child, "Width", widths[column])
        if child.localName in ("Button", "CheckBox"):
            set_attr(child, "MinHeight", "40")
        set_attr(child, "Margin", "2,0,3,0")

    toggle = create_xaml(document, "ToggleButton", {
        "AutomationProperties.AutomationId": "ContextDossierToggleButton",
        "AutomationProperties.Name": "{DynamicResource Ui.DossierHeading}",
        "ToolTip": "{DynamicResource Ui.DossierHeading}",
        "IsChecked": "{Binding IsContextDossierOpen, Mode=TwoWay}",
        "Width": "138",
        "MinHeight": "40",
        "Padding": "5,3",
        "Margin": "2,0,3,0",
        "FontSize": "9",
        "Cursor": "Hand",
    })
    toggle_text = create_xaml(document, "TextBlock", {
        "Text": "{DynamicResource Ui.DossierHeading}",
        "Foreground": "{DynamicResource BrassBrush}",
        "FontWeight": "Bold",
        "TextAlignment": "Center",
        "TextWrapping": "Wrap",
    })
    toggle.appendChild(toggle_text)
    header_grid.appendChild(toggle)

    # Replace fixed cards with equal flexible status columns. The existing
    # status content and automation IDs are retained.
    status_grid = required(status, lambda item: item.localName == "Grid", "status cards grid")
    for child in element_children(status_grid):
        if child.localName == "Grid.ColumnDefinitions":
            status_grid.removeChild(child)
    status_grid.tagName = "UniformGrid"
    set_attr(status_grid, "Columns", "5")
    for child in element_children(status_grid):
        if child.localName == "Border":
            remove_attr(child, "Grid.Column")
            set_attr(child, "MinWidth", "0")

    # Navigation remains the primary rail. Clearing the filter is now an MVVM
    # command so its behavior remains local to the reusable presentation.
    navigation = required(workspace, lambda item: item.getAttribute("AutomationProperties.AutomationId") == "OperationalRailCard", "operational rail")
    clear_button = required(navigation, lambda item: item.getAttribute("AutomationProperties.AutomationId") == "ClearToolFilterButton", "clear-filter button")
    remove_attr(clear_button, "Click")
    set_attr(clear_button, "Command", "{Binding ClearFilterCommand}")

    active = required(workspace, lambda item: item.getAttribute("AutomationProperties.AutomationId") == "ActiveWorkbenchFrame", "active workbench frame")
    active_grid = required(active, lambda item: item.localName == "Grid", "active workbench grid")
    content = required(active_grid, lambda item: item.localName == "ContentControl" and item.getAttribute("AutomationProperties.AutomationId") == "ActiveWorkbenchContent", "active page content")
    pinned = required(active_grid, lambda item: item.getAttribute("AutomationProperties.AutomationId") == "PinnedDeckCard", "split deck")
    pinned.parentNode.removeChild(pinned)
    for child in element_children(active_grid):
        if child.localName == "Grid.ColumnDefinitions":
            active_grid.removeChild(child)
    remove_attr(content, "Grid.Column")
    remove_attr(active, "Grid.Column")
    active_grid.appendChild(content.cloneNode(True))
    content.parentNode.removeChild(content)
    remove_attr(pinned, "Grid.Column")
    remove_attr(pinned, "Width")
    remove_attr(pinned, "Margin")
    set_attr(pinned, "MinWidth", "0")
    set_attr(pinned, "MaxWidth", "360")
    set_attr(pinned, "MinHeight", "132")

    # Pull shell sections into small presentation controls. MainWindow stays
    # the WindowChrome/lifecycle composition root.
    remove_attr(header, "Grid.Row")
    remove_attr(status, "Grid.Row")
    remove_attr(navigation, "Grid.Column")
    remove_attr(footer, "Grid.Row")
    remove_attr(palette, "Grid.Row")
    remove_attr(palette, "Grid.RowSpan")

    template_document = document.createElementNS(WPF_NS, "ResourceDictionary")
    for name, value in {
        "xmlns": WPF_NS,
        "xmlns:x": "http://schemas.microsoft.com/winfx/2006/xaml",
        "xmlns:services": "clr-namespace:CalradiaForge.Desktop.Services",
        "xmlns:presentation": "clr-namespace:CalradiaForge.Desktop.Presentation",
        "xmlns:shell": "clr-namespace:System.Windows.Shell;assembly=PresentationFramework",
        "xmlns:materialDesign": "http://materialdesigninxaml.net/winfx/xaml/themes",
    }.items():
        template_document.setAttribute(name, value)
    for template in visual_templates:
        template_document.appendChild(document.createTextNode("\n  "))
        template_document.appendChild(template.cloneNode(True))
    template_document.appendChild(document.createTextNode("\n  "))
    template_document.appendChild(page.cloneNode(True))
    template_document.appendChild(document.createTextNode("\n "))
    VIEW_RESOURCES.mkdir(parents=True, exist_ok=True)
    (VIEW_RESOURCES / "ToolPageTemplates.xaml").write_text(
        template_document.toxml(), encoding="utf-8"
    )

    # A ScrollViewer in the drawer keeps the reference available without
    # reducing the route's working width at the minimum window size.
    dossier_markup = wrap_user_control(
        "ToolDossierControl",
        "<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Disabled\" Focusable=\"False\">\n"
        + xaml(dossier) + "\n</ScrollViewer>",
    )
    (PRESENTATION / "ToolDossierControl.xaml").write_text(dossier_markup, encoding="utf-8")

    for class_name, node in [
        ("WorkbenchHeaderControl", header),
        ("WorkbenchStatusControl", status),
        ("WorkbenchNavigationControl", navigation),
        ("CommandPaletteControl", palette),
        ("WorkbenchFooterControl", footer),
    ]:
        (PRESENTATION / (class_name + ".xaml")).write_text(
            wrap_user_control(class_name, xaml(node)), encoding="utf-8"
        )

    workspace_markup = wrap_user_control(
        "WorkbenchWorkspaceControl",
        f'''<Grid>
 <presentation:AdaptiveWorkbenchPanel AutomationProperties.AutomationId="ResponsiveWorkbenchLayout"
  MinimumPrimaryWidth="{{Binding CurrentPage.MinimumPresentationWidth, FallbackValue=360}}"
  PreferredSecondaryWidth="280" MinimumSecondaryWidth="250" Gap="8">
  {xaml(active)}
  {xaml(pinned)}
 </presentation:AdaptiveWorkbenchPanel>
 <Border AutomationProperties.AutomationId="ContextDossierPanel" Panel.ZIndex="5" HorizontalAlignment="Right" VerticalAlignment="Stretch" Width="310" MaxWidth="46%" Margin="4,0,4,0"
  Background="{{DynamicResource RailSurfaceBrush}}" BorderBrush="{{DynamicResource BrassBrush}}" BorderThickness="1" CornerRadius="4" Padding="8"
  Visibility="{{Binding IsContextDossierOpen, Converter={{StaticResource BooleanToVisibilityConverter}}}}">
  <presentation:ToolDossierControl DataContext="{{Binding CurrentPage}}"/>
 </Border>
</Grid>''',
    )
    (PRESENTATION / "WorkbenchWorkspaceControl.xaml").write_text(workspace_markup, encoding="utf-8")

    shell_markup = f'''<UserControl x:Class="CalradiaForge.Desktop.Presentation.WorkbenchShellView"
 xmlns="{WPF_NS}"
 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
 xmlns:presentation="clr-namespace:CalradiaForge.Desktop.Presentation"
 MinWidth="0" MinHeight="0">
 <Grid Margin="14,8,14,12">
  <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*" MinHeight="320"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
  <presentation:WorkbenchHeaderControl Grid.Row="0"/>
  <presentation:WorkbenchStatusControl Grid.Row="1"/>
  <Grid Grid.Row="2">
   <Grid.ColumnDefinitions><ColumnDefinition Width="0.55*" MinWidth="220" MaxWidth="276"/><ColumnDefinition Width="12"/><ColumnDefinition Width="1.45*" MinWidth="400"/></Grid.ColumnDefinitions>
   <presentation:WorkbenchNavigationControl Grid.Column="0"/>
   <Grid Grid.Column="1"/>
   <presentation:WorkbenchWorkspaceControl Grid.Column="2"/>
  </Grid>
  <presentation:WorkbenchFooterControl Grid.Row="3"/>
  <presentation:CommandPaletteControl Grid.Row="0" Grid.RowSpan="4" Panel.ZIndex="20"/>
 </Grid>
</UserControl>
'''
    (PRESENTATION / "WorkbenchShellView.xaml").write_text(shell_markup, encoding="utf-8")

    # Replace Window-level implicit template with a merged, reusable dictionary.
    replacement_resources = '''<Window.Resources>
  <ResourceDictionary>
   <ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Views/ToolPageTemplates.xaml"/>
   </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
 </Window.Resources>'''
    text = replace_main_region(original, "<Window.Resources>", replacement_resources)
    body_start = '<Grid Grid.Row="1" Margin="14,8,14,12">'
    replacement_body = '<presentation:WorkbenchShellView Grid.Row="1"/>'
    text = replace_main_region(text, body_start, replacement_body)
    MAIN_WINDOW.write_text(text, encoding="utf-8")

    # Generate the small boilerplate code-behind files only after every XAML
    # extraction/selector-count assertion has succeeded.
    for class_name in [
        "WorkbenchShellView", "WorkbenchHeaderControl", "WorkbenchStatusControl",
        "WorkbenchNavigationControl", "WorkbenchWorkspaceControl", "WorkbenchFooterControl",
        "CommandPaletteControl", "ToolDossierControl",
    ]:
        code = f'''using System.Windows.Controls;

namespace CalradiaForge.Desktop.Presentation
{{
    public partial class {class_name} : UserControl
    {{
        public {class_name}() => InitializeComponent();
'''
        if class_name == "ToolDossierControl":
            code += '''
        void CopyCommandClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && element.Tag is string text && !string.IsNullOrEmpty(text))
            {
                try { System.Windows.Clipboard.SetText(text); } catch { }
            }
        }
'''
        code += "    }\n}\n"
        (PRESENTATION / (class_name + ".xaml.cs")).write_text(code, encoding="utf-8")

    print("PASS: MainWindow remains the root window and shell sections are split into reusable presentation controls.")
    print(f"PASS: extracted {len(visual_templates)} independently keyed visualizer templates and one implicit ToolPage template.")


if __name__ == "__main__":
    main()
