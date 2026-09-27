using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    public static class ModuleValidator
    {
        const int MaximumDirectoryEntries = 200000;
        const int MaximumDirectories = 10000;
        const int MaximumDirectoryDepth = 64;
        const int MaximumXmlFiles = 10000;

        sealed class ScanBudget
        {
            internal int Entries;
            internal int Directories = 1;
            internal int XmlFiles;
            internal bool Incomplete;
            internal bool SkippedReparsePoint;
            internal bool Unreadable;
            internal bool EntryLimitReached;

            internal bool TryEntry()
            {
                if (Entries >= MaximumDirectoryEntries)
                {
                    Incomplete = true;
                    EntryLimitReached = true;
                    return false;
                }
                Entries++;
                return true;
            }

            internal bool TryDirectory(int depth)
            {
                if (depth > MaximumDirectoryDepth || Directories >= MaximumDirectories)
                {
                    Incomplete = true;
                    return false;
                }
                Directories++;
                return true;
            }

            internal bool TryXmlFile()
            {
                if (XmlFiles >= MaximumXmlFiles)
                {
                    Incomplete = true;
                    return false;
                }
                XmlFiles++;
                return true;
            }
        }

        sealed class CancellationCheckingStream : Stream
        {
            readonly Stream inner;
            readonly CancellationToken cancellationToken;

            internal CancellationCheckingStream(Stream inner, CancellationToken cancellationToken)
            {
                this.inner = inner;
                this.cancellationToken = cancellationToken;
            }

            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => inner.CanSeek;
            public override bool CanWrite => false;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => inner.Position = value; }
            public override void Flush() { inner.Flush(); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return inner.Read(buffer, offset, count);
            }
            public override int ReadByte()
            {
                cancellationToken.ThrowIfCancellationRequested();
                return inner.ReadByte();
            }
            public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

            protected override void Dispose(bool disposing)
            {
                if (disposing) inner.Dispose();
                base.Dispose(disposing);
            }
        }

        // Disable external entities to keep module inspection free of side effects.
        static XDocument Deserialize(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = new CancellationCheckingStream(
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, FileOptions.SequentialScan),
                cancellationToken))
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 32 * 1024 * 1024
            }))
            {
                var document = XDocument.Load(reader);
                cancellationToken.ThrowIfCancellationRequested();
                return document;
            }
        }

        public static ModuleDiagnostics Inspect(string folder)
        {
            return Inspect(folder, CancellationToken.None);
        }

        internal static ModuleDiagnostics Inspect(string folder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var d=new ModuleDiagnostics();
            var requirements=new List<Tuple<string,string,string,string>>();
            if(!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);
            var budget = new ScanBudget();
            var moduleDirectories = new List<string>();
            try
            {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                {
                    budget.SkippedReparsePoint = true;
                    AddScanNotice(d, folder, budget);
                    return d;
                }
                foreach (var entry in Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!budget.TryEntry()) break;
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(entry); }
                    catch (Exception error) when (IsPathAccessError(error)) { budget.Unreadable = true; continue; }
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        budget.SkippedReparsePoint = true;
                        continue;
                    }
                    if ((attributes & FileAttributes.Directory) == 0) continue;
                    if (!budget.TryDirectory(1)) continue;
                    moduleDirectories.Add(entry);
                }
            }
            catch (Exception error) when (IsPathAccessError(error)) { budget.Unreadable = true; }

            foreach(var directory in moduleDirectories.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var manifest=Path.Combine(directory,"SubModule.xml");
                if(!File.Exists(manifest)) { Add(d,"Warning","manifest_missing",directory,manifest,"Missing SubModule.xml","Check module folder"); continue; }
                try
                {
                    if ((File.GetAttributes(manifest) & FileAttributes.ReparsePoint) != 0)
                    {
                        budget.SkippedReparsePoint = true;
                        continue;
                    }
                }
                catch (Exception error) when (IsPathAccessError(error)) { budget.Unreadable = true; continue; }
                try
                {
                    var x=Deserialize(manifest, cancellationToken); var root=x.Root;
                    if(root?.Name.LocalName!="Module") throw new XmlException("Expected Module root");
                    var id=(string)root.Element("Id")?.Attribute("value");
                    if(string.IsNullOrWhiteSpace(id)) throw new XmlException("Missing Id");
                    var m=new Module {Id=id, Version=(string)root.Element("Version")?.Attribute("value"), Folder=directory};
                    var dependencyIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var dependency in root.Descendants("DependedModule"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var dependencyId = (string)dependency.Attribute("Id");
                        if (!string.Equals((string)dependency.Attribute("Optional"), "true", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(dependencyId) && dependencyIds.Add(dependencyId))
                            m.Dependencies.Add(dependencyId);
                        var version = (string)dependency.Attribute("DependentVersion");
                        if (!string.IsNullOrWhiteSpace(dependencyId) && !string.IsNullOrWhiteSpace(version))
                            requirements.Add(Tuple.Create(id, dependencyId, version, manifest));
                    }
                    d.Modules.Add(m);
                    var incompatibles = new HashSet<string>(StringComparer.Ordinal);
                    var incompatibleModules = root.Element("IncompatibleModules");
                    if (incompatibleModules != null)
                    {
                        foreach (var incompatible in incompatibleModules.Descendants("Module"))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var incompatibleId = (string)incompatible.Attribute("Id");
                            if (!string.IsNullOrEmpty(incompatibleId) && incompatibles.Add(incompatibleId))
                                m.Dependencies.Add("INCOMPATIBLE:" + incompatibleId);
                        }
                    }
                    foreach(var dll in root.Descendants("DLLName"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var tags=dll.Parent.Element("Tags")?.Elements("Tag")??Enumerable.Empty<XElement>();
                        if(tags.Any(t=>(string)t.Attribute("key")=="RejectedPlatform" && (string)t.Attribute("value")=="WindowsSteam")) continue;
                        if(tags.Any(t=>(string)t.Attribute("key")=="DedicatedServerType" && (string)t.Attribute("value")!="none")) continue;
                        var name=(string)dll.Attribute("value")??"";
                        if(name.Length==0 || Path.GetFileName(name)!=name) { Add(d,"Error","dll_path",id,manifest,"Invalid DLL path","Use filename only"); continue; }
                        var file=Path.Combine(directory,"bin","Win64_Shipping_Client",name);
                        if(!File.Exists(file) && !File.Exists(Path.Combine(Path.GetDirectoryName(folder),"bin","Win64_Shipping_Client",name))) Add(d,"Error","dll_missing",id,file,"Declared DLL missing","Build or reinstall module");
                    }
                    InspectXml(d,id,directory,budget,cancellationToken);
                }
                catch(Exception e) when(e is XmlException || IsPathAccessError(e)) { Add(d,"Error","manifest_invalid",directory,manifest,e.Message,"Fix manifest"); }
            }
            cancellationToken.ThrowIfCancellationRequested();
            AddScanNotice(d, folder, budget);
            var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicateIds=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in d.Modules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ids.Add(module.Id)) duplicateIds.Add(module.Id);
            }
            foreach (var duplicateId in duplicateIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Add(d,"Error","duplicate_id",duplicateId,folder,"Duplicate ID","Assign unique IDs");
            }
            foreach(var m in d.Modules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach(var dep in m.Dependencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (dep.StartsWith("INCOMPATIBLE:", StringComparison.Ordinal))
                    {
                        var target = dep.Substring(13);
                        if (ids.Contains(target)) Add(d,"Error","incompatible_module",m.Id,m.Folder,target,"Disable incompatible module");
                    }
                    else if (!ids.Contains(dep)) Add(d,"Error","dependency_missing",m.Id,m.Folder,dep,"Install dependency");
                }
                for (var index = m.Dependencies.Count - 1; index >= 0; index--)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (m.Dependencies[index].StartsWith("INCOMPATIBLE:", StringComparison.Ordinal))
                        m.Dependencies.RemoveAt(index);
                }
            }
            var map=new Dictionary<string,Module>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in d.Modules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!map.ContainsKey(module.Id)) map.Add(module.Id, module);
            }
            foreach(var requirement in requirements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // A declaration difference is evidence to review, not proof of binary incompatibility.
                if(map.TryGetValue(requirement.Item2,out var installed) && !string.Equals(requirement.Item3,installed.Version,StringComparison.OrdinalIgnoreCase))
                    Add(d,"Warning","dependency_version_difference",requirement.Item1,requirement.Item4,
                        requirement.Item2+": declared "+requirement.Item3+"; installed "+(installed.Version??"unknown"),
                        "Check the author's supported versions and test this combination. A version difference alone does not prove incompatibility.");
            }
            var colors=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach(var id in map.Keys) Visit(id,map,colors,d,cancellationToken);
            return d;
        }
        static void Visit(string id,Dictionary<string,Module> map,Dictionary<string,int> color,ModuleDiagnostics d,CancellationToken cancellationToken)
        {
            var pending = new Stack<Tuple<string,bool>>();
            pending.Push(Tuple.Create(id, false));
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();
                if (current.Item2)
                {
                    color[current.Item1] = 2;
                    continue;
                }
                if (color.TryGetValue(current.Item1, out var state))
                {
                    if (state == 1) Add(d,"Error","dependency_cycle",current.Item1,map[current.Item1].Folder,"Dependency cycle","Remove circular dependency");
                    continue;
                }
                color[current.Item1] = 1;
                pending.Push(Tuple.Create(current.Item1, true));
                var dependencies = new List<string>();
                foreach (var dependency in map[current.Item1].Dependencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (map.ContainsKey(dependency)) dependencies.Add(dependency);
                }
                for (var index = dependencies.Count - 1; index >= 0; index--)
                    pending.Push(Tuple.Create(dependencies[index], false));
            }
        }
        static void InspectXml(ModuleDiagnostics d,string id,string directory,ScanBudget budget,CancellationToken cancellationToken)
        {
            var pending = new Stack<Tuple<string,int>>();
            pending.Push(Tuple.Create(directory,1));
            while (pending.Count > 0 && !budget.EntryLimitReached)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();
                try
                {
                    foreach (var entry in Directory.EnumerateFileSystemEntries(current.Item1, "*", SearchOption.TopDirectoryOnly))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!budget.TryEntry()) break;
                        FileAttributes attributes;
                        try { attributes = File.GetAttributes(entry); }
                        catch (Exception error) when (IsPathAccessError(error)) { budget.Unreadable = true; continue; }
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            budget.SkippedReparsePoint = true;
                            continue;
                        }
                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            if (string.Equals(Path.GetFileName(entry), "bin", StringComparison.OrdinalIgnoreCase)) continue;
                            var depth = current.Item2 + 1;
                            if (budget.TryDirectory(depth)) pending.Push(Tuple.Create(entry, depth));
                            continue;
                        }
                        if (!string.Equals(Path.GetExtension(entry), ".xml", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(Path.GetFileName(entry), "SubModule.xml", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!budget.TryXmlFile()) continue;
                        try { Deserialize(entry, cancellationToken); }
                        catch(XmlException e)
                        {
                            var engineAsset=entry.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part=>new[]{"SceneObj","AssetPackages","AssetSources"}.Contains(part,StringComparer.OrdinalIgnoreCase));
                            Add(d,engineAsset?"Warning":"Error",engineAsset?"asset_xml_review":"xml_invalid",id,entry,e.Message,
                                engineAsset?"Review with the Bannerlord asset tools. Generic XML validation does not establish whether this engine asset is loadable.":"Fix XML");
                        }
                        catch(Exception e) when (IsPathAccessError(e)) { Add(d,"Warning","xml_unreadable",id,entry,e.Message,"Check file access and repeat the scan; XML validity was not established."); }
                    }
                }
                catch(Exception e) when (IsPathAccessError(e)) { budget.Unreadable = true; }
            }
        }

        static bool IsPathAccessError(Exception error) => error is IOException || error is UnauthorizedAccessException || error is SecurityException;

        static void AddScanNotice(ModuleDiagnostics diagnostics, string folder, ScanBudget budget)
        {
            if (!budget.Incomplete && !budget.SkippedReparsePoint && !budget.Unreadable) return;
            var reasons = new List<string>();
            if (budget.Incomplete) reasons.Add("a directory, depth, entry, or XML-file bound was reached");
            if (budget.SkippedReparsePoint) reasons.Add("reparse points were skipped");
            if (budget.Unreadable) reasons.Add("some paths could not be read");
            Add(diagnostics,"Warning","analysis_scan_incomplete",folder,folder,
                "The module tree was only partially inspected because " + string.Join(", ", reasons) + ".",
                "Narrow the selected folder and ensure the desired module files are directly accessible.");
        }
        static void Add(ModuleDiagnostics d,string level,string code,string module,string file,string message,string suggestion) => d.Findings.Add(new Finding { Level=level,Code=code,Module=module,File=file,Message=message,Suggestion=suggestion });
    }
}
