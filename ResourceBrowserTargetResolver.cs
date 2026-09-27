using System;
using System.Linq;

namespace BannerlordImportAutomation
{
	/// <summary>Matches the exact visible module/Assets path without touching UI controls.</summary>
	public static class ResourceBrowserTargetResolver
	{
		public const string PathSeparator = " > ";

		public static string ResolveUniqueAssetsPath(string[] candidatePaths, string moduleName)
		{
			if (candidatePaths == null)
				throw new ArgumentNullException("candidatePaths");
			if (string.IsNullOrWhiteSpace(moduleName))
				throw new ArgumentException("A visible module folder name is required.", "moduleName");
			if (candidatePaths.Any(string.IsNullOrWhiteSpace))
				throw new InvalidOperationException("At least one visible Assets tree path could not be read; no destination will be selected.");

			string[] matches = candidatePaths
				.Where(path => !string.IsNullOrWhiteSpace(path) && IsModuleAssetsPath(path, moduleName))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
			if (matches.Length == 0)
				throw new InvalidOperationException("No visible Assets tree path was found directly under module '" + moduleName + "'. Candidates: " +
					(candidatePaths.Length == 0 ? "(none)" : string.Join("; ", candidatePaths)) + ". Run --inspect and provide the exact visible module folder name.");
			if (matches.Length > 1)
				throw new InvalidOperationException("More than one visible Assets path matched module '" + moduleName + "': " + string.Join("; ", matches));
			return matches[0];
		}

		private static bool IsModuleAssetsPath(string path, string moduleName)
		{
			string[] segments = path.Split(new[] { PathSeparator }, StringSplitOptions.None);
			for (int index = 0; index < segments.Length - 1; index++)
			{
				if (index == segments.Length - 2 &&
					string.Equals(segments[index].Trim(), moduleName, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(segments[index + 1].Trim(), "Assets", StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
	}
}
