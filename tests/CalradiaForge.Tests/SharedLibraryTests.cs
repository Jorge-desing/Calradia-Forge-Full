using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using CalradiaForge.Sdk;

internal static class SharedLibraryTests
{
    interface IPrice { int Calculate(int count); }
    interface IOther { }
    sealed class Price : IPrice { public int Calculate(int count) => checked(count * 7); }
    static void Assert(bool value) { if (!value) throw new Exception("Shared library assertion failed"); }
    static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    static void RejectWith<T>(Action action, params string[] fragments) where T : Exception
    {
        try { action(); }
        catch (T error)
        {
            foreach (var fragment in fragments)
                Assert(error.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
            return;
        }
        throw new Exception("Expected " + typeof(T).Name);
    }

    static string WorkspaceRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "CalradiaForge.sln"))) return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Could not locate the Calradia Forge workspace root.");
    }

    static string[] ManifestDependencies(string path)
    {
        var document = new XmlDocument();
        document.Load(path);
        return document.SelectNodes("/Module/DependedModules/DependedModule/@Id")
            .Cast<XmlAttribute>().Select(attribute => attribute.Value).ToArray();
    }

    static string[] LinesContaining(string path, string text)
    {
        return File.ReadAllLines(path).Where(line => line.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
    }
    public static void Run(Action<string, Action> test)
    {
        test("SDK availability isolates subscriber failures and keeps services usable", () => {
            var expected=new InvalidOperationException("Broken extension startup");
            bool notified=false;
            Action<IForgeRegistry> broken=registry=>throw expected;
            Action<IForgeRegistry> healthy=registry=> {
                Assert(ReferenceEquals(registry,ForgeApi.Registry));
                ForgeApi.Libraries.OpenModule("Healthy").Provide<IPrice>("prices",new Version(1,0),new Price());
                notified=true;
            };
            ForgeApi.Available+=broken; ForgeApi.Available+=healthy;
            try {
                AggregateException failure=null;
                try { ForgeApi.Connect(new CalradiaForge.Core.TestEngine()); }
                catch(AggregateException error) { failure=error; }
                Assert(failure!=null && failure.InnerExceptions.Count==1 && ReferenceEquals(failure.InnerExceptions[0],expected));
                Assert(notified);
                Assert(ForgeApi.Libraries.OpenModule("Consumer").Require<IPrice>("Healthy","prices",new Version(1,0)).Use(p=>p.Calculate(3))==21);
            } finally { ForgeApi.Available-=broken; ForgeApi.Available-=healthy; ForgeApi.Disconnect(); }
        });
        test("SDK reconnect and disconnect revoke previous session services", () => {
            try {
                ForgeApi.Connect(new CalradiaForge.Core.TestEngine());
                var oldRegistry=ForgeApi.Libraries;
                var provider=oldRegistry.OpenModule("Provider");
                provider.Provide<IPrice>("prices",new Version(1,0),new Price());
                var old=oldRegistry.OpenModule("Consumer").Require<IPrice>("Provider","prices",new Version(1,0));
                Reject<ArgumentNullException>(()=>ForgeApi.Connect(null));
                Assert(old.Use(p=>p.Calculate(1))==7);
                ForgeApi.Connect(new CalradiaForge.Core.TestEngine());
                Assert(!ReferenceEquals(oldRegistry,ForgeApi.Libraries));
                Reject<ObjectDisposedException>(()=>old.Use(p=>p.Calculate(1)));
                Reject<InvalidOperationException>(()=>ForgeApi.Libraries.OpenModule("NewConsumer").Require<IPrice>("Provider","prices",new Version(1,0)));
                var current=ForgeApi.Libraries;
                ForgeApi.Disconnect(); ForgeApi.Disconnect();
                Assert(ForgeApi.Registry==null && ForgeApi.Libraries==null);
                Reject<ObjectDisposedException>(()=>current.OpenModule("AfterDisconnect"));
            } finally { ForgeApi.Disconnect(); }
        });
        test("Price provider and consumer share the exact CLR contract and revoke handles on unregister or unload", () => {
            var contract = typeof(CalradiaForge.PriceContracts.IPriceCalculator);
            var contractAssembly = contract.Assembly;
            var providerAssembly = typeof(CalradiaForge.PriceProvider.PriceCalculator).Assembly;
            var consumerAssembly = typeof(CalradiaForge.PriceConsumer.SharedPriceTest).Assembly;
            Assert(providerAssembly != consumerAssembly);
            Assert(typeof(CalradiaForge.PriceProvider.PriceCalculator).GetInterfaces().Single() == contract);
            var consumerContractReference = consumerAssembly.GetReferencedAssemblies().Single(name =>
                name.Name == contractAssembly.GetName().Name);
            Assert(consumerContractReference.FullName == contractAssembly.GetName().FullName);
            Assert(ReferenceEquals(System.Reflection.Assembly.Load(consumerContractReference), contractAssembly));

            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("CalradiaForgePriceProvider");
                var consumer=registry.OpenModule("CalradiaForgePriceConsumer");
                var monitor = consumer.Watch<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider", "prices", new Version(1, 0));
                Assert(monitor.Current.Status.ToString() == "ServiceMissing");
                var publication = provider.BeginPublication();
                publication.Add<CalradiaForge.PriceContracts.IPriceCalculator>("prices", new Version(1, 0), new CalradiaForge.PriceProvider.PriceCalculator());
                Assert(!consumer.Resolve<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider", "prices", new Version(1, 0)).IsAvailable);
                publication.Commit();
                var optionalResolution = consumer.Resolve<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider", "prices", new Version(1, 0));
                Assert(monitor.Current.IsAvailable && optionalResolution.TryGetService(out var resolvedPrice) && resolvedPrice.Use(prices => prices.CalculateTotal(7, 3)) == 21);
                var staleAfterUnregister = consumer.Require<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider","prices",new Version(1,0));
                var example=new CalradiaForge.PriceConsumer.SharedPriceTest(consumer);
                var execution=new TestExecution(null,148,System.Threading.CancellationToken.None);
                try { example.Prepare(execution);example.Execute(execution);example.Verify(execution); }
                finally { example.Cleanup(execution); }
                Assert(execution.Steps.Contains("shared total=21"));
                Assert(execution.Steps.Contains("Shared provider returned 21"));
                Assert(execution.Steps.Any(step => step.Contains("provider=CalradiaForgePriceProvider") && step.Contains("service=prices") && step.Contains("api=1.0")));
                publication.Dispose();
                Assert(monitor.Current.Status.ToString() == "ServiceMissing");
                Reject<ObjectDisposedException>(()=>staleAfterUnregister.Use(prices=>prices.CalculateTotal(7,1)));
                Reject<InvalidOperationException>(()=>consumer.Require<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider","prices",new Version(1,0)));
                monitor.Dispose();

                provider.Provide<CalradiaForge.PriceContracts.IPriceCalculator>("prices",new Version(1,0),new CalradiaForge.PriceProvider.PriceCalculator());
                var staleAfterProviderUnload = consumer.Require<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider","prices",new Version(1,0));
                provider.Dispose();
                Reject<ObjectDisposedException>(()=>staleAfterProviderUnload.Use(prices=>prices.CalculateTotal(7,1)));
                Reject<InvalidOperationException>(()=>consumer.Require<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider","prices",new Version(1,0)));

                var replacementProvider = registry.OpenModule("CalradiaForgePriceProvider");
                replacementProvider.Provide<CalradiaForge.PriceContracts.IPriceCalculator>("prices",new Version(1,0),new CalradiaForge.PriceProvider.PriceCalculator());
                var staleAfterRegistryDisconnect = consumer.Require<CalradiaForge.PriceContracts.IPriceCalculator>("CalradiaForgePriceProvider","prices",new Version(1,0));
                registry.Dispose();
                Reject<ObjectDisposedException>(()=>staleAfterRegistryDisconnect.Use(prices=>prices.CalculateTotal(7,1)));
            }
        });

        test("Price example manifests and packaging keep one provider-owned contract assembly", () => {
            var root = WorkspaceRoot();
            var providerManifest = Path.Combine(root, "modules", "CalradiaForgePriceProvider", "SubModule.xml");
            var consumerManifest = Path.Combine(root, "modules", "CalradiaForgePriceConsumer", "SubModule.xml");
            var providerDependencies = ManifestDependencies(providerManifest);
            var consumerDependencies = ManifestDependencies(consumerManifest);
            Assert(providerDependencies.Contains("CalradiaForge"));
            Assert(consumerDependencies.Contains("CalradiaForge"));
            Assert(consumerDependencies.Contains("CalradiaForgePriceProvider"));

            var providerProject = new XmlDocument();
            providerProject.Load(Path.Combine(root, "examples", "CalradiaForge.PriceProvider", "CalradiaForge.PriceProvider.csproj"));
            var consumerProject = new XmlDocument();
            consumerProject.Load(Path.Combine(root, "examples", "CalradiaForge.PriceConsumer", "CalradiaForge.PriceConsumer.csproj"));
            Assert(providerProject.SelectSingleNode("/Project/ItemGroup/ProjectReference[contains(@Include, 'PriceContracts.csproj')]") != null);
            var consumerContractReference = consumerProject.SelectSingleNode("/Project/ItemGroup/ProjectReference[contains(@Include, 'PriceContracts.csproj')]");
            Assert(consumerContractReference != null && consumerContractReference.Attributes["Private"] != null && consumerContractReference.Attributes["Private"].Value == "false");

            var providerOutput = Path.Combine(root, "examples", "CalradiaForge.PriceProvider", "bin", "Release", "net472");
            var consumerOutput = Path.Combine(root, "examples", "CalradiaForge.PriceConsumer", "bin", "Release", "net472");
            Assert(File.Exists(Path.Combine(providerOutput, "CalradiaForge.PriceProvider.dll")));
            Assert(File.Exists(Path.Combine(providerOutput, "CalradiaForge.PriceContracts.dll")));
            Assert(File.Exists(Path.Combine(consumerOutput, "CalradiaForge.PriceConsumer.dll")));
            Assert(!File.Exists(Path.Combine(consumerOutput, "CalradiaForge.PriceContracts.dll")));

            var packageScript = Path.Combine(root, "tools", "package.ps1");
            var packageContractCopies = LinesContaining(packageScript, "CalradiaForge.PriceContracts.dll");
            Assert(packageContractCopies.Length == 1 && packageContractCopies[0].Contains("$providerBin") && !packageContractCopies[0].Contains("$consumerBin"));
            var deployScript = Path.Combine(root, "tools", "deploy_to_game.ps1");
            var deployContractCopies = LinesContaining(deployScript, "CalradiaForge.PriceContracts.dll");
            Assert(deployContractCopies.Length == 1 && deployContractCopies[0].Contains("Target = 'CalradiaForgePriceProvider\\bin\\Win64_Shipping_Client\\CalradiaForge.PriceContracts.dll'") && !deployContractCopies[0].Contains("CalradiaForgePriceConsumer"));
        });
        test("Shared library resolves typed compatible provider", () => {
            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("Economy"); var consumer=registry.OpenModule("Trade");
                provider.Provide<IPrice>("prices",new Version(1,2),new Price());
                Assert(consumer.Require<IPrice>("economy","PRICES",new Version(1,0,0)).Use(p=>p.Calculate(3))==21);
            }
        });

        test("Shared library Resolve, Require, and notification scaling report benchmarks", TestSharedLibraryPerformanceBenchmarks);

        test("Shared library Resolve exposes statuses without weakening Require", () => {
            using (var registry = new SharedLibraryRegistry())
            {
                var consumer = registry.OpenModule("Trade");
                var missingProvider = consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0));
                Assert(missingProvider.Status.ToString() == "ProviderMissing");
                Assert(!missingProvider.IsAvailable && !string.IsNullOrWhiteSpace(missingProvider.Diagnostic));
                Assert(!missingProvider.TryGetService(out var absentProviderHandle) && absentProviderHandle == null);
                Reject<InvalidOperationException>(() => consumer.Require<IPrice>("Economy", "prices", new Version(1, 0)));

                var provider = registry.OpenModule("Economy");
                var missingService = consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0));
                Assert(missingService.Status.ToString() == "ServiceMissing");
                Assert(!missingService.IsAvailable && !missingService.TryGetService(out var absentServiceHandle) && absentServiceHandle == null);
                Reject<InvalidOperationException>(() => consumer.Require<IPrice>("Economy", "prices", new Version(1, 0)));

                provider.Provide<IPrice>("prices", new Version(1, 2), new Price());
                var contractMismatch = consumer.Resolve<IOther>("Economy", "prices", new Version(1, 0));
                Assert(contractMismatch.Status.ToString() == "ContractMismatch");
                Assert(!contractMismatch.IsAvailable && !contractMismatch.TryGetService(out var mismatchedHandle) && mismatchedHandle == null);
                Reject<InvalidOperationException>(() => consumer.Require<IOther>("Economy", "prices", new Version(1, 0)));

                var incompatible = consumer.Resolve<IPrice>("Economy", "prices", new Version(2, 0));
                Assert(incompatible.Status.ToString() == "IncompatibleVersion");
                Assert(!incompatible.IsAvailable && !incompatible.TryGetService(out var incompatibleHandle) && incompatibleHandle == null);
                Reject<InvalidOperationException>(() => consumer.Require<IPrice>("Economy", "prices", new Version(2, 0)));

                var available = consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0));
                Assert(available.Status.ToString() == "Available" && available.IsAvailable);
                Assert(available.TryGetService(out var handle) && handle != null && handle.Use(price => price.Calculate(3)) == 21);
                Assert(consumer.Require<IPrice>("Economy", "prices", new Version(1, 0)).Use(price => price.Calculate(3)) == 21);
            }
        });

        test("Shared service batch commits and withdraws its complete publication as one lease", () => {
            using (var registry = new SharedLibraryRegistry())
            {
                var provider = registry.OpenModule("Economy");
                var consumer = registry.OpenModule("Trade");
                var priceWatch = consumer.Watch<IPrice>("Economy", "prices", new Version(1, 0));
                var taxWatch = consumer.Watch<IPrice>("Economy", "taxes", new Version(1, 0));
                Assert(priceWatch.Current.Status.ToString() == "ServiceMissing");
                Assert(taxWatch.Current.Status.ToString() == "ServiceMissing");
                var priceEvents = 0;
                var taxEvents = 0;
                var observedCompleteCommit = false;
                priceWatch.Changed += (sender, args) =>
                {
                    priceEvents++;
                    if (args.Current.IsAvailable)
                        observedCompleteCommit = taxWatch.Current.IsAvailable;
                };
                taxWatch.Changed += (sender, args) => taxEvents++;

                var batch = provider.BeginPublication();
                batch.Add<IPrice>("prices", new Version(1, 0), new Price());
                batch.Add<IPrice>("taxes", new Version(1, 0), new Price());
                Assert(!consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0)).IsAvailable);
                Assert(!consumer.Resolve<IPrice>("Economy", "taxes", new Version(1, 0)).IsAvailable);
                batch.Commit();
                Assert(priceWatch.Current.IsAvailable && taxWatch.Current.IsAvailable);
                Assert(observedCompleteCommit);
                Assert(priceEvents == 1 && taxEvents == 1);
                var stalePrice = consumer.Require<IPrice>("Economy", "prices", new Version(1, 0));
                var staleTax = consumer.Require<IPrice>("Economy", "taxes", new Version(1, 0));

                batch.Dispose();
                Assert(priceWatch.Current.Status.ToString() == "ServiceMissing");
                Assert(taxWatch.Current.Status.ToString() == "ServiceMissing");
                Assert(priceEvents == 2 && taxEvents == 2);
                Reject<ObjectDisposedException>(() => stalePrice.Use(price => price.Calculate(1)));
                Reject<ObjectDisposedException>(() => staleTax.Use(price => price.Calculate(1)));
                priceWatch.Dispose();
                taxWatch.Dispose();
            }
        });

        test("Shared service batch rejects empty and duplicate publications without partial state", () => {
            using (var registry = new SharedLibraryRegistry())
            {
                var provider = registry.OpenModule("Economy");
                var consumer = registry.OpenModule("Trade");
                var emptyBatch = provider.BeginPublication();
                Reject<InvalidOperationException>(() => emptyBatch.Commit());
                emptyBatch.Dispose();
                Assert(consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0)).Status.ToString() == "ServiceMissing");

                var duplicateStaged = provider.BeginPublication();
                duplicateStaged.Add<IPrice>("prices", new Version(1, 0), new Price());
                duplicateStaged.Add<IPrice>("taxes", new Version(1, 0), new Price());
                var stagedDuplicateRejected = false;
                try
                {
                    duplicateStaged.Add<IPrice>("prices", new Version(1, 0), new Price());
                    duplicateStaged.Commit();
                }
                catch (InvalidOperationException) { stagedDuplicateRejected = true; }
                finally { duplicateStaged.Dispose(); }
                Assert(stagedDuplicateRejected);
                Assert(consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0)).Status.ToString() == "ServiceMissing");
                Assert(consumer.Resolve<IPrice>("Economy", "taxes", new Version(1, 0)).Status.ToString() == "ServiceMissing");

                var existingRegistration = provider.Provide<IPrice>("existing", new Version(1, 0), new Price());
                var existingHandle = consumer.Require<IPrice>("Economy", "existing", new Version(1, 0));
                var duplicateExisting = provider.BeginPublication();
                duplicateExisting.Add<IPrice>("staged", new Version(1, 0), new Price());
                var duplicateRejected = false;
                try
                {
                    duplicateExisting.Add<IPrice>("existing", new Version(1, 0), new Price());
                    duplicateExisting.Commit();
                }
                catch (InvalidOperationException) { duplicateRejected = true; }
                finally { duplicateExisting.Dispose(); }
                Assert(duplicateRejected);
                Assert(consumer.Resolve<IPrice>("Economy", "staged", new Version(1, 0)).Status.ToString() == "ServiceMissing");
                Assert(existingHandle.Use(price => price.Calculate(3)) == 21);
                existingRegistration.Dispose();

                var discarded = provider.BeginPublication();
                discarded.Add<IPrice>("discarded", new Version(1, 0), new Price());
                discarded.Dispose();
                Assert(consumer.Resolve<IPrice>("Economy", "discarded", new Version(1, 0)).Status.ToString() == "ServiceMissing");
            }
        });

        test("Shared service watch queues reentrant withdrawal and republish snapshots", () => {
            using (var registry = new SharedLibraryRegistry())
            {
                var provider = registry.OpenModule("Economy");
                var consumer = registry.OpenModule("Trade");
                var firstWatch = consumer.Watch<IPrice>("Economy", "prices", new Version(1, 0));
                var secondWatch = consumer.Watch<IPrice>("Economy", "prices", new Version(1, 0));
                var callbackDepth = 0;
                var maximumCallbackDepth = 0;
                var reentrantMutationPerformed = false;
                var observedNewerCurrent = false;
                var snapshots = new System.Collections.Generic.List<string>();
                SharedService<IPrice> withdrawnHandle = null;
                SharedService<IPrice> replacementHandle = null;

                EventHandler<SharedServiceChangedEventArgs<IPrice>> firstHandler = (sender, args) =>
                {
                    callbackDepth++;
                    maximumCallbackDepth = Math.Max(maximumCallbackDepth, callbackDepth);
                    try
                    {
                        if (!reentrantMutationPerformed && args.Current.IsAvailable)
                        {
                            reentrantMutationPerformed = true;
                            Assert(args.Current.TryGetService(out withdrawnHandle) && withdrawnHandle != null);
                            provider.Dispose();
                            var replacementProvider = registry.OpenModule("Economy");
                            replacementProvider.Provide<IPrice>("prices", new Version(1, 0), new Price());
                            replacementHandle = consumer.Require<IPrice>("Economy", "prices", new Version(1, 0));
                            Assert(!ReferenceEquals(withdrawnHandle, replacementHandle));
                        }
                    }
                    finally { callbackDepth--; }
                };

                EventHandler<SharedServiceChangedEventArgs<IPrice>> secondHandler = (sender, args) =>
                {
                    callbackDepth++;
                    maximumCallbackDepth = Math.Max(maximumCallbackDepth, callbackDepth);
                    try
                    {
                        snapshots.Add(args.Previous.Status + "->" + args.Current.Status);
                        if (args.Current.IsAvailable && reentrantMutationPerformed && secondWatch.Current.IsAvailable)
                        {
                            Assert(args.Current.TryGetService(out var queuedHandle) && queuedHandle != null);
                            Assert(secondWatch.Current.TryGetService(out var latestHandle) && latestHandle != null);
                            observedNewerCurrent |= !ReferenceEquals(queuedHandle, latestHandle);
                        }
                    }
                    finally { callbackDepth--; }
                };
                firstWatch.Changed += firstHandler;
                secondWatch.Changed += secondHandler;

                provider.Provide<IPrice>("prices", new Version(1, 0), new Price());

                Assert(reentrantMutationPerformed);
                Assert(maximumCallbackDepth == 1);
                Assert(observedNewerCurrent);
                Assert(snapshots.SequenceEqual(new[]
                {
                    "ServiceMissing->Available",
                    "Available->ProviderMissing",
                    "ProviderMissing->ServiceMissing",
                    "ServiceMissing->Available"
                }));
                Assert(firstWatch.Current.IsAvailable && secondWatch.Current.IsAvailable);
                Reject<ObjectDisposedException>(() => withdrawnHandle.Use(price => price.Calculate(1)));
                Assert(replacementHandle.Use(price => price.Calculate(1)) == 7);
                firstWatch.Dispose();
                secondWatch.Dispose();
            }
        });

        test("Shared service watch reports snapshots, generations, isolated reentrant callbacks and disconnect cleanup", () => {
            var ownerThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            using (var registry = new SharedLibraryRegistry())
            {
                var consumer = registry.OpenModule("Trade");
                var monitor = consumer.Watch<IPrice>("Economy", "prices", new Version(1, 0));
                Assert(monitor.Current.Status.ToString() == "ProviderMissing");
                var callbackCount = 0;
                var callbackThread = 0;
                var transitions = new System.Collections.Generic.List<string>();
                var reentrantResolutionAvailable = true;
                EventHandler<SharedServiceChangedEventArgs<IPrice>> faulty = (sender, args) => throw new InvalidOperationException("watch handler failed");
                EventHandler<SharedServiceChangedEventArgs<IPrice>> secondFaulty = (sender, args) => throw new ArgumentException("second watch handler failed");
                EventHandler<SharedServiceChangedEventArgs<IPrice>> reentrant = (sender, args) =>
                {
                    callbackCount++;
                    callbackThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    transitions.Add(args.Previous.Status + "->" + args.Current.Status);
                    Assert(monitor.Current.Status == args.Current.Status);
                    var live = consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0));
                    reentrantResolutionAvailable &= live.IsAvailable == args.Current.IsAvailable;
                    if (live.TryGetService(out var callbackHandle) && callbackHandle != null)
                        Assert(callbackHandle.Use(price => price.Calculate(1)) == 7);
                };
                monitor.Changed += faulty;
                monitor.Changed += secondFaulty;
                monitor.Changed += reentrant;
                Assert(callbackCount == 0); // Creating a watch establishes only its initial snapshot.

                var provider = registry.OpenModule("Economy");
                Assert(callbackCount == 1 && transitions[0] == "ProviderMissing->ServiceMissing");
                Assert(callbackThread == ownerThread && reentrantResolutionAvailable);
                Assert(monitor.LastNotificationError is AggregateException);
                var aggregate = (AggregateException)monitor.LastNotificationError;
                Assert(aggregate.InnerExceptions.Count == 2 && aggregate.InnerExceptions.Any(error => error.Message.Contains("watch handler failed")) && aggregate.InnerExceptions.Any(error => error.Message.Contains("second watch handler failed")));
                monitor.Changed -= faulty;
                monitor.Changed -= secondFaulty;

                var firstRegistration = provider.Provide<IPrice>("prices", new Version(1, 0), new Price());
                Assert(callbackCount == 2 && transitions[1] == "ServiceMissing->Available");
                Assert(monitor.LastNotificationError == null);
                var firstGeneration = consumer.Require<IPrice>("Economy", "prices", new Version(1, 0));
                firstRegistration.Dispose();
                Assert(callbackCount == 3 && transitions[2] == "Available->ServiceMissing");
                var secondRegistration = provider.Provide<IPrice>("prices", new Version(1, 0), new Price());
                Assert(callbackCount == 4 && transitions[3] == "ServiceMissing->Available");
                var secondGeneration = consumer.Require<IPrice>("Economy", "prices", new Version(1, 0));
                Assert(!ReferenceEquals(firstGeneration, secondGeneration));
                Reject<ObjectDisposedException>(() => firstGeneration.Use(price => price.Calculate(1)));
                Assert(secondGeneration.Use(price => price.Calculate(1)) == 7);

                var backgroundResolutionRejected = Task.Run(() => Reject<InvalidOperationException>(() => consumer.Resolve<IPrice>("Economy", "prices", new Version(1, 0))));
                Assert(backgroundResolutionRejected.Wait(5000));
                backgroundResolutionRejected.GetAwaiter().GetResult();
                var backgroundSnapshotRejected = Task.Run(() => Reject<InvalidOperationException>(() => { var status = monitor.Current.Status.ToString(); }));
                Assert(backgroundSnapshotRejected.Wait(5000));
                backgroundSnapshotRejected.GetAwaiter().GetResult();
                var backgroundWatchRejected = Task.Run(() => Reject<InvalidOperationException>(() => consumer.Watch<IPrice>("Economy", "prices", new Version(1, 0))));
                Assert(backgroundWatchRejected.Wait(5000));
                backgroundWatchRejected.GetAwaiter().GetResult();
                var batch = provider.BeginPublication();
                batch.Add<IPrice>("thread-check", new Version(1, 0), new Price());
                var backgroundAddRejected = Task.Run(() => Reject<InvalidOperationException>(() => batch.Add<IPrice>("background", new Version(1, 0), new Price())));
                Assert(backgroundAddRejected.Wait(5000));
                backgroundAddRejected.GetAwaiter().GetResult();
                var backgroundCommitRejected = Task.Run(() => Reject<InvalidOperationException>(() => batch.Commit()));
                Assert(backgroundCommitRejected.Wait(5000));
                backgroundCommitRejected.GetAwaiter().GetResult();
                Assert(consumer.Resolve<IPrice>("Economy", "thread-check", new Version(1, 0)).Status.ToString() == "ServiceMissing");
                batch.Dispose();

                secondRegistration.Dispose();
                Assert(callbackCount == 5 && transitions[4] == "Available->ServiceMissing");
                var providerUnload = provider.Provide<IPrice>("prices", new Version(1, 0), new Price());
                var beforeUnloadCount = callbackCount;
                provider.Dispose();
                Assert(callbackCount == beforeUnloadCount + 1 && transitions[callbackCount - 1] == "Available->ProviderMissing");
                Assert(monitor.Current.Status.ToString() == "ProviderMissing");
                providerUnload.Dispose();
                monitor.Dispose();
            }

            try
            {
                ForgeApi.Connect(new CalradiaForge.Core.TestEngine());
                var consumer = ForgeApi.Libraries.OpenModule("DisconnectConsumer");
                var monitor = consumer.Watch<IPrice>("DisconnectProvider", "prices", new Version(1, 0));
                var disconnectEvents = 0;
                monitor.Changed += (sender, args) => disconnectEvents++;
                ForgeApi.Disconnect();
                Assert(disconnectEvents == 0);
                Reject<ObjectDisposedException>(() => { var status = monitor.Current.Status; });
                Reject<ObjectDisposedException>(() => { var error = monitor.LastNotificationError; });
            }
            finally { ForgeApi.Disconnect(); }
        });

        test("Shared library rejects missing versions and contract mismatches", () => {
            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("Economy"); var consumer=registry.OpenModule("Trade");
                provider.Provide<IPrice>("prices",new Version(1,2),new Price());
                RejectWith<InvalidOperationException>(()=>consumer.Require<IPrice>("Absent","prices",new Version(1,0)), "Absent", "prices", "minimum API");
                RejectWith<InvalidOperationException>(()=>consumer.Require<IPrice>("Economy","missing",new Version(1,0)), "Economy", "missing", "minimum API");
                RejectWith<InvalidOperationException>(()=>consumer.Require<IPrice>("Economy","prices",new Version(2,0)), "Economy", "prices", "provider offers 1.2", "requires major 2 and at least 2.0", "Align the provider's published API version");
                RejectWith<InvalidOperationException>(()=>consumer.Require<IPrice>("Economy","prices",new Version(1,3)), "Economy", "prices", "provider offers 1.2", "requires major 1 and at least 1.3", "Align the provider's published API version");
                RejectWith<InvalidOperationException>(()=>consumer.Require<IOther>("Economy","prices",new Version(1,0)), "Economy", "prices", typeof(IPrice).FullName, typeof(IOther).FullName, "publishes", "requests", "API 1.2", "minimum API 1.0", "same shared contract assembly");
            }
        });
        test("Shared library rejects duplicate owner and service IDs", () => {
            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("Economy");
                Reject<InvalidOperationException>(()=>registry.OpenModule("ECONOMY"));
                provider.Provide<IPrice>("prices",new Version(1,0),new Price());
                Reject<InvalidOperationException>(()=>provider.Provide<IPrice>("Prices",new Version(1,0),new Price()));
                Reject<ArgumentException>(()=>provider.Provide<Price>("concrete",new Version(1,0),new Price()));
                Reject<ArgumentException>(()=>provider.Provide<IPrice>("zero",new Version(0,1),new Price()));
            }
        });
        test("Shared library invalidates stale handles after replacement", () => {
            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("Economy"); var consumer=registry.OpenModule("Trade");
                var registration=provider.Provide<IPrice>("prices",new Version(1,0),new Price());
                var old=consumer.Require<IPrice>("Economy","prices",new Version(1,0));
                registration.Dispose(); provider.Provide<IPrice>("prices",new Version(1,1),new Price());
                Reject<ObjectDisposedException>(()=>old.Use(p=>p.Calculate(1)));
                Assert(consumer.Require<IPrice>("Economy","prices",new Version(1,0)).Use(p=>p.Calculate(1))==7);
            }
        });
        test("Shared library enforces consumer and registry lifetime", () => {
            var registry=new SharedLibraryRegistry();var provider=registry.OpenModule("Economy");var consumer=registry.OpenModule("Trade");
            provider.Provide<IPrice>("prices",new Version(1,0),new Price());
            var handle=consumer.Require<IPrice>("Economy","prices",new Version(1,0));
            consumer.Dispose();Reject<ObjectDisposedException>(()=>handle.Use(p=>p.Calculate(1)));
            registry.Dispose();registry.Dispose();Reject<ObjectDisposedException>(()=>registry.OpenModule("New"));
        });
        test("Shared library provider unload revokes existing handles", () => {
            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("Economy");var consumer=registry.OpenModule("Trade");
                provider.Provide<IPrice>("prices",new Version(1,0),new Price());
                var old=consumer.Require<IPrice>("Economy","prices",new Version(1,0));provider.Dispose();
                registry.OpenModule("Economy").Provide<IPrice>("prices",new Version(1,0),new Price());
                Reject<ObjectDisposedException>(()=>old.Use(p=>p.Calculate(1)));
            }
        });
        test("Shared library blocks background access and preserves provider errors", () => {
            using(var registry=new SharedLibraryRegistry()) {
                var provider=registry.OpenModule("Economy");var consumer=registry.OpenModule("Trade");
                provider.Provide<IPrice>("prices",new Version(1,0),new Price());
                var handle=consumer.Require<IPrice>("Economy","prices",new Version(1,0));
                Task.Run(()=>Reject<InvalidOperationException>(()=>handle.Use(p=>p.Calculate(1)))).GetAwaiter().GetResult();
                Reject<OverflowException>(()=>handle.Use(p=>p.Calculate(int.MaxValue)));
                Assert(handle.Use(p=>p.Calculate(1))==7);
            }
        });
    }

    private static void TestSharedLibraryPerformanceBenchmarks()
    {
        const int queryCount = 4096;
        var minimumVersion = new Version(1, 0);
        using (var registry = new SharedLibraryRegistry())
        {
            var provider = registry.OpenModule("BenchmarkProvider");
            var consumer = registry.OpenModule("BenchmarkConsumer");
            provider.Provide<IPrice>("prices", minimumVersion, new Price());

            // Warm both public paths before timing. Each result is checked so the workload also
            // guards the expected status and fail-fast success contract.
            for (var i = 0; i < 64; i++)
            {
                Assert(consumer.Resolve<IPrice>("BenchmarkProvider", "prices", minimumVersion).IsAvailable);
                Assert(consumer.Require<IPrice>("BenchmarkProvider", "prices", minimumVersion) != null);
            }

            var resolveWatch = Stopwatch.StartNew();
            for (var i = 0; i < queryCount; i++)
            {
                var result = consumer.Resolve<IPrice>("BenchmarkProvider", "prices", minimumVersion);
                Assert(result.Status == SharedServiceResolutionStatus.Available && result.TryGetService(out var handle) && handle != null);
            }
            resolveWatch.Stop();

            var requireWatch = Stopwatch.StartNew();
            for (var i = 0; i < queryCount; i++)
            {
                var handle = consumer.Require<IPrice>("BenchmarkProvider", "prices", minimumVersion);
                Assert(handle != null);
            }
            requireWatch.Stop();

            var missingWatch = Stopwatch.StartNew();
            for (var i = 0; i < queryCount; i++)
                Assert(consumer.Resolve<IPrice>("MissingBenchmarkProvider", "prices", minimumVersion).Status == SharedServiceResolutionStatus.ProviderMissing);
            missingWatch.Stop();

            Console.WriteLine("PERF SharedLibrary.Resolve state=Available operations=" + queryCount +
                " total_ms=" + resolveWatch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) +
                " us_per_op=" + (resolveWatch.Elapsed.TotalMilliseconds * 1000d / queryCount).ToString("F3", CultureInfo.InvariantCulture));
            Console.WriteLine("PERF SharedLibrary.Require state=Available operations=" + queryCount +
                " total_ms=" + requireWatch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) +
                " us_per_op=" + (requireWatch.Elapsed.TotalMilliseconds * 1000d / queryCount).ToString("F3", CultureInfo.InvariantCulture));
            Console.WriteLine("PERF SharedLibrary.Resolve state=ProviderMissing operations=" + queryCount +
                " total_ms=" + missingWatch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) +
                " us_per_op=" + (missingWatch.Elapsed.TotalMilliseconds * 1000d / queryCount).ToString("F3", CultureInfo.InvariantCulture));
        }

        foreach (var monitorCount in new[] { 0, 1, 100, 1000 })
            BenchmarkSharedLibraryNotificationScale(monitorCount, 20, minimumVersion);
    }

    private static void BenchmarkSharedLibraryNotificationScale(int monitorCount, int cycles, Version minimumVersion)
    {
        using (var registry = new SharedLibraryRegistry())
        {
            var provider = registry.OpenModule("NotificationProvider");
            var consumer = registry.OpenModule("NotificationConsumer");
            var monitors = new List<SharedServiceMonitor<IPrice>>(monitorCount);
            var callbackCount = 0;
            var availableTransitions = 0;
            var unavailableTransitions = 0;
            var unexpectedTransition = false;
            EventHandler<SharedServiceChangedEventArgs<IPrice>> handler = (sender, args) =>
            {
                callbackCount++;
                if (args.Previous.Status == SharedServiceResolutionStatus.ServiceMissing &&
                    args.Current.Status == SharedServiceResolutionStatus.Available)
                    availableTransitions++;
                else if (args.Previous.Status == SharedServiceResolutionStatus.Available &&
                    args.Current.Status == SharedServiceResolutionStatus.ServiceMissing)
                    unavailableTransitions++;
                else
                    unexpectedTransition = true;
            };

            for (var i = 0; i < monitorCount; i++)
            {
                var monitor = consumer.Watch<IPrice>("NotificationProvider", "prices", minimumVersion);
                Assert(monitor.Current.Status == SharedServiceResolutionStatus.ServiceMissing);
                monitor.Changed += handler;
                monitors.Add(monitor);
            }

            // Warm the same add/remove cycle, then exclude it from the reported callback totals.
            var warmup = provider.Provide<IPrice>("prices", minimumVersion, new Price());
            warmup.Dispose();
            callbackCount = 0;
            availableTransitions = 0;
            unavailableTransitions = 0;
            unexpectedTransition = false;

            var watch = Stopwatch.StartNew();
            for (var i = 0; i < cycles; i++)
            {
                var registration = provider.Provide<IPrice>("prices", minimumVersion, new Price());
                registration.Dispose();
            }
            watch.Stop();

            var expectedTransitions = monitorCount * cycles;
            Assert(!unexpectedTransition);
            Assert(availableTransitions == expectedTransitions && unavailableTransitions == expectedTransitions);
            Assert(callbackCount == expectedTransitions * 2);
            Console.WriteLine("PERF SharedLibrary.Notifications monitors=" + monitorCount +
                " cycles=" + cycles +
                " callbacks=" + callbackCount +
                " registry_changes=" + (cycles * 2) +
                " total_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) +
                " ms_per_change=" + (watch.Elapsed.TotalMilliseconds / (cycles * 2)).ToString("F3", CultureInfo.InvariantCulture));

            foreach (var monitor in monitors) monitor.Dispose();
        }
    }
}
