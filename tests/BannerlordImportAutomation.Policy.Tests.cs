using System;
using System.IO;
using System.Linq;
using BannerlordImportAutomation;

internal static class BannerlordImportAutomationPolicyTests
{
	public static void Run()
	{
		TestCliReadOnlyModes();
		string root = Path.Combine(Path.GetTempPath(), "Bannerlord Import ñ " + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		try
		{
			string source = Path.Combine(root, "assets with spaces");
			Directory.CreateDirectory(source);
			WriteFile(source, "arrow.fbx");
			WriteFile(source, "shield model.fbx");
			WriteFile(source, "espada ñ.fbx");
			WriteFile(source, "texture.png");
			WriteFile(source, "unsupported.obj");
			string nested = Path.Combine(source, "nested");
			Directory.CreateDirectory(nested);
			WriteFile(nested, "nested.fbx");

			string[] models = ImportBatchPolicy.GetFiles(source, "*.fbx").Select(Path.GetFileName).ToArray();
			Assert(models.Length == 3, "Only the three top-level FBX files should match.");
			Assert(models.Contains("espada ñ.fbx") && models.Contains("shield model.fbx"), "Unicode and spaced filenames should be preserved.");
			Assert(ImportBatchPolicy.GetFiles(source, "*.png").Count == 1, "PNG should be accepted.");
			ExpectRejected(() => ImportBatchPolicy.GetFiles(source, "*.obj"), "Unsupported file extension");
			ExpectRejected(() => ImportBatchPolicy.GetFiles(source, "nested\\*.fbx"), "without directory separators");
			ExpectRejected(() => ImportBatchPolicy.GetFiles(Path.Combine(root, "missing"), "*.fbx"), "Source folder was not found");

			string empty = Path.Combine(root, "empty");
			Directory.CreateDirectory(empty);
			Assert(ImportBatchPolicy.GetFiles(empty, "*.fbx").Count == 0, "An empty match must remain a clean zero-file batch.");

			string duplicate = Path.Combine(root, "duplicates");
			Directory.CreateDirectory(duplicate);
			WriteFile(duplicate, "same.fbx");
			WriteFile(duplicate, "same.png");
			string[] collisions = ImportBatchPolicy.FindDuplicateAssetNames(ImportBatchPolicy.GetFiles(duplicate, "*.*"));
			Assert(collisions.Length == 1 && collisions[0] == "same", "Cross-extension duplicate asset names should be detected.");

			string hundred = Path.Combine(root, "hundred");
			Directory.CreateDirectory(hundred);
			for (int index = 1; index <= 100; index++)
				WriteFile(hundred, "asset" + index.ToString("D3") + ".fbx");
			Assert(ImportBatchPolicy.GetFiles(hundred, "*.fbx").Count == 100, "Exactly 100 files should be accepted.");
			WriteFile(hundred, "asset101.fbx");
			ExpectRejected(() => ImportBatchPolicy.GetFiles(hundred, "*.fbx"), "maximum of 100");

			string folder;
			string pattern;
			string moduleName;
			Assert(!ImportBatchPolicy.TryParseSubmissionArguments(new[] { source }, out folder, out pattern, out moduleName), "Submission must require a module argument.");
			Assert(ImportBatchPolicy.TryParseSubmissionArguments(new[] { source, "*.png", "--module-name", "CalradiaForge" }, out folder, out pattern, out moduleName), "An explicit module submission request should parse.");
			Assert(pattern == "*.png" && moduleName == "CalradiaForge", "Parsed submission options should be preserved.");
			ImportBatchPolicy.ValidateModuleName(moduleName);
			ExpectRejected(() => ImportBatchPolicy.ValidateModuleName(" Modules "), "exact visible module folder name");

		string target = ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
			new[] { "Modules > Native > Assets", "Modules > CalradiaForge > Assets" }, "CalradiaForge");
		Assert(target == "Modules > CalradiaForge > Assets", "The exact requested module target should be selected.");
		ExpectRejected(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(new[] { "Modules > Native > Assets" }, "CalradiaForge"), "No visible Assets");
		ExpectRejected(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
			new[] { "Modules > CalradiaForge > Assets", "Sandbox > CalradiaForge > Assets" }, "CalradiaForge"), "More than one visible Assets path");
		ExpectRejected(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
			new[] { "Modules > CalradiaForge > Assets > Art > Assets" }, "CalradiaForge"), "No visible Assets");
		ExpectRejected(() => ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
			new[] { string.Empty, "Modules > CalradiaForge > Assets" }, "CalradiaForge"), "could not be read");
		string repeated = ResourceBrowserTargetResolver.ResolveUniqueAssetsPath(
			new[] { "Modules > CalradiaForge > Assets", "Modules > CalradiaForge > Assets" }, "CalradiaForge");
			Assert(repeated == "Modules > CalradiaForge > Assets", "Identical repeated UIA paths should collapse to one path.");

			Console.WriteLine("PASS: bounded batch policy, extension/filter/collision rules, command target requirements, and target path resolution.");
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	private static void TestCliReadOnlyModes()
	{
		CliResult help = InvokeCli(new[] { "--help" });
		Assert(help.ExitCode == 0 && help.StandardOutput.Contains("--inspect") && help.StandardOutput.Contains("--module-name"),
			"Help should expose read-only inspection and explicit module targeting.");

		string root = Path.Combine(Path.GetTempPath(), "Bannerlord CLI ñ " + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(root, "sample model.fbx"), "fixture");
			CliResult dryRun = InvokeCli(new[] { "--dry-run", root, "*.fbx" });
			Assert(dryRun.ExitCode == 0 && dryRun.StandardOutput.Contains("DRY RUN: no Editor windows were attached"),
				"Dry-run should complete through the read-only branch without attaching to UI Automation.");
			Assert(dryRun.StandardOutput.Contains("sample model.fbx"), "Dry-run should include the matching filename.");

			CliResult noModule = InvokeCli(new[] { root });
			Assert(noModule.ExitCode == 1 && noModule.StandardOutput.Contains("--module-name"),
				"A submission without a module name should stop at command validation.");

			CliResult empty = InvokeCli(new[] { "--dry-run", root, "*.png" });
			Assert(empty.ExitCode == 0 && empty.StandardOutput.Contains("Matched files: 0"), "An empty dry-run should report zero without touching UI.");

			CliResult missing = InvokeCli(new[] { "--dry-run", Path.Combine(root, "missing"), "*.fbx" });
			Assert(missing.ExitCode == 2 && missing.StandardError.Contains("Source folder was not found"),
				"A missing dry-run folder should fail with a clear error.");
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	private static CliResult InvokeCli(string[] args)
	{
		TextWriter originalOut = Console.Out;
		TextWriter originalError = Console.Error;
		using (var output = new StringWriter())
		using (var error = new StringWriter())
		{
			try
			{
				Console.SetOut(output);
				Console.SetError(error);
				return new CliResult(Program.Execute(args), output.ToString(), error.ToString());
			}
			finally
			{
				Console.SetOut(originalOut);
				Console.SetError(originalError);
			}
		}
	}

	private static void WriteFile(string folder, string name)
	{
		File.WriteAllText(Path.Combine(folder, name), "fixture");
	}

	private static void ExpectRejected(Action operation, string expectedMessage)
	{
		try
		{
			operation();
		}
		catch (Exception exception)
		{
			Assert(exception.Message.Contains(expectedMessage), "Rejection did not include expected reason: " + expectedMessage + ". Actual: " + exception.Message);
			return;
		}
		throw new InvalidOperationException("An invalid input was accepted; expected rejection: " + expectedMessage);
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}

	private sealed class CliResult
	{
		public CliResult(int exitCode, string standardOutput, string standardError)
		{
			ExitCode = exitCode;
			StandardOutput = standardOutput;
			StandardError = standardError;
		}

		public int ExitCode { get; private set; }
		public string StandardOutput { get; private set; }
		public string StandardError { get; private set; }
	}
}
