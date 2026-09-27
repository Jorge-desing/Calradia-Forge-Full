using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Automation;
using BannerlordFbxImporter.Automation;

namespace BannerlordFbxImporter;

/// <summary>
/// Process-scoped, read-only Resource Browser inspection. Submission remains
/// disabled until the full import settings and inventory flow is manually calibrated.
/// </summary>
public sealed class WindowsEditorAutomation : IResourceBrowserAutomation, IDisposable
{
    private const int MaximumInspectedControls = 1000;
    private const int MaximumTreeDepth = 64;
    private const int MaximumPickerMenuControls = 2000;
    private const uint GwOwner = 4;
    private readonly Process _process;
    private readonly AutomationElement _mainWindow;
    private readonly string _windowTitle;
    private readonly IntPtr _windowHandle;
    private bool _disposed;

    private WindowsEditorAutomation(Process process, AutomationElement mainWindow, string windowTitle, IntPtr windowHandle)
    {
        _process = process;
        _mainWindow = mainWindow;
        _windowTitle = windowTitle;
        _windowHandle = windowHandle;
    }

    public static WindowsEditorAutomation Attach(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.ValidateBasic();
        Process[] matches = Process.GetProcessesByName(config.EditorProcessName);
        var candidates = new List<(Process Process, EditorWindowCandidate Window)>();
        foreach (Process process in matches)
        {
            try
            {
                EditorWindowCandidate[] processMatches = EnumerateProcessWindows(process)
                    .Where(window => window.IsVisible &&
                        string.Equals(window.Title, config.ResourceBrowserWindowTitle, StringComparison.Ordinal))
                    .ToArray();
                if (processMatches.Length == 0)
                {
                    process.Dispose();
                    continue;
                }

                candidates.AddRange(processMatches.Select(window => (process, window)));
            }
            catch
            {
                process.Dispose();
            }
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException($"No visible '{config.ResourceBrowserWindowTitle}' window was found in configured Editor process '{config.EditorProcessName}'. The helper will not launch the game or search other processes.");
        if (candidates.Count != 1)
        {
            foreach (Process process in candidates.Select(candidate => candidate.Process).Distinct()) process.Dispose();
            throw new InvalidOperationException($"Found {candidates.Count} visible '{config.ResourceBrowserWindowTitle}' windows in processes named '{config.EditorProcessName}'. Close duplicates so the Editor target remains unambiguous.");
        }

        (Process selected, EditorWindowCandidate selectedWindow) = candidates[0];
        try
        {
            EditorWindowCandidate resolved = ProcessWindowResolver.ResolveUnique([selectedWindow], selected.Id, config.ResourceBrowserWindowTitle);
            AutomationElement root = AutomationElement.FromHandle(resolved.Handle)
                ?? throw new InvalidOperationException("The configured Resource Browser window is not exposed through UI Automation.");
            if (root.Current.ProcessId != selected.Id)
                throw new InvalidOperationException("The selected window process identity changed during attachment.");
            if (!string.Equals(root.Current.Name, config.ResourceBrowserWindowTitle, StringComparison.Ordinal))
                throw new InvalidOperationException("The selected Editor window title changed during attachment.");
            return new WindowsEditorAutomation(selected, root, config.ResourceBrowserWindowTitle, resolved.Handle);
        }
        catch
        {
            selected.Dispose();
            throw;
        }
    }

    public EditorInspection Inspect()
    {
        ThrowIfDisposed();
        IReadOnlyList<AutomationElement> elements = EnumerateControls(_mainWindow, MaximumInspectedControls, out bool truncated);
        var treeItems = BuildTreeItems(elements);
        var controls = new List<InspectedControl>(elements.Count);
        foreach (AutomationElement element in elements)
        {
            try
            {
                var current = element.Current;
                if (current.ProcessId != _process.Id) continue;
                string name = current.Name ?? "";
                string type = current.ControlType?.ProgrammaticName.Replace("ControlType.", "", StringComparison.Ordinal) ?? "";
                string path = type == "TreeItem"
                    ? treeItems.FirstOrDefault(item => item.Element.Equals(element)).Path ?? ""
                    : BuildControlPath(element, name);
                var rectangle = current.BoundingRectangle;
                string bounds = $"{rectangle.Left:0.##},{rectangle.Top:0.##},{rectangle.Width:0.##},{rectangle.Height:0.##}";
                string patterns = string.Join(",", new[]
                {
                    HasPattern(element, InvokePattern.Pattern) ? "Invoke" : null,
                    HasPattern(element, ValuePattern.Pattern) ? "Value" : null,
                    HasPattern(element, SelectionItemPattern.Pattern) ? "Select" : null,
                    HasPattern(element, ExpandCollapsePattern.Pattern) ? "ExpandCollapse" : null,
                    HasPattern(element, TogglePattern.Pattern) ? "Toggle" : null
                }.Where(value => value is not null));
                controls.Add(new(name, current.AutomationId ?? "", type, path, _process.Id, patterns, bounds));
            }
            catch (ElementNotAvailableException)
            {
                controls.Add(new("<unavailable>", "<unavailable>", "<unavailable>", "<unavailable>", _process.Id, "unavailable", "<unavailable>"));
            }
        }

        string[] paths = treeItems.Select(item => item.Path).ToArray();
        var warnings = new List<string>();
        if (truncated) warnings.Add($"Control listing stopped at {MaximumInspectedControls} elements.");
        if (treeItems.Any(item => item.Path.Split(" > ", StringSplitOptions.None).Length < 3))
            warnings.Add("UI Automation exposed the visible tree items without a provable Modules > module > Assets hierarchy. Destination resolution must fail closed until the Editor exposes parent paths or a human-approved calibration records them.");
        if (treeItems.Count == 0)
            warnings.Add("No visible tree items with readable names and bounds were available.");
        return new EditorInspection(_process.Id, _process.ProcessName, _windowTitle, paths, controls, warnings);
    }

    public string ResolveUniqueAssetsTarget(string moduleName)
    {
        ThrowIfDisposed();
        List<(AutomationElement Element, string Name, string Path)> items = BuildTreeItems(
            EnumerateControls(_mainWindow, MaximumInspectedControls, out bool truncated)).ToList();
        if (truncated)
            throw new InvalidOperationException("The UI tree exceeded its bound; a unique destination cannot be established safely.");
        return ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(items
            .Where(item => string.Equals(item.Name, "Assets", StringComparison.Ordinal))
            .Select(item => (string?)item.Path), moduleName);
    }

    public UiSubmissionResult SubmitFile(string moduleName, string expectedAssetsPath, Preflight.AssetFile file)
    {
        ThrowIfDisposed();
        return new UiSubmissionResult(UiSubmissionState.Unknown,
            "UNKNOWN: submission is disabled until the real Editor settings, complete resource inventory, replacement safeguards, and final Import action are calibrated and one sample is manually verified. No clicks, key input, or file changes were sent.");
    }

    /// <summary>
    /// Invokes only the uniquely exposed "Import new asset" context-menu command.
    /// This requests the file picker and deliberately stops before selecting a file.
    /// </summary>
    public void OpenImportPicker()
    {
        ThrowIfDisposed();
        EnsureTargetWindowStillMatches();

        var candidates = new List<ImportPickerMenuCandidate<AutomationElement>>();
        foreach (AutomationElement scopeRoot in EnumeratePickerScopeRoots())
        {
            IReadOnlyList<AutomationElement> controls = EnumerateControls(scopeRoot, MaximumPickerMenuControls, out bool truncated);
            if (truncated)
                throw new InvalidOperationException("The Resource Browser context-menu tree exceeded its UIA bound. No menu item was invoked.");

            foreach (AutomationElement element in controls)
            {
                try
                {
                    var current = element.Current;
                    if (current.ProcessId != _process.Id || current.IsOffscreen ||
                        current.ControlType != ControlType.MenuItem ||
                        !string.Equals(current.Name, ImportPickerMenuAction.ExactMenuItemName, StringComparison.Ordinal) ||
                        !HasVisibleMenuAncestor(element))
                        continue;

                    candidates.Add(new ImportPickerMenuCandidate<AutomationElement>(
                        current.Name,
                        current.ControlType.ProgrammaticName,
                        current.ProcessId,
                        _windowTitle,
                        IsInsideVisibleMenu: true,
                        IsVisible: true,
                        current.IsEnabled,
                        HasPattern(element, InvokePattern.Pattern),
                        element));
                }
                catch (ElementNotAvailableException)
                {
                    // A stale UIA node is not actionable. The unique-target check below
                    // will fail closed if the menu item disappeared while inspecting.
                }
            }
        }

        ImportPickerMenuAction.InvokeUnique(candidates, _process.Id, _windowTitle, element =>
        {
            EnsureTargetWindowStillMatches();
            var current = element.Current;
            if (current.ProcessId != _process.Id || current.ControlType != ControlType.MenuItem ||
                !string.Equals(current.Name, ImportPickerMenuAction.ExactMenuItemName, StringComparison.Ordinal) ||
                current.IsOffscreen || !current.IsEnabled)
                throw new InvalidOperationException("The selected context-menu element changed before invocation.");

            if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern) || pattern is not InvokePattern invokePattern)
                throw new InvalidOperationException("The exact context-menu item no longer exposes InvokePattern.");
            invokePattern.Invoke();
        });
    }

    private void EnsureTargetWindowStillMatches()
    {
        if (_process.HasExited)
            throw new InvalidOperationException("The configured Editor process exited before the picker command could be invoked.");
        if (!IsWindowNative(_windowHandle))
            throw new InvalidOperationException("The attached Resource Browser window handle is no longer valid.");
        _ = GetWindowThreadProcessId(_windowHandle, out uint ownerProcessId);
        if (ownerProcessId != unchecked((uint)_process.Id))
            throw new InvalidOperationException("The attached Resource Browser window changed process identity.");

        var current = _mainWindow.Current;
        if (current.ProcessId != _process.Id || current.NativeWindowHandle != _windowHandle.ToInt32() ||
            !string.Equals(current.Name, _windowTitle, StringComparison.Ordinal))
            throw new InvalidOperationException("The exact Resource Browser window title, handle, or process identity changed.");
    }

    private IReadOnlyList<AutomationElement> EnumeratePickerScopeRoots()
    {
        var roots = new List<AutomationElement> { _mainWindow };
        foreach (IntPtr handle in EnumerateProcessWindows(_process)
            .Where(window => window.ProcessId == _process.Id && window.IsVisible && window.Handle != _windowHandle)
            .Select(window => window.Handle))
        {
            if (!IsOwnedByResourceBrowser(handle)) continue;
            AutomationElement? popup = AutomationElement.FromHandle(handle);
            if (popup is null) continue;
            try
            {
                var current = popup.Current;
                if (current.ProcessId == _process.Id)
                    roots.Add(popup);
            }
            catch (ElementNotAvailableException)
            {
                // A disappearing popup cannot be a safe automation target.
            }
        }
        return roots;
    }

    private bool IsOwnedByResourceBrowser(IntPtr handle)
    {
        IntPtr current = handle;
        var visited = new HashSet<IntPtr>();
        for (int depth = 0; depth < 8; depth++)
        {
            if (!visited.Add(current)) return false;
            IntPtr owner = GetWindow(current, GwOwner);
            if (owner == IntPtr.Zero) return false;
            _ = GetWindowThreadProcessId(owner, out uint ownerProcessId);
            if (ownerProcessId != unchecked((uint)_process.Id)) return false;
            if (owner == _windowHandle) return true;
            current = owner;
        }
        return false;
    }

    private bool HasVisibleMenuAncestor(AutomationElement element)
    {
        AutomationElement? parent = TreeWalker.ControlViewWalker.GetParent(element);
        for (int depth = 0; parent is not null && depth < 8; depth++)
        {
            var current = parent.Current;
            if (current.ProcessId != _process.Id || current.IsOffscreen) return false;
            if (current.ControlType == ControlType.Menu) return true;
            if (parent.Equals(_mainWindow)) return false;
            parent = TreeWalker.ControlViewWalker.GetParent(parent);
        }
        return false;
    }

    private List<(AutomationElement Element, string Name, string Path)> BuildTreeItems(IReadOnlyList<AutomationElement> elements)
    {
        var snapshots = new List<(AutomationElement Element, string Name, double Left, double Top)>();
        foreach (AutomationElement element in elements)
        {
            var current = element.Current;
            if (current.ProcessId != _process.Id || current.ControlType != ControlType.TreeItem || current.IsOffscreen) continue;
            if (string.IsNullOrWhiteSpace(current.Name))
                throw new InvalidOperationException("A visible Resource Browser tree item has an unreadable name; target resolution is unsafe.");
            var rectangle = current.BoundingRectangle;
            if (rectangle.IsEmpty || rectangle.Width <= 0 || rectangle.Height <= 0)
                throw new InvalidOperationException($"Visible tree item '{current.Name}' has unreadable bounds; parent hierarchy cannot be inferred safely.");
            snapshots.Add((element, current.Name, rectangle.Left, rectangle.Top));
        }

        snapshots.Sort(static (left, right) =>
        {
            int vertical = left.Top.CompareTo(right.Top);
            return vertical != 0 ? vertical : left.Left.CompareTo(right.Left);
        });
        var stack = new Stack<(double Left, string Name)>();
        var result = new List<(AutomationElement, string, string)>(snapshots.Count);
        foreach (var node in snapshots)
        {
            while (stack.Count > 0 && node.Left <= stack.Peek().Left + 2.0)
                stack.Pop();
            string path = string.Join(" > ", stack.Reverse().Select(parent => parent.Name).Append(node.Name));
            result.Add((node.Element, node.Name, path));
            if (stack.Count >= MaximumTreeDepth)
                throw new InvalidOperationException("Resource Browser visible tree depth exceeded the safety bound.");
            stack.Push((node.Left, node.Name));
        }
        return result;
    }

    private string BuildControlPath(AutomationElement element, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return SafeName(_mainWindow);
        var names = new List<string> { name };
        AutomationElement? current = TreeWalker.ControlViewWalker.GetParent(element);
        for (int depth = 0; current is not null && depth < 8; depth++)
        {
            if (current.Current.ProcessId != _process.Id) break;
            string parentName = current.Current.Name ?? "";
            if (!string.IsNullOrWhiteSpace(parentName)) names.Add(parentName);
            if (current.Equals(_mainWindow)) break;
            current = TreeWalker.ControlViewWalker.GetParent(current);
        }
        names.Reverse();
        return string.Join(" > ", names);
    }

    private static IReadOnlyList<AutomationElement> EnumerateControls(AutomationElement root, int maximum, out bool truncated)
    {
        var result = new List<AutomationElement>(Math.Min(maximum, 256));
        var pending = new Stack<AutomationElement>();
        pending.Push(root);
        truncated = false;
        while (pending.Count > 0)
        {
            AutomationElement element = pending.Pop();
            result.Add(element);
            if (result.Count >= maximum)
            {
                truncated = pending.Count > 0;
                break;
            }
            AutomationElement? child = TreeWalker.ControlViewWalker.GetFirstChild(element);
            var children = new List<AutomationElement>();
            while (child is not null && children.Count < maximum)
            {
                children.Add(child);
                child = TreeWalker.ControlViewWalker.GetNextSibling(child);
            }
            for (int index = children.Count - 1; index >= 0; index--)
                pending.Push(children[index]);
        }
        return result;
    }

    private static ControlType ParseControlType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "window" => ControlType.Window,
        "button" => ControlType.Button,
        "menuitem" => ControlType.MenuItem,
        "edit" => ControlType.Edit,
        "treeitem" => ControlType.TreeItem,
        "text" => ControlType.Text,
        "pane" => ControlType.Pane,
        "listitem" => ControlType.ListItem,
        "checkbox" => ControlType.CheckBox,
        "combobox" => ControlType.ComboBox,
        "dataitem" => ControlType.DataItem,
        _ => throw new InvalidOperationException($"Unsupported ControlType '{value}'.")
    };

    public static ControlType ParseConfiguredControlType(string? value) => ParseControlType(value);

    private static bool HasPattern(AutomationElement element, AutomationPattern pattern)
    {
        try { return element.TryGetCurrentPattern(pattern, out _); }
        catch { return false; }
    }

    private static string SafeName(AutomationElement element)
    {
        try { return element.Current.Name ?? ""; }
        catch { return "<unavailable>"; }
    }

    private static IReadOnlyList<EditorWindowCandidate> EnumerateProcessWindows(Process process)
    {
        var handles = new List<IntPtr>();
        EnumThreadWindowsCallback callback = (handle, state) =>
        {
            _ = GetWindowThreadProcessId(handle, out uint ownerProcessId);
            if (ownerProcessId == unchecked((uint)process.Id)) handles.Add(handle);
            return true;
        };

        foreach (ProcessThread thread in process.Threads)
            _ = EnumThreadWindows(unchecked((uint)thread.Id), callback, IntPtr.Zero);

        return handles.Select(handle =>
        {
            var title = new StringBuilder(Math.Max(256, GetWindowTextLength(handle) + 1));
            _ = GetWindowText(handle, title, title.Capacity);
            return new EditorWindowCandidate(process.Id, title.ToString(), IsWindowVisibleNative(handle), handle);
        }).ToArray();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowsEditorAutomation));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _process.Dispose();
    }

    private delegate bool EnumThreadWindowsCallback(IntPtr handle, IntPtr parameter);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumThreadWindows(uint threadId, EnumThreadWindowsCallback callback, IntPtr parameter);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maximumCount);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr handle);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "IsWindowVisible", SetLastError = true)]
    private static extern bool IsWindowVisibleNative(IntPtr handle);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "IsWindow", SetLastError = true)]
    private static extern bool IsWindowNative(IntPtr handle);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindow", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr handle, uint command);
}
