using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BannerlordImportAutomation
{
	/// <summary>Bounded, UI-independent input and command policy for batch submission.</summary>
	public static class ImportBatchPolicy
	{
		public const int MaximumBatchFiles = 100;
		private static readonly string[] SupportedExtensions = { ".fbx", ".png" };

		public static bool TryParseSubmissionArguments(string[] args, out string sourceFolder, out string pattern, out string moduleName)
		{
			sourceFolder = null;
			pattern = "*.fbx";
			moduleName = null;
			if (args == null)
				return false;

			if (args.Length == 3 && IsOption(args[1], "--module-name"))
			{
				sourceFolder = args[0];
				moduleName = args[2];
				return true;
			}
			if (args.Length == 4 && IsOption(args[2], "--module-name"))
			{
				sourceFolder = args[0];
				pattern = args[1];
				moduleName = args[3];
				return true;
			}
			return false;
		}

		public static List<string> GetFiles(string sourceFolder, string pattern)
		{
			if (string.IsNullOrWhiteSpace(pattern) || pattern.IndexOfAny(new[] { '\\', '/' }) >= 0)
				throw new ArgumentException("Pattern must be a top-level filename pattern without directory separators.", "pattern");
			if (!Directory.Exists(sourceFolder))
				throw new DirectoryNotFoundException("Source folder was not found: " + sourceFolder);

			var files = new List<string>();
			foreach (string file in Directory.EnumerateFiles(sourceFolder, pattern, SearchOption.TopDirectoryOnly))
			{
				if (!SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
					throw new InvalidOperationException("Unsupported file extension. This helper accepts only .fbx and .png: " + Path.GetFileName(file));
				files.Add(file);
				if (files.Count > MaximumBatchFiles)
					throw new InvalidOperationException("Batch exceeds the maximum of " + MaximumBatchFiles + " files. Split it into smaller batches.");
			}
			return files.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ToList();
		}

		public static void ValidateFileBatch(ICollection<string> files)
		{
			if (files == null)
				throw new ArgumentNullException("files");
			if (files.Count > MaximumBatchFiles)
				throw new InvalidOperationException("Batch contains more than " + MaximumBatchFiles + " files.");
			foreach (string file in files)
			{
				if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
					throw new InvalidOperationException("Reparse-point input is not accepted: " + file);
			}
		}

		public static string[] FindDuplicateAssetNames(IEnumerable<string> files)
		{
			if (files == null)
				throw new ArgumentNullException("files");
			return files.GroupBy(file => Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key)
				.ToArray();
		}

		public static void ValidateModuleName(string moduleName)
		{
			if (string.IsNullOrWhiteSpace(moduleName) || moduleName.Trim() != moduleName ||
				moduleName.IndexOf(ResourceBrowserTargetResolver.PathSeparator, StringComparison.Ordinal) >= 0 ||
				moduleName.IndexOfAny(new[] { '\\', '/' }) >= 0)
				throw new ArgumentException("Provide the exact visible module folder name with --module-name.", "moduleName");
		}

		private static bool IsOption(string value, params string[] options)
		{
			return options.Any(option => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
		}
	}
}
