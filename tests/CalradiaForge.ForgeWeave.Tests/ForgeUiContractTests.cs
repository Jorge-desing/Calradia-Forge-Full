using System;
using System.IO;
using System.Linq;
using CalradiaForge.Core;
using CalradiaForge.Sdk;

namespace TaleWorlds.Library
{
    // Metadata-only Gauntlet ViewModel stub lets these SDK contract tests run without
    // booting or loading the game runtime.
    public abstract class ViewModel { }
}

namespace CalradiaForge.ForgeWeave.Tests
{
    internal static class ForgeUiContractTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("Gauntlet UI discovery accepts a valid owned ViewModel and prefab", ValidDiscovery);
            test("Gauntlet UI discovery rejects a missing command binding", MissingBinding);
            test("Gauntlet UI discovery rejects a non-Gauntlet ViewModel", WrongViewModel);
            test("Gauntlet UI discovery rejects duplicate command identifiers", DuplicateCommands);
            test("Gauntlet UI registry rejects duplicate pages and removes module owners", RegistryLifecycle);
            test("Gauntlet UI policy enforces page and command context", ContextGate);
            test("Gauntlet UI policy requires test mode and a campaign copy for writers", WriterGates);
        }

        static void ValidDiscovery()
        {
            WithFixture("<Prefab><Window><ButtonWidget Command.Click=\"ExecuteClose\" /></Window></Prefab>", path =>
            {
                var page = ForgeUiDiscovery.DescribeAt(typeof(ValidPage), "ForgeUiTests", path);
                Check(page.Id == "forge-ui.valid" && page.Owner == "ForgeUiTests" && page.Commands.Single().Binding == "ExecuteClose");
            });
        }

        static void MissingBinding()
        {
            WithFixture("<Prefab><Window><ButtonWidget Command.Click=\"OtherAction\" /></Window></Prefab>", path =>
                Throws<InvalidOperationException>(() => ForgeUiDiscovery.DescribeAt(typeof(ValidPage), "ForgeUiTests", path)));
        }

        static void WrongViewModel()
        {
            Throws<InvalidOperationException>(() => ForgeUiDiscovery.DescribeAt(typeof(PlainPage), "ForgeUiTests", "fixture.dll"));
        }

        static void DuplicateCommands()
        {
            WithFixture("<Prefab><Window><ButtonWidget Command.Click=\"ExecuteOne\" /><ButtonWidget Command.Click=\"ExecuteTwo\" /></Window></Prefab>", path =>
                Throws<InvalidOperationException>(() => ForgeUiDiscovery.DescribeAt(typeof(DuplicateCommandPage), "ForgeUiTests", path)));
        }

        static void RegistryLifecycle()
        {
            var page = new ForgeUiPageDescriptor { Id = "forge-ui.valid", Owner = "ForgeUiTests", Prefab = "Page", TitleKey = "title", Context = Context.Any, ViewModelType = typeof(ValidPage), Commands = Array.Empty<ForgeUiCommandDescriptor>() };
            var registry = new TestEngine();
            registry.Register(page);
            Check(registry.FindPage(page.Id) != null && registry.GetPages().Count == 1);
            Throws<ArgumentException>(() => registry.Register(page));
            Check(registry.RemoveOwner("ForgeUiTests") == 1 && registry.FindPage(page.Id) == null);
        }

        static void ContextGate()
        {
            var command = new ForgeUiCommandDescriptor { Id = "mission", Binding = "ExecuteClose", Context = Context.Mission, ChangesState = false };
            var page = NewPage(Context.Campaign, command);
            Check(ForgeUiPolicy.GetUnavailableReason(page, Context.Mission, false, false).Contains("requires Campaign"));
            page.Context = Context.Any;
            Check(ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, false, false).Contains("requires Mission"));
            Check(ForgeUiPolicy.GetUnavailableReason(page, Context.Mission, false, false) == null);
        }

        static void WriterGates()
        {
            var command = new ForgeUiCommandDescriptor { Id = "writer", Binding = "ExecuteClose", Context = Context.Campaign, ChangesState = true };
            var page = NewPage(Context.Any, command);
            Check(ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, false, false).Contains("test mode"));
            Check(ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, true, false).Contains("campaign copy"));
            Check(ForgeUiPolicy.GetUnavailableReason(page, Context.Campaign, true, true) == null);
        }

        static ForgeUiPageDescriptor NewPage(Context context, ForgeUiCommandDescriptor command) => new ForgeUiPageDescriptor
        {
            Id = "forge-ui.policy", Owner = "ForgeUiTests", Prefab = "Page", TitleKey = "title", Context = context,
            ViewModelType = typeof(ValidPage), Commands = new[] { command }
        };

        static void WithFixture(string prefabXml, Action<string> action)
        {
            var temp = Path.Combine(Path.GetTempPath(), "CalradiaForgeUiContracts-" + Guid.NewGuid().ToString("N"));
            var module = Path.Combine(temp, "ForgeUiTests");
            var prefab = Path.Combine(module, "GUI", "Prefabs", "Page.xml");
            var assembly = Path.Combine(module, "bin", "Win64_Shipping_Client", "fixture.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(prefab));
            Directory.CreateDirectory(Path.GetDirectoryName(assembly));
            File.WriteAllText(prefab, prefabXml);
            File.WriteAllBytes(assembly, new byte[] { 0x4D, 0x5A });
            try { action(assembly); }
            finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
        }

        static void Check(bool condition)
        {
            if (!condition) throw new Exception("Gauntlet UI contract assertion failed.");
        }

        static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        [ForgeUiPage("forge-ui.valid", "Page", "title", Context = Context.Any)]
        public sealed class ValidPage : TaleWorlds.Library.ViewModel
        {
            [ForgeUiCommand("close", "ExecuteClose", Context = Context.Any)]
            public void ExecuteClose() { }
        }

        [ForgeUiPage("forge-ui.plain", "Page", "title", Context = Context.Any)]
        public sealed class PlainPage { }

        [ForgeUiPage("forge-ui.duplicate", "Page", "title", Context = Context.Any)]
        public sealed class DuplicateCommandPage : TaleWorlds.Library.ViewModel
        {
            [ForgeUiCommand("duplicate", "ExecuteOne", Context = Context.Any)] public void ExecuteOne() { }
            [ForgeUiCommand("duplicate", "ExecuteTwo", Context = Context.Any)] public void ExecuteTwo() { }
        }
    }
}
