using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace CalradiaForge.Sdk
{
    /// <summary>Declares a standalone Gauntlet prefab that Forge can open as an overlay.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ForgeUiPageAttribute : Attribute
    {
        public string Id { get; }
        public string Prefab { get; }
        public string TitleKey { get; }
        public Context Context { get; set; } = Context.Any;

        public ForgeUiPageAttribute(string id, string prefab, string titleKey)
        { Id = id; Prefab = prefab; TitleKey = titleKey; }
    }

    /// <summary>Declares a parameterless Gauntlet command method on a Forge UI page ViewModel.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ForgeUiCommandAttribute : Attribute
    {
        public string Id { get; }
        public string Binding { get; }
        public Context Context { get; set; } = Context.Any;
        public bool ChangesState { get; set; }

        public ForgeUiCommandAttribute(string id, string binding)
        { Id = id; Binding = binding; }
    }

    public sealed class ForgeUiCommandDescriptor
    {
        public string Id { get; internal set; }
        public string Binding { get; internal set; }
        public Context Context { get; internal set; }
        public bool ChangesState { get; internal set; }
        public MethodInfo Method { get; internal set; }
    }

    public sealed class ForgeUiPageDescriptor
    {
        public string Id { get; internal set; }
        public string Owner { get; internal set; }
        public string Prefab { get; internal set; }
        public string TitleKey { get; internal set; }
        public Context Context { get; internal set; }
        public Type ViewModelType { get; internal set; }
        public string PrefabPath { get; internal set; }
        public IReadOnlyList<ForgeUiCommandDescriptor> Commands { get; internal set; }

        public object CreateViewModel()
        {
            if (ViewModelType == null || ViewModelType.IsAbstract || ViewModelType.ContainsGenericParameters)
                throw new InvalidOperationException("The registered UI ViewModel type is unavailable.");
            return Activator.CreateInstance(ViewModelType);
        }
    }

    /// <summary>Read-only catalog and registration surface for standalone extension pages.</summary>
    public interface IForgeUiRegistry
    {
        void Register(ForgeUiPageDescriptor page);
        IReadOnlyList<ForgeUiPageDescriptor> GetPages();
        ForgeUiPageDescriptor FindPage(string id);
        int RemoveOwner(string owner);
        void Clear();
    }

    /// <summary>Host utility for opening a registered page through its owning module's Gauntlet prefab.</summary>
    public static class ForgeUI
    {
        public static event Action<string> PageOpenRequested;
        public static event Action PageCloseRequested;

        /// <summary>Requests that the active Forge host open one registered extension prefab.</summary>
        /// <param name="pageId">Registered page identifier belonging to a loaded module.</param>
        public static void OpenPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId)) throw new ArgumentException("A registered page ID is required.", nameof(pageId));
            var handler = PageOpenRequested;
            if (handler == null) throw new InvalidOperationException("The Calradia Forge Gauntlet host is not available.");
            handler(pageId.Trim());
        }

        public static void ClosePage()
        {
            var handler = PageCloseRequested;
            if (handler == null) throw new InvalidOperationException("The Calradia Forge Gauntlet host is not available.");
            handler();
        }

        public static void Clear()
        {
            PageOpenRequested = null;
            PageCloseRequested = null;
        }
    }

    /// <summary>Build-time and runtime metadata checks shared by the SDK auto-registration flow.</summary>
    public static class ForgeUiDiscovery
    {
        public static ForgeUiPageDescriptor Describe(Type type, string moduleId)
            => DescribeAt(type, moduleId, type?.Assembly.Location);

        // The host always uses the loaded assembly path through Describe. This overload keeps
        // prefab ownership checks testable without loading test fixtures from a game module.
        internal static ForgeUiPageDescriptor DescribeAt(Type type, string moduleId, string assemblyLocation)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var page = type.GetCustomAttributes(typeof(ForgeUiPageAttribute), false).Cast<ForgeUiPageAttribute>().SingleOrDefault();
            if (page == null) return null;
            if (string.IsNullOrWhiteSpace(moduleId)) throw new InvalidOperationException("The owning module ID is required.");
            if (!string.Equals(moduleId, moduleId.Trim(), StringComparison.Ordinal) || moduleId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || moduleId.Contains(".."))
                throw new InvalidOperationException("The owning module ID is invalid.");
            if (string.IsNullOrWhiteSpace(page.Id) || page.Id.Length > 96 || page.Id.Any(char.IsWhiteSpace))
                throw new InvalidOperationException("UI page IDs must be non-empty, whitespace-free, and at most 96 characters.");
            if (string.IsNullOrWhiteSpace(page.TitleKey) || page.TitleKey.Length > 160)
                throw new InvalidOperationException("A bounded localization title key is required.");
            if (!Enum.IsDefined(typeof(Context), page.Context)) throw new InvalidOperationException("The page context is invalid.");
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException("A UI page ViewModel must be a concrete type with a public parameterless constructor.");
            if (!IsGauntletViewModel(type))
                throw new InvalidOperationException("A UI page type must derive from TaleWorlds.Library.ViewModel.");

            var prefab = NormalizePrefab(page.Prefab);
            if (string.IsNullOrWhiteSpace(assemblyLocation)) throw new InvalidOperationException("The UI page assembly has no module file location.");
            var fullAssemblyPath = Path.GetFullPath(assemblyLocation);
            var moduleRoot = Directory.GetParent(Path.GetDirectoryName(fullAssemblyPath))?.Parent?.FullName;
            if (string.IsNullOrWhiteSpace(moduleRoot) || !string.Equals(new DirectoryInfo(moduleRoot).Name, moduleId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The assembly must be located in the declared owner's module folder.");
            var prefabPath = Path.GetFullPath(Path.Combine(moduleRoot, "GUI", "Prefabs", prefab.Replace('/', Path.DirectorySeparatorChar) + ".xml"));
            var expectedRoot = Path.GetFullPath(Path.Combine(moduleRoot, "GUI", "Prefabs")) + Path.DirectorySeparatorChar;
            if (!prefabPath.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(prefabPath))
                throw new FileNotFoundException("The declared prefab must exist inside the owning module's GUI/Prefabs folder.", prefabPath);

            var commands = new List<ForgeUiCommandDescriptor>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var command = method.GetCustomAttributes(typeof(ForgeUiCommandAttribute), false).Cast<ForgeUiCommandAttribute>().SingleOrDefault();
                if (command == null) continue;
                if (string.IsNullOrWhiteSpace(command.Id) || command.Id.Length > 96 || !ids.Add(command.Id))
                    throw new InvalidOperationException("UI command IDs must be unique and non-empty within a page.");
                if (string.IsNullOrWhiteSpace(command.Binding) || !string.Equals(method.Name, command.Binding, StringComparison.Ordinal) || !bindings.Add(command.Binding))
                    throw new InvalidOperationException("Each UI command binding must match exactly one annotated public method name.");
                if (method.IsStatic || method.IsAbstract || method.ContainsGenericParameters || method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                    throw new InvalidOperationException("Annotated Gauntlet commands must be public instance void methods without parameters.");
                if (!Enum.IsDefined(typeof(Context), command.Context)) throw new InvalidOperationException("A UI command context is invalid.");
                commands.Add(new ForgeUiCommandDescriptor { Id = command.Id, Binding = command.Binding, Context = command.Context, ChangesState = command.ChangesState, Method = method });
            }

            var document = XDocument.Load(prefabPath, LoadOptions.None);
            var gauntletBindings = new HashSet<string>(document.Descendants().Attributes().Where(a => a.Name.LocalName == "Command.Click").Select(a => a.Value), StringComparer.Ordinal);
            var missing = bindings.Where(binding => !gauntletBindings.Contains(binding)).ToArray();
            if (missing.Length > 0) throw new InvalidOperationException("Prefab Command.Click binding missing for: " + string.Join(", ", missing));

            return new ForgeUiPageDescriptor
            {
                Id = page.Id.Trim(), Owner = moduleId.Trim(), Prefab = prefab, TitleKey = page.TitleKey.Trim(),
                Context = page.Context, ViewModelType = type, PrefabPath = prefabPath,
                Commands = new ReadOnlyCollection<ForgeUiCommandDescriptor>(commands)
            };
        }

        static bool IsGauntletViewModel(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
                if (string.Equals(current.FullName, "TaleWorlds.Library.ViewModel", StringComparison.Ordinal)) return true;
            return false;
        }

        static string NormalizePrefab(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("A prefab name is required.");
            var normalized = value.Trim().Replace('\\', '/');
            if (normalized.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(0, normalized.Length - 4);
            if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(":") || normalized.Split('/').Any(part => part.Length == 0 || part == "." || part == ".." || part.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'))))
                throw new InvalidOperationException("Prefab names must be relative XML resource names within the owning module.");
            return normalized;
        }
    }

    /// <summary>Applies the host's open-time context and test-mode gates to declared page commands.</summary>
    public static class ForgeUiPolicy
    {
        public static string GetUnavailableReason(ForgeUiPageDescriptor page, Context currentContext, bool testingEnabled, bool campaignCopyConfirmed)
        {
            if (page == null) return "The extension page is not registered.";
            if (!Enum.IsDefined(typeof(Context), currentContext)) return "The current game context is invalid.";
            if (page.Context != Context.Any && page.Context != currentContext)
                return "This extension page requires " + page.Context + " context; current context is " + currentContext + ".";

            foreach (var command in page.Commands ?? new ForgeUiCommandDescriptor[0])
            {
                if (command.Context != Context.Any && command.Context != currentContext)
                    return "The page command '" + command.Id + "' requires " + command.Context + " context; current context is " + currentContext + ".";
                if (!command.ChangesState) continue;
                if (!testingEnabled) return "The state-changing page command '" + command.Id + "' requires test mode.";
                if (currentContext == Context.Campaign && !campaignCopyConfirmed)
                    return "The state-changing page command '" + command.Id + "' requires a confirmed campaign copy.";
            }
            return null;
        }
    }
}
