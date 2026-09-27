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
		private static readonly string[] ImportWords = { "import", "importar", "importieren", "importer" };
		private static readonly string[] ConfirmButtonNames = {
			"import", "import asset", "import new asset", "importar", "importar recurso",
			"importieren", "import resource", "confirm import"
		};

		private static int Main(string[] args)
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

		private static int Run(string[] args)
		{
			if (args.Length == 0 || IsOption(args[0], "--help", "-h", "/?"))
			{
				PrintUsage();
				return args.Length == 0 ? 1 : 0;
			}

			if (IsOption(args[0], "--dry-run"))
				return DryRun(args.Skip(1).ToArray());

			if (IsOption(args[0], "--inspect"))
				return InspectEditor();

			if (args.Length > 2)
			{
				PrintUsage();
				return 1;
			}

			string sourceFolder = Path.GetFullPath(args[0]);
			string pattern = args.Length > 1 ? args[1] : "*.fbx";
			if (!Directory.Exists(sourceFolder))
				throw new DirectoryNotFoundException("Source folder was not found: " + sourceFolder);

			List<string> files = GetFiles(sourceFolder, pattern);
			if (files.Count == 0)
			{
				Console.WriteLine("No files matched the requested pattern.");
				return 0;
			}

			var duplicateNames = files
				.GroupBy(file => Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key)
				.ToArray();
			if (duplicateNames.Length > 0)
				throw new InvalidOperationException("Duplicate asset names would collide in the Editor: " + string.Join(", ", duplicateNames));

			Console.WriteLine(files.Count + " file(s) are ready.");
			Console.WriteLine("Editor process: " + EditorProcessName + ".exe");
			Console.WriteLine("The Resource Browser must be open at your module's Assets folder.");
			Console.WriteLine("This tool submits files to the Editor UI; it cannot verify the compiled TPAC output.");
			Console.WriteLine("Type IMPORT and press Enter to proceed, or anything else to cancel:");
			if (!string.Equals(Console.ReadLine(), "IMPORT", StringComparison.Ordinal))
			{
				Console.WriteLine("Cancelled before interacting with the Editor.");
				return 3;
			}

			using (var automation = new UIA3Automation())
			{
				var editor = AttachResourceBrowser(automation);
				int submitted = 0;
				int failed = 0;
				foreach (string file in files)
				{
					try
					{
						SubmitOne(editor.Window, automation, file);
						submitted++;
						Console.WriteLine("SUBMITTED (verify in Resource Browser): " + Path.GetFileName(file));
					}
					catch (Exception ex)
					{
						failed++;
						Console.Error.WriteLine("FAILED: " + Path.GetFileName(file) + " -> " + ex.Message);
						Console.Error.WriteLine("The batch will continue only if no modal dialog is left open.");
						if (editor.Window.ModalWindows.Any())
						{
							Console.Error.WriteLine("Stopping because a modal dialog is still open; review it before another import.");
							break;
						}
					}
					Thread.Sleep(500);
				}

				Console.WriteLine("Finished. Submitted to UI: " + submitted + "; failed: " + failed + ".");
				Console.WriteLine("Check Resource Browser output and compile status manually; this is not a TPAC validation.");
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

			List<string> files = GetFiles(sourceFolder, pattern);
			Console.WriteLine("DRY RUN: no Editor windows were attached and no files were imported.");
			Console.WriteLine("Pattern: " + pattern);
			Console.WriteLine("Matched files: " + files.Count);
			foreach (string file in files)
				Console.WriteLine(Path.GetFileName(file));

			var collisions = files.GroupBy(file => Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
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
					var desktop = automation.GetDesktop();
					Console.WriteLine("Visible import menu entries:");
					var entries = desktop.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
						.Where(item => item.IsEnabled && !item.IsOffscreen && ContainsImportWord(item.Name)).ToArray();
					if (entries.Length == 0)
						Console.WriteLine("  (none exposed; open the Assets context menu and run --inspect again)");
					foreach (var entry in entries)
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
					var application = Application.Attach(process);
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
				(string.Join("; ", attempts)));
		}

		private static void SubmitOne(Window browser, UIA3Automation automation, string filePath)
		{
			var assetsNodes = browser.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
				.Where(item => string.Equals(item.Name, "Assets", StringComparison.OrdinalIgnoreCase)).ToArray();
			if (assetsNodes.Length != 1)
				throw new InvalidOperationException("Expected one visible Assets tree item; found " + assetsNodes.Length + ". Run --inspect for its current UIA data.");

			assetsNodes[0].RightClick();
			Thread.Sleep(300);

			var importItems = automation.GetDesktop().FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
				.Where(item => item.IsEnabled && !item.IsOffscreen && ContainsImportWord(item.Name)).ToArray();
			if (importItems.Length != 1)
				throw new InvalidOperationException("Expected one enabled visible import menu item; found " + importItems.Length +
					". Candidates: " + FormatNames(importItems) + ". No menu item was invoked.");
			importItems[0].AsMenuItem().Invoke();

			Window fileDialog = WaitForModal(browser, 8000);
			if (fileDialog == null)
				throw new InvalidOperationException("The file selection dialog did not appear.");

			var fileNameBox = fileDialog.FindFirstDescendant(cf => cf.ByAutomationId("1148"))?.AsTextBox();
			if (fileNameBox == null)
				throw new InvalidOperationException("The standard file-name field 1148 was not exposed by the dialog.");
			fileNameBox.Enter(filePath);
			Thread.Sleep(200);
			Keyboard.Press(VirtualKeyShort.RETURN);

			Window optionsDialog = WaitForModal(browser, 8000);
			if (optionsDialog == null)
				throw new InvalidOperationException("No import-options dialog appeared after choosing the file; import status is unknown.");

			var buttons = optionsDialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
			var confirmButtons = buttons.Where(button => IsKnownConfirmButton(button.Name)).ToArray();
			if (confirmButtons.Length != 1)
				throw new InvalidOperationException("Expected one explicit import confirmation button; found " + confirmButtons.Length +
					". Dialog buttons: " + FormatNames(buttons) + ". No button was invoked.");
			confirmButtons[0].AsButton().Invoke();
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

		private static void DumpRelevantControls(Window window)
		{
			ControlType[] types = { ControlType.TreeItem, ControlType.MenuItem, ControlType.Button, ControlType.Edit, ControlType.Window };
			foreach (ControlType type in types)
			{
				foreach (var element in window.FindAllDescendants(cf => cf.ByControlType(type)))
					PrintControl(element);
			}
		}

		private static void PrintControl(AutomationElement element)
		{
			string name = Safe(() => element.Name);
			string automationId = Safe(() => element.AutomationId);
			string controlType = Safe(() => element.ControlType.ToString());
			string className = Safe(() => element.ClassName);
			string helpText = Safe(() => element.HelpText);
			Console.WriteLine("  Name='" + name + "' AutomationId='" + automationId + "' ControlType='" + controlType + "' ClassName='" + className + "' HelpText='" + helpText + "'");
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

		private static List<string> GetFiles(string sourceFolder, string pattern)
		{
			return Directory.EnumerateFiles(sourceFolder, pattern, SearchOption.TopDirectoryOnly)
				.OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static bool IsOption(string value, params string[] options)
		{
			return options.Any(option => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
		}

		private static void PrintUsage()
		{
			Console.WriteLine("Bannerlord Import Automation");
			Console.WriteLine("  <folder> [pattern]       Submit matching files through the open Resource Browser UI");
			Console.WriteLine("  --dry-run <folder> [pattern]  List matches and detect duplicate asset names; no UI access");
			Console.WriteLine("  --inspect                Read-only UIA control report; no clicks or imports");
			Console.WriteLine("  --help                   Show this help");
			Console.WriteLine("The tool reports submission to the UI, not TPAC compilation or successful game loading.");
		}

		private sealed class EditorAttachment
		{
			public EditorAttachment(Process process, Window window)
			{
				Process = process;
				Window = window;
			}

			public Process Process { get; }
			public Window Window { get; }
		}
	}
}
