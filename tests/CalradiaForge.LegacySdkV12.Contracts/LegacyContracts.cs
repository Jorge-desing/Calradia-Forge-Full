using System;
using System.Collections.Generic;
using System.Threading;

namespace CalradiaForge.Sdk
{
    // Exact v12 identities and members needed to compile an old consumer binary.
    // Keep this contract aligned with the pre-v13 SDK without referencing current SDK source.
    public enum Context { Any, Campaign, Mission }

    public sealed class Descriptor
    {
        public string Id { get; set; }
        public string Module { get; set; }
        public string Name { get; set; }
        public Context Context { get; set; }
        public bool ChangesState { get; set; }
    }

    public sealed class Finding
    {
        public string Level { get; set; }
        public string Code { get; set; }
        public string Module { get; set; }
        public string File { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
    }

    public interface ITestServices
    {
        Context CurrentContext { get; }
        bool IsCampaignActive { get; }
        object GetService(Type type);
        void Register(string module, string level, string message);
    }

    public sealed class TestExecution
    {
        public ITestServices Services { get; }
        public CancellationToken Cancellation { get; }
        public int Seed { get; }
        public Random Random { get; }
        public List<string> Steps { get; } = new List<string>();

        public TestExecution(ITestServices services, int seed, CancellationToken cancellation)
        {
            Services = services;
            Seed = seed;
            Random = new Random(seed);
            Cancellation = cancellation;
        }

        public void Verify(bool condition, string description)
        {
            Cancellation.ThrowIfCancellationRequested();
            Steps.Add(description);
            if (!condition) throw new InvalidOperationException(description);
        }
    }

    public interface ITestCase
    {
        Descriptor Descriptor { get; }
        void Prepare(TestExecution execution);
        void Execute(TestExecution execution);
        void Verify(TestExecution execution);
        void Cleanup(TestExecution execution);
    }

    public interface ICommand
    {
        Descriptor Descriptor { get; }
        string Execute(TestExecution execution, string argument);
    }

    public interface IDiagnosticProvider
    {
        Descriptor Descriptor { get; }
        IEnumerable<Finding> Inspect(TestExecution execution);
    }

    public interface IForgeRegistry
    {
        void Register(ITestCase test);
        void Register(ICommand command);
        void Register(IDiagnosticProvider provider);
    }

    public static class ForgeApi
    {
        public const int Version = 12;
    }
}
