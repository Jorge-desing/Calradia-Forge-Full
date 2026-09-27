using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CalradiaForge.Sdk;

namespace CalradiaForge.Tests
{
    public static class ModSettingsPathSafetyTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("ModSettings path builder rejects path and alternate-stream syntax", RejectPathSyntax);
            test("ModSettings public entry points reject empty IDs before file access", RejectEmptyIds);
            test("ModSettings preserves safe IDs including spaces and Unicode", PreserveSafeIds);
        }

        private static void RejectPathSyntax()
        {
            var invalidIds = new[]
            {
                "../outside",
                @"..\outside",
                @"C:\outside",
                "C:outside",
                @"\\server\share",
                "folder/name",
                "settings:stream",
                "bad?.id",
                "bad\0id"
            };

            foreach (var modId in invalidIds)
            {
                try
                {
                    ResolvePath(modId);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                throw new Exception("Unsafe ModSettings ID was accepted: " + modId);
            }
        }

        private static void RejectEmptyIds()
        {
            AssertArgumentException(() => ModSettings.Register(" ", new object()));
            AssertArgumentException(() => ModSettings.Save("", new object()));
        }

        private static void PreserveSafeIds()
        {
            const string modId = "Calradia Forge_日本";
            var path = ResolvePath(modId);
            var expectedDirectory = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord",
                "Configs",
                "ModSettings"));

            if (!string.Equals(Path.GetDirectoryName(path), expectedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("A safe ModSettings ID did not resolve inside the configuration directory.");
            }

            if (!string.Equals(Path.GetFileName(path), modId + ".json", StringComparison.Ordinal))
            {
                throw new Exception("A safe ModSettings ID was unexpectedly changed.");
            }
        }

        private static string ResolvePath(string modId)
        {
            var method = typeof(ModSettings).GetMethod("GetSettingsPath", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                throw new Exception("ModSettings path resolver was not found.");
            }

            try
            {
                return (string)method.Invoke(null, new object[] { modId });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static void AssertArgumentException(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception("Expected an ArgumentException.");
        }
    }
}
