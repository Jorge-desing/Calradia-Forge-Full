using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace BannerlordEditorImport
{
	/// <summary>
	/// Maneja el Editor de Bannerlord por UI Automation para importar
	/// archivos en lote. Ver README.md: separa claramente qué es
	/// automatización genérica de Windows (verificada) de qué es
	/// específico del Editor de TaleWorlds (sin verificar — hay que
	/// completarlo con Accessibility Insights / inspect.exe).
	/// </summary>
	public sealed class EditorImportAutomation
	{
		// --------------------------------------------------------------
		// TODO (específico de TaleWorlds, sin verificar):
		// Completa estos dos valores inspeccionando el Editor con
		// Accessibility Insights for Windows o inspect.exe. Ver
		// instrucciones en README.md. Dejé el nombre más probable según
		// tutoriales de la comunidad, pero no está confirmado.
		// --------------------------------------------------------------
		private const string AssetsNodeName = "Assets";
		private const string ImportMenuItemNameContains = "Import";

		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

		public sealed class ImportOutcome
		{
			public string FilePath;
			public bool Success;
			public string Error;
		}

		// ================================================================
		// Orquestación de alto nivel
		// ================================================================

		public List<ImportOutcome> BatchImport(
			string editorExePath,
			string sourceFolder,
			Action<int, int, string> onProgress = null)
		{
			var files = Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly)
				.Where(f => f.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
						 || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
				.ToList();

			var mainWindow = AttachOrLaunchEditor(editorExePath);

			var results = new List<ImportOutcome>();
			for (int i = 0; i < files.Count; i++)
			{
				var file = files[i];
				var outcome = new ImportOutcome { FilePath = file };
				try
				{
					ImportSingleFile(mainWindow, file);
					outcome.Success = true;
				}
				catch (Exception ex)
				{
					// Un archivo que falle (diálogo inesperado, timeout,
					// etc.) no debe tumbar el resto del lote.
					outcome.Success = false;
					outcome.Error = ex.Message;
				}

				results.Add(outcome);
				onProgress?.Invoke(i + 1, files.Count, Path.GetFileName(file));
			}

			return results;
		}

		private void ImportSingleFile(AutomationElement mainWindow, string filePath)
		{
			var assetsNode = FindAssetsNode(mainWindow);
			RightClick(assetsNode);

			var importItem = WaitForMenuItemContaining(ImportMenuItemNameContains, DefaultTimeout);
			ClickMenuItem(importItem);

			var openDialog = WaitForOpenFileDialog(DefaultTimeout);
			TypeFileIntoOpenDialog(openDialog, filePath);

			// TODO (específico de TaleWorlds): si tras importar aparece un
			// diálogo de confirmación (aviso de escala, nombre duplicado,
			// etc. — mencionados en tutoriales de la comunidad), agrégalo
			// aquí con el mismo patrón que WaitForOpenFileDialog +
			// ClickButtonByName("OK") una vez sepas su texto exacto.
		}

		// ================================================================
		// Lanzar / adjuntar el proceso del Editor
		// (genérico — funciona tal cual)
		// ================================================================

		private AutomationElement AttachOrLaunchEditor(string editorExePath)
		{
			var processName = Path.GetFileNameWithoutExtension(editorExePath);
			var existing = Process.GetProcessesByName(processName).FirstOrDefault();
			var proc = existing ?? Process.Start(new ProcessStartInfo(editorExePath) { UseShellExecute = true });

			return WaitForMainWindow(proc, TimeSpan.FromSeconds(60));
		}

		private AutomationElement WaitForMainWindow(Process proc, TimeSpan timeout)
		{
			var deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline)
			{
				proc.Refresh();
				if (proc.MainWindowHandle != IntPtr.Zero)
				{
					var element = AutomationElement.FromHandle(proc.MainWindowHandle);
					if (element != null)
						return element;
				}
				Thread.Sleep(500);
			}
			throw new TimeoutException("No se encontró la ventana principal del Editor a tiempo.");
		}

		// ================================================================
		// Encontrar el nodo "Assets" en el árbol del Resource Browser
		// (el matching por nombre es genérico; AssetsNodeName es el TODO)
		// ================================================================

		private AutomationElement FindAssetsNode(AutomationElement mainWindow)
		{
			var condition = new PropertyCondition(AutomationElement.NameProperty, AssetsNodeName);
			var node = mainWindow.FindFirst(TreeScope.Descendants, condition);

			if (node == null)
				throw new InvalidOperationException(
					$"No se encontró un elemento de UI llamado '{AssetsNodeName}'. " +
					"Verifica el nombre real con Accessibility Insights (ver README.md) " +
					"y actualiza la constante AssetsNodeName.");

			return node;
		}

		// ================================================================
		// Clic derecho simulado sobre un AutomationElement
		// (genérico — funciona tal cual)
		// ================================================================

		[DllImport("user32.dll")]
		private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

		private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
		private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
		private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
		private const uint MOUSEEVENTF_LEFTUP = 0x0004;

		private void RightClick(AutomationElement element)
		{
			ClickCenter(element, right: true);
		}

		private void LeftClick(AutomationElement element)
		{
			ClickCenter(element, right: false);
		}

		private void ClickCenter(AutomationElement element, bool right)
		{
			var rect = element.Current.BoundingRectangle;
			var x = (int)(rect.Left + rect.Width / 2);
			var y = (int)(rect.Top + rect.Height / 2);

			Cursor.Position = new System.Drawing.Point(x, y);
			Thread.Sleep(100);

			if (right)
			{
				mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
				mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
			}
			else
			{
				mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
				mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
			}

			Thread.Sleep(300);
		}

		// ================================================================
		// Menú contextual: esperar y hacer clic en el ítem "Import"
		// (búsqueda por texto parcial es genérica; el texto es el TODO)
		// ================================================================

		private AutomationElement WaitForMenuItemContaining(string textFragment, TimeSpan timeout)
		{
			var deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline)
			{
				var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem);
				var items = AutomationElement.RootElement.FindAll(TreeScope.Descendants, condition);

				foreach (AutomationElement item in items)
				{
					if (item.Current.Name?.IndexOf(textFragment, StringComparison.OrdinalIgnoreCase) >= 0)
						return item;
				}
				Thread.Sleep(200);
			}

			throw new TimeoutException(
				$"No apareció ningún ítem de menú que contenga '{textFragment}'. " +
				"Verifica el texto real con Accessibility Insights y actualiza " +
				"ImportMenuItemNameContains.");
		}

		private void ClickMenuItem(AutomationElement menuItem)
		{
			if (menuItem.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
			{
				((InvokePattern)pattern).Invoke();
			}
			else
			{
				LeftClick(menuItem);
			}
		}

		// ================================================================
		// Diálogo estándar "Abrir archivo" de Windows
		// (genérico — funciona tal cual; "#32770" es la clase Win32
		// estándar de los diálogos comunes, no algo propio de TaleWorlds)
		// ================================================================

		private AutomationElement WaitForOpenFileDialog(TimeSpan timeout)
		{
			var deadline = DateTime.UtcNow + timeout;
			var condition = new PropertyCondition(AutomationElement.ClassNameProperty, "#32770");

			while (DateTime.UtcNow < deadline)
			{
				var dialog = AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
				if (dialog != null)
					return dialog;
				Thread.Sleep(200);
			}

			throw new TimeoutException("No apareció el diálogo de 'Abrir archivo' a tiempo.");
		}

		private void TypeFileIntoOpenDialog(AutomationElement dialog, string filePath)
		{
			// Traer la ventana al frente antes de escribir.
			var hwnd = new IntPtr(dialog.Current.NativeWindowHandle);
			SetForegroundWindow(hwnd);
			Thread.Sleep(200);

			// El combo "Nombre de archivo:" de los diálogos estándar de
			// Windows acepta texto tecleado directamente sin necesidad de
			// ubicar el control exacto: basta con que el diálogo tenga el
			// foco.
			SendKeys.SendWait("^a"); // seleccionar cualquier texto previo
			SendKeys.SendWait(SendKeys_Escape(filePath));
			Thread.Sleep(150);
			SendKeys.SendWait("{ENTER}");
			Thread.Sleep(500);
		}

		private static string SendKeys_Escape(string text)
		{
			// SendKeys trata +^%~(){} como especiales; hay que escaparlos.
			var special = "+^%~(){}";
			var sb = new System.Text.StringBuilder();
			foreach (var c in text)
			{
				if (special.IndexOf(c) >= 0)
					sb.Append('{').Append(c).Append('}');
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);
	}
}
