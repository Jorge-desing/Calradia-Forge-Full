using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Desktop.Presentation
{
    /// <summary>Declarative metadata for a routed workbench surface. Attributes describe intent; XAML owns layout.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class ForgeUiPageAttribute(string key, string region, bool releaseOnNavigate = true) : Attribute
    {
        public string Key { get; } = key ?? throw new ArgumentNullException(nameof(key));
        public string Region { get; } = region ?? throw new ArgumentNullException(nameof(region));
        public bool ReleaseOnNavigate { get; } = releaseOnNavigate;
    }

    /// <summary>Declares a command slot that the view renders without inspecting controls or event handlers.</summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class ForgeUiCommandAttribute(string id, string slot, bool cancellable = false) : Attribute
    {
        public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));
        public string Slot { get; } = slot ?? throw new ArgumentNullException(nameof(slot));
        public bool Cancellable { get; } = cancellable;
    }

    internal sealed class DeclarativeUiDescriptor(Type owner, string key, string region, bool releaseOnNavigate, IReadOnlyList<DeclarativeUiCommand> commands)
    {
        public Type Owner { get; } = owner;
        public string Key { get; } = key;
        public string Region { get; } = region;
        public bool ReleaseOnNavigate { get; } = releaseOnNavigate;
        public IReadOnlyList<DeclarativeUiCommand> Commands { get; } = commands;
    }

    internal sealed class DeclarativeUiCommand(string property, string id, string slot, bool cancellable)
    {
        public string Property { get; } = property;
        public string Id { get; } = id;
        public string Slot { get; } = slot;
        public bool Cancellable { get; } = cancellable;
    }

    /// <summary>Discovers immutable UI descriptors once from attributes; it never walks a WPF visual tree.</summary>
    internal static class DeclarativeUiCatalog
    {
        static readonly IReadOnlyList<DeclarativeUiDescriptor> descriptors = Discover(Assembly.GetExecutingAssembly());

        public static IReadOnlyList<DeclarativeUiDescriptor> Descriptors => descriptors;

        public static TPage Create<TPage>(string key, Func<TPage> factory) where TPage : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var descriptor = descriptors.SingleOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal));
            if (descriptor == null) throw new InvalidOperationException("Unknown declared UI page: " + key);
            if (!descriptor.Owner.IsAssignableFrom(typeof(TPage))) throw new InvalidOperationException("Page factory type does not match UI descriptor: " + key);
            var page = factory() ?? throw new InvalidOperationException("Declared page factory returned null: " + key);
            if (!descriptor.Owner.IsInstanceOfType(page)) throw new InvalidOperationException("Page instance does not match UI descriptor: " + key);
            return page;
        }

        public static IReadOnlyList<DeclarativeUiDescriptor> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            var pages = assembly.GetTypes()
                .Select(type => new { Type = type, Page = type.GetCustomAttribute<ForgeUiPageAttribute>(inherit: false) })
                .Where(item => item.Page != null)
                .Select(item => new DeclarativeUiDescriptor(
                    item.Type,
                    item.Page.Key,
                    item.Page.Region,
                    item.Page.ReleaseOnNavigate,
                    item.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Select(property => new { Property = property, Command = property.GetCustomAttribute<ForgeUiCommandAttribute>(inherit: false) })
                        .Where(item2 => item2.Command != null)
                        .Select(item2 => new DeclarativeUiCommand(item2.Property.Name, item2.Command.Id, item2.Command.Slot, item2.Command.Cancellable))
                        .ToArray()))
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToArray();
            Validate(pages);
            return pages;
        }

        internal static void Validate(IReadOnlyList<DeclarativeUiDescriptor> pages)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var commandIds = new HashSet<string>(StringComparer.Ordinal);
            var regions = new HashSet<string>(["Workbench", "Evidence", "Status"], StringComparer.Ordinal);
            foreach (var page in pages)
            {
                if (string.IsNullOrWhiteSpace(page.Key) || !keys.Add(page.Key)) throw new InvalidOperationException("Duplicate or empty Forge UI page key: " + page.Key);
                if (!regions.Contains(page.Region)) throw new InvalidOperationException("Unknown Forge UI region '" + page.Region + "' on " + page.Key);
                foreach (var command in page.Commands)
                {
                    if (string.IsNullOrWhiteSpace(command.Id) || !commandIds.Add(command.Id)) throw new InvalidOperationException("Duplicate or empty Forge UI command id: " + command.Id);
                    if (command.Slot != "Primary" && command.Slot != "Secondary") throw new InvalidOperationException("Unknown command slot '" + command.Slot + "' on " + command.Id);
                    var property = page.Owner.GetProperty(command.Property, BindingFlags.Instance | BindingFlags.Public);
                    if (property == null || !typeof(System.Windows.Input.ICommand).IsAssignableFrom(property.PropertyType)) throw new InvalidOperationException("Declared command must bind to ICommand: " + page.Owner.Name + "." + command.Property);
                    if (command.Cancellable && property.PropertyType != typeof(AsyncRelayCommand)) throw new InvalidOperationException("Cancellable command must use the internal AsyncRelayCommand: " + command.Id);
                }
            }
        }
    }
}
