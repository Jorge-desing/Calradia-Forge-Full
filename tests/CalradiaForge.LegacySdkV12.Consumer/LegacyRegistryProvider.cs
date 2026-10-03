using System;
using System.Collections.Generic;
using CalradiaForge.Sdk;

namespace CalradiaForge.LegacySdkV12
{
    public sealed class LegacyRegistryProvider : IForgeRegistry
    {
        public void Register(ITestCase test) { }
        public void Register(ICommand command) { }
        public void Register(IDiagnosticProvider provider) { }

        public int GetCompiledForgeApiVersion() => ForgeApi.Version;

        public void RegisterLegacyContracts(IForgeRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            registry.Register(new LegacyTestCase());
            registry.Register(new LegacyCommand());
            registry.Register(new LegacyDiagnosticProvider());
        }

        public ITestServices CreateTestServices() => new LegacyTestServices();

        sealed class LegacyTestCase : ITestCase
        {
            public Descriptor Descriptor => new Descriptor
            {
                Id = "legacy.v12.test",
                Module = "legacy-v12-fixture",
                Name = "Legacy SDK v12 test",
                Context = Context.Any
            };

            public void Prepare(TestExecution execution) { }
            public void Execute(TestExecution execution) { }
            public void Verify(TestExecution execution) { }
            public void Cleanup(TestExecution execution) { }
        }

        sealed class LegacyCommand : ICommand
        {
            public Descriptor Descriptor => new Descriptor
            {
                Id = "legacy.v12.command",
                Module = "legacy-v12-fixture",
                Name = "Legacy SDK v12 command",
                Context = Context.Any
            };

            public string Execute(TestExecution execution, string argument) => "legacy-v12-command";
        }

        sealed class LegacyDiagnosticProvider : IDiagnosticProvider
        {
            public Descriptor Descriptor => new Descriptor
            {
                Id = "legacy.v12.diagnostic",
                Module = "legacy-v12-fixture",
                Name = "Legacy SDK v12 diagnostic",
                Context = Context.Any
            };

            public IEnumerable<Finding> Inspect(TestExecution execution)
            {
                return new[] { new Finding { Level = "Info", Code = "legacy-v12-provider", Module = "legacy-v12-fixture" } };
            }
        }

        sealed class LegacyTestServices : ITestServices
        {
            public Context CurrentContext => Context.Any;
            public bool IsCampaignActive => false;
            public object GetService(Type type) => null;
            public void Register(string module, string level, string message) { }
        }
    }
}
