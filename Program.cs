using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace BannerlordImportAutomation
{
	internal static class Program
	{
		private const string EditorProcessName = "TaleWorlds.MountAndBlade.Launcher";
		private const string ResourceBrowserTitle = "Resource Browser";
		private const int UiTimeoutMilliseconds = 8000;
		private static readonly string[] ImportWords = { "import", "importar", "importieren", "importer" };
		private static readonly string[] ConfirmButtonNames = {
			"import", "import asset", "import new asset", "importar", "importar recurso",
			"importieren", "import resource", "confirm import"
		};

		private static int Main(string[] args)
		{
			return Execute(args);
		}

		internal static int Execute(string[] args)
		{
			try
			{
				return Run(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("ERROR: " + ex.Message);
				return 2;
			}
		}

		internal static int Run(string[] args)
		{
			if (args.Length == 0 || IsOption(args[0], "--help", "-h", "/?"))
			{
				PrintUsage();
				return args.Length == 0 ? 1 : 0;
			}

			if (IsOption(args[0], "--dry-run"))
				return DryRun(args.Skip(1).ToArray());

			if (IsOption(args[0], "--inspect"))
			{
				if (args.Length != 1)
				{
					PrintUsage();
					return 1;
				}
				return InspectEditor();
			}

			return SubmitBatch(args);
		}

		private static int SubmitBatch(string[] args)
		{
			string sourceFolder;
			string pattern;
			string moduleName;
			if (!ImportBatchPolicy.TryParseSubmissionArguments(args, out sourceFolder, out pattern, out moduleName))
			{
				PrintUsage();
				return 1;
			}

			ImportBatchPolicy.ValidateModuleName(moduleName);
			string resolvedSourceFolder = Path.GetFullPath(sourceFolder);
			if (!Directory.Exists(resolvedSourceFolder))
				throw new DirectoryNotFoundException("Source folder was not found: " + resolvedSourceFolder);

			List<string> files = ImportBatchPolicy.GetFiles(resolvedSourceFolder, pattern);
			ImportBatchPolicy.ValidateFileBatch(files);
			if (files.Count == 0)
			{
				Console.WriteLine("No supported files matched the requested pattern. No Editor window was attached.");
				return 0;
			}

			string[] duplicateNames = ImportBatchPolicy.FindDuplicateAssetNames(files);
			if (duplicateNames.Length > 0)
				throw new InvalidOperationException("Duplicate asset names would collide in the Editor: " + string.Join(", ", duplicateNames));

			using (var automation = new UIA3Automation())
			{
				EditorAttachment editor = AttachResourceBrowser(automation);
				string targetPath;
				FindAssetsNode(editor.Window, moduleName, out targetPath);

				Console.WriteLine("Ready to submit " + files.Count + " file(s) to the open Resource Browser.");
				Console.WriteLine("Editor process: " + EditorProcessName + ".exe (PID " + editor.Process.Id + ")");
				Console.WriteLine("Destination: " + targetPath);
				Console.WriteLine("Files (top-level only; maximum " + ImportBatchPolicy.MaximumBatchFiles + "):");
				foreach (string file in files)
					Console.WriteLine("  " + Path.GetFileName(file));
				Console.WriteLine("This submits through the visible Editor UI. It cannot verify TPAC compilation or runtime loading.");
				Console.WriteLine("Type IMPORT and press Enter to proceed, or anything else to cancel:");
				if (!string.Equals(Console.ReadLine(), "IMPORT", StringComparison.Ordinal))
				{
					Console.WriteLine("Cancelled before interacting with the Resource Browser.");
					return 3;
				}

				int submitted = 0;
				int failed = 0;
				for (int index = 0; index < files.Count; index++)
				{
					string file = files[index];
					try
					{
						SubmitOne(editor.Window, automation, moduleName, targetPath, file);
						submitted++;
						Console.WriteLine("SUBMITTED (verify in Resource Browser): " + Path.GetFileName(file));
					}
					catch (Exception ex)
					{
						failed++;
						Console.Error.WriteLine("FAILED / OUTCOME NOT VERIFIED: " + Path.GetFileName(file) + " -> " + ex.Message);
						Console.Error.WriteLine("Stopping the batch. No automatic retry will be attempted.");
						break;
					}
				}

				int notAttempted = files.Count - submitted - failed;
				Console.WriteLine("Finished. UI submissions: " + submitted + "; failed or uncertain: " + failed + "; not attempted: " + notAttempted + ".");
				Console.WriteLine("Verify the imported resources and compiled package manually; SUBMITTED is not TPAC validation.");
				return failed == 0 ? 0 : 2;
			}
		}

		private static int DryRun(string[] args)
		{
			if (args.Length < 1 || args.Length > 2)
			{
				PrintUsage();
				return 1;
			}

			string sourceFolder = Path.GetFullPath(args[0]);
			string pattern = args.Length > 1 ? args[1] : "*.fbx";
			if (!Directory.Exists(sourceFolder))
				throw new DirectoryNotFoundException("Source folder was not found: " + sourceFolder);

			List<string> files = ImportBatchPolicy.GetFiles(sourceFolder, pattern);
			ImportBatchPolicy.ValidateFileBatch(files);
			Console.WriteLine("DRY RUN: no Editor windows were attached and no files were imported.");
			Console.WriteLine("Pattern: " + pattern);
			Console.WriteLine("Supported extensions: .fbx, .png");
			Console.WriteLine("Matched files: " + files.Count + " (maximum " + ImportBatchPolicy.MaximumBatchFiles + ")");
			foreach (string file in files)
				Console.WriteLine(Path.GetFileName(file));

			string[] collisions = ImportBatchPolicy.FindDuplicateAssetNames(files);
			if (collisions.Length > 0)
			{
				Console.WriteLine("Duplicate asset names: " + string.Join(", ", collisions));
				return 2;
			}
			return 0;
		}

		private static int InspectEditor()
		{
			using (var automation = new UIA3Automation())
			{
				EditorAttachment editor = AttachResourceBrowser(automation);
				Console.WriteLine("Process: " + editor.Process.ProcessName + ".exe (PID " + editor.Process.Id + ")");
				Console.WriteLine("Window: " + editor.Window.Title);
				Console.WriteLine("Read-only inspection; no clicks, keystrokes, file selection, or import will be performed.");
				DumpRelevantControls(editor.Window);

				try
				{
					AutomationElement[] entries = automation.GetDesktop()
						.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
						.Where(item => item.FrameworkAutomationElement.ProcessId.Value == editor.Process.Id && item.IsEnabled && !item.IsOffscreen && ContainsImportWord(item.Name)).ToArray();
					Console.WriteLine("Visible import menu entries:");
					if (entries.Length == 0)
						Console.WriteLine("  (none exposed; open the Assets context menu manually and run --inspect again)");
					foreach (AutomationElement entry in entries)
						PrintControl(entry);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Desktop menu inspection unavailable: " + ex.Message);
				}
			}
			return 0;
		}

		private static EditorAttachment AttachResourceBrowser(UIA3Automation automation)
		{
			Process[] processes = Process.GetProcessesByName(EditorProcessName);
			if (processes.Length == 0)
				throw new InvalidOperationException("Editor process not found: " + EditorProcessName + ".exe");

			var attempts = new List<string>();
			foreach (Process process in processes)
			{
				try
				{
					Application application = Application.Attach(process);
					Window[] windows = application.GetAllTopLevelWindows(automation);
					Window browser = windows.FirstOrDefault(window =>
						window.Title.IndexOf(ResourceBrowserTitle, StringComparison.OrdinalIgnoreCase) >= 0);
					if (browser != null)
						return new EditorAttachment(process, browser);

					attempts.Add("PID " + process.Id + ": " + string.Join(" | ", windows.Select(window => window.Title)));
				}
				catch (Exception ex)
				{
					attempts.Add("PID " + process.Id + ": " + ex.Message);
				}
			}

			throw new InvalidOperationException("The Editor process is running, but no '" + ResourceBrowserTitle + "' window was found. " +
				string.Join("; ", attempts));
		}

		private static AutomationElement FindAssetsNode(Window browser, string moduleName, out string targetPath)
		{
			AutomationElement[] assetsNodes = browser.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
				.Where(item => !item.IsOffscreen && string.Equals(item.Name, "Assets", StringComparison.OrdinalIgnoreCase))
				.ToArray();
			// Preserve unreadable candidates so the resolver can fail closed instead of silently
			// treating an incomplete UI Automation tree as a unique destination.
			string[] candidatePaths = assetsNodes.Select(GetTreeItemPath).ToArray();
			string resolvedTargetPath = ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(candidatePaths, moduleName);
			targetPath = resolvedTargetPath;

			AutomationElement[] matches = assetsNodes
				.Where(item => string.Equals(GetTreeItemPath(item), resolvedTargetPath, StringComparison.OrdinalIgnoreCase))
				.ToArray();
			if (matches.Length != 1)
				throw new InvalidOperationException("The resolved destination path did not map to exactly one visible Assets control.");
			return matches[0];
		}

		private static void SubmitOne(Window browser, UIA3Automation automation, string moduleName, string expectedTargetPath, string filePath)
		{
			string actualTargetPath;
			AutomationElement assetsNode = FindAssetsNode(browser, moduleName, out actualTargetPath);
			if (!string.Equals(actualTargetPath, expectedTargetPath, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("The Resource Browser destination changed after confirmation.");

			assetsNode.RightClick();
			AutomationElement[] importItems = WaitForImportMenuItems(automation, browser.FrameworkAutomationElement.ProcessId.Value, UiTimeoutMilliseconds);
			if (importItems.Length != 1)
				throw new InvalidOperationException("Expected one enabled visible import menu item; found " + importItems.Length +
					". Candidates: " + FormatNames(importItems) + ". No menu item was invoked.");
			importItems[0].AsMenuItem().Invoke();

			Window fileDialog = WaitForModal(browser, UiTimeoutMilliseconds);
			if (fileDialog == null)
				throw new InvalidOperationException("The file selection dialog did not appear; stopping because UI state is uncertain.");

			TextBox fileNameBox = fileDialog.FindFirstDescendant(cf => cf.ByAutomationId("1148"))?.AsTextBox();
			if (fileNameBox == null)
				throw new InvalidOperationException("The standard file-name field 1148 was not exposed; stopping because UI state is uncertain.");
			fileNameBox.Enter(filePath);
			Keyboard.Press(VirtualKeyShort.RETURN);

			Window optionsDialog = WaitForModal(browser, UiTimeoutMilliseconds);
			if (optionsDialog == null)
				throw new InvalidOperationException("No import-options dialog appeared after choosing the file; outcome is unknown.");

			AutomationElement[] buttons = optionsDialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
			AutomationElement[] confirmButtons = buttons.Where(button => IsKnownConfirmButton(button.Name)).ToArray();
			if (confirmButtons.Length != 1)
				throw new InvalidOperationException("Expected one explicit import confirmation button; found " + confirmButtons.Length +
					". Dialog buttons: " + FormatNames(buttons) + ". No button was invoked.");

			confirmButtons[0].AsButton().Invoke();
			if (!WaitForModalToClose(browser, UiTimeoutMilliseconds))
				throw new InvalidOperationException("The confirmation dialog did not close; submission outcome is unknown.");
		}

		private static AutomationElement[] WaitForImportMenuItems(UIA3Automation automation, int editorProcessId, int timeoutMilliseconds)
		{
			var timer = Stopwatch.StartNew();
			while (timer.ElapsedMilliseconds < timeoutMilliseconds)
			{
				AutomationElement[] items = automation.GetDesktop()
					.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
					.Where(item => item.FrameworkAutomationElement.ProcessId.Value == editorProcessId && item.IsEnabled && !item.IsOffscreen && ContainsImportWord(item.Name)).ToArray();
				if (items.Length > 1)
					return items;
				if (items.Length == 1)
					return items;
				Thread.Sleep(100);
			}
			return new AutomationElement[0];
		}

		private static Window WaitForModal(Window browser, int timeoutMilliseconds)
		{
			var timer = Stopwatch.StartNew();
			while (timer.ElapsedMilliseconds < timeoutMilliseconds)
			{
				Window dialog = browser.ModalWindows.FirstOrDefault();
				if (dialog != null)
					return dialog;
				Thread.Sleep(100);
			}
			return null;
		}

		private static bool WaitForModalToClose(Window browser, int timeoutMilliseconds)
		{
			var timer = Stopwatch.StartNew();
			while (timer.ElapsedMilliseconds < timeoutMilliseconds)
			{
				if (!browser.ModalWindows.Any())
					return true;
				Thread.Sleep(100);
			}
			return !browser.ModalWindows.Any();
		}

		private static void DumpRelevantControls(Window window)
		{
			ControlType[] types = { ControlType.TreeItem, ControlType.MenuItem, ControlType.Button, ControlType.Edit, ControlType.Window };
			foreach (ControlType type in types)
			{
				IEnumerable<AutomationElement> elements = window.FindAllDescendants(cf => cf.ByControlType(type));
				if (type == ControlType.TreeItem)
					elements = elements.Where(element => !element.IsOffscreen);
				foreach (AutomationElement element in elements)
				{
					string path = type == ControlType.TreeItem ? GetTreeItemPath(element) : string.Empty;
					PrintControl(element, path);
				}
			}
		}

		private static string GetTreeItemPath(AutomationElement element)
		{
			var names = new List<string>();
			AutomationElement current = element;
			int depth = 0;
			while (current != null && depth++ < 64)
			{
				try
				{
					if (current.ControlType == ControlType.TreeItem && !string.IsNullOrWhiteSpace(current.Name))
						names.Add(current.Name.Trim());
					current = current.Parent;
				}
				catch
				{
					return string.Empty;
				}
			}
			names.Reverse();
			return string.Join(ResourceBrowserTargetResolver.PathSeparator, names);
		}

		private static void PrintControl(AutomationElement element, string path = null)
		{
			string name = Safe(() => element.Name);
			string automationId = Safe(() => element.AutomationId);
			string controlType = Safe(() => element.ControlType.ToString());
			string className = Safe(() => element.ClassName);
			string helpText = Safe(() => element.HelpText);
			Console.WriteLine("  Name='" + name + "' AutomationId='" + automationId + "' ControlType='" + controlType + "' ClassName='" + className + "' HelpText='" + helpText + "'" +
				(string.IsNullOrWhiteSpace(path) ? string.Empty : " Path='" + path + "'"));
		}

		private static string Safe(Func<string> getter)
		{
			try { return getter() ?? string.Empty; }
			catch { return "<unavailable>"; }
		}

		private static bool ContainsImportWord(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			return ImportWords.Any(word => value.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private static bool IsKnownConfirmButton(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			string normalized = value.Trim();
			return ConfirmButtonNames.Any(name => string.Equals(normalized, name, StringComparison.OrdinalIgnoreCase));
		}

		private static string FormatNames(IEnumerable<AutomationElement> elements)
		{
			string[] names = elements.Select(element => Safe(() => element.Name)).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
			return names.Length == 0 ? "(no accessible names)" : string.Join(", ", names);
		}

		private static bool IsOption(string value, params string[] options)
		{
			return options.Any(option => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
		}

		private static void PrintUsage()
		{
			Console.WriteLine("Bannerlord Import Automation (visible UI submission only)");
			Console.WriteLine("  <folder> [pattern] --module-name <visible-module-folder>");
			Console.WriteLine("      Submit top-level .fbx/.png files to one explicit module Assets folder; maximum " + ImportBatchPolicy.MaximumBatchFiles + " files.");
			Console.WriteLine("  --dry-run <folder> [pattern]  Validate and list files; never attach to the Editor.");
			Console.WriteLine("  --inspect                      Read-only UIA control/path report; no clicks or imports.");
			Console.WriteLine("  --help                         Show this help.");
			Console.WriteLine("Submission requires typing IMPORT. The tool reports SUBMITTED UI actions, not TPAC compilation or successful game loading.");
		}

		private sealed class EditorAttachment
		{
			public EditorAttachment(Process process, Window window)
			{
				Process = process;
				Window = window;
			}

			public Process Process { get; private set; }
			public Window Window { get; private set; }
		}
	}
}
