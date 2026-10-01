using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Core
{
    // This reflector has no Harmony reference. It observes only the public API when a compatible
    // Harmony runtime is already loaded by the game or another module.
    public static class HarmonyDiagnostics
    {
        const int MaximumDiscoveredMethods=2000;
        const int MaximumPatchesPerMethod=128;
        const int MaximumOrderingIds=32;
        const int MaximumNotes=20;
        const int MaximumTextLength=512;

        public static HarmonySnapshot Inspect(IEnumerable<Assembly> assemblies,string filter=null,int maximumMethods=200)
        {
            return Inspect(FindHarmonyType(assemblies),filter,maximumMethods);
        }

        public static HarmonySnapshot Inspect(Type harmonyType,string filter=null,int maximumMethods=200)
        {
            var result=new HarmonySnapshot {CapturedAt=DateTime.UtcNow.ToString("O")};
            if(harmonyType==null)
            {
                result.Status="Harmony is not loaded (optional).";
                result.Notes.Add("No patching API was loaded by this session.");
                return result;
            }

            result.Available=true;
            result.RuntimeAssembly=Text(harmonyType.Assembly.GetName().Name);
            result.RuntimeVersion=Text(harmonyType.Assembly.GetName().Version?.ToString());
            var methods=FindMethod(harmonyType,"GetAllPatchedMethods",0,null);
            var patchInfo=FindMethod(harmonyType,"GetPatchInfo",1,typeof(MethodBase));
            if(methods==null || patchInfo==null)
            {
                result.Status="Harmony is loaded, but its patch query API is unavailable.";
                result.Notes.Add("Update Harmony or inspect this runtime with its own tooling.");
                return result;
            }

            result.Supported=true;
            IEnumerable patched;
            try { patched=methods.Invoke(null,null) as IEnumerable; }
            catch(Exception error) { return Failed(result,"Harmony patch query failed",error); }
            if(patched==null)
            {
                result.Status="Harmony returned no enumerable patch targets.";
                return result;
            }

            var captured=new List<HarmonyPatchedMethod>();
            foreach(var value in patched)
            {
                if(result.DiscoveredMethodCount>=MaximumDiscoveredMethods)
                {
                    result.Truncated=true;
                    AddNote(result,"Discovery stopped after "+MaximumDiscoveredMethods+" patched methods.");
                    break;
                }
                var target=value as MethodBase;
                if(target==null) {result.SkippedMethodCount++;continue;}
                result.DiscoveredMethodCount++;
                try
                {
                    var row=DescribeTarget(target,patchInfo,result);
                    if(Matches(row,filter))captured.Add(row);
                }
                catch(Exception error)
                {
                    result.SkippedMethodCount++;
                    AddNote(result,"Patch metadata skipped for "+Text(target.Name)+": "+Describe(error));
                }
            }

            result.OwnerCount=captured.SelectMany(row=>row.Owners).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            result.SharedMethodCount=captured.Count(row=>row.HasMultipleOwners);
            result.ActiveMethodCount=captured.Count(row=>row.MetadataStatus=="Active");
            result.EmptyMetadataMethodCount=captured.Count(row=>row.MetadataStatus!="Active");
            var limit=Math.Max(1,Math.Min(maximumMethods,500));
            var ordered=captured.OrderByDescending(row=>row.HasMultipleOwners).ThenByDescending(row=>row.Patches.Count).ThenBy(row=>row.DeclaringType,StringComparer.Ordinal).ThenBy(row=>row.Method,StringComparer.Ordinal).ThenBy(row=>row.Signature,StringComparer.Ordinal).ToList();
            if(ordered.Count>limit) {result.Truncated=true;AddNote(result,"Displaying "+limit+" of "+ordered.Count+" matching patch targets.");ordered=ordered.Take(limit).ToList();}
            result.Methods=ordered;
            result.DisplayedMethodCount=ordered.Count;
            if(result.SharedMethodCount>0) AddNote(result,result.SharedMethodCount+" targets have more than one Harmony owner. They are review candidates, not proven conflicts.");
            result.Status=ordered.Count==0
                ? (string.IsNullOrWhiteSpace(filter)?"Harmony is loaded. No patched methods were reported.":"Harmony is loaded. No patched methods match this filter.")
                : result.ActiveMethodCount==0
                    ? "Harmony returned target identities without active patch metadata. They are not confirmed patch evidence."
                    : "Harmony patch atlas is read-only. Shared targets are observations, not proven conflicts.";
            return result;
        }

        static Type FindHarmonyType(IEnumerable<Assembly> assemblies)
        {
            if(assemblies==null)return null;
            foreach(var assembly in assemblies)
            {
                try
                {
                    var harmony=assembly?.GetType("HarmonyLib.Harmony",false);
                    if(harmony!=null)return harmony;
                }
                catch(ReflectionTypeLoadException) { /* Non-loadable types in foreign assemblies are ignored */ }
                catch(Exception) { /* Assembly reflection probe failure ignored */ }
            }
            return null;
        }

        static HarmonySnapshot Failed(HarmonySnapshot result,string prefix,Exception error)
        {
            result.Status=prefix+": "+Describe(error);
            AddNote(result,"No patches were changed by this query.");
            return result;
        }

        static MethodInfo FindMethod(Type type,string name,int parameterCount,Type expectedParameter)
        {
            return type.GetMethods(BindingFlags.Public|BindingFlags.Static).FirstOrDefault(method=>
            {
                if(method.Name!=name || method.GetParameters().Length!=parameterCount)return false;
                if(expectedParameter==null)return true;
                return method.GetParameters()[0].ParameterType.IsAssignableFrom(expectedParameter);
            });
        }

        static HarmonyPatchedMethod DescribeTarget(MethodBase target,MethodInfo getPatchInfo,HarmonySnapshot result)
        {
            var row=new HarmonyPatchedMethod {
                Assembly=Text(target.DeclaringType?.Assembly.GetName().Name),
                DeclaringType=Text(target.DeclaringType?.FullName),
                Method=Text(target.Name),
                Signature=Signature(target),
                MetadataStatus="No metadata"
            };
            var info=getPatchInfo.Invoke(null,new object[]{target});
            if(info==null) {AddNote(result,"Harmony returned no patch metadata for "+row.DeclaringType+"."+row.Method+".");return row;}
            row.Owners=Strings(Read(info,"Owners")).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value=>value,StringComparer.Ordinal).ToList();
            foreach(var kind in new[]{"Prefix","Postfix","Transpiler","Finalizer"}) AddPatches(row,info,kind,result);
            if(row.Owners.Count==0)row.Owners=row.Patches.Select(p=>p.Owner).Where(value=>!string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value=>value,StringComparer.Ordinal).ToList();
            row.HasMultipleOwners=row.Owners.Count>1;
            if(row.Owners.Count>0 || row.Patches.Count>0)row.MetadataStatus="Active";
            else AddNote(result,"Harmony returned no active patch metadata for "+row.DeclaringType+"."+row.Method+".");
            return row;
        }

        static void AddPatches(HarmonyPatchedMethod row,object info,string kind,HarmonySnapshot result)
        {
            var property=kind+"s";
            if(kind=="Prefix")property="Prefixes";
            if(kind=="Postfix")property="Postfixes";
            var count=0;
            foreach(var patch in Values(Read(info,property)))
            {
                if(count++>=MaximumPatchesPerMethod)
                {
                    result.Truncated=true;
                    AddNote(result,"Patch metadata was limited to "+MaximumPatchesPerMethod+" "+kind.ToLowerInvariant()+" entries per target.");
                    break;
                }
                var patchMethod=Read(patch,"PatchMethod","Method","method") as MethodBase;
                if(patchMethod==null)
                {
                    var descriptor=Read(patch,"PatchMethod","Method","method");
                    patchMethod=Read(descriptor,"method","Method") as MethodBase;
                }
                row.Patches.Add(new HarmonyPatchObservation {
                    Kind=kind,
                    Owner=Text(Read(patch,"owner","Owner")) ?? "unknown",
                    Priority=Integer(Read(patch,"priority","Priority")),
                    Index=Integer(Read(patch,"index","Index")),
                    Before=Strings(Read(patch,"before","Before"),MaximumOrderingIds,result),
                    After=Strings(Read(patch,"after","After"),MaximumOrderingIds,result),
                    PatchAssembly=Text(patchMethod?.DeclaringType?.Assembly.GetName().Name),
                    PatchType=Text(patchMethod?.DeclaringType?.FullName),
                    PatchMethod=Text(patchMethod?.Name),
                    PatchSignature=patchMethod==null?null:Signature(patchMethod)
                });
            }
        }

        static bool Matches(HarmonyPatchedMethod row,string filter)
        {
            if(string.IsNullOrWhiteSpace(filter))return true;
            var value=filter.Trim();
            if(Contains(row.Assembly,value)||Contains(row.DeclaringType,value)||Contains(row.Method,value)||Contains(row.Signature,value))return true;
            return row.Owners.Any(owner=>Contains(owner,value)) || row.Patches.Any(patch=>Contains(patch.Owner,value)||Contains(patch.Kind,value)||Contains(patch.PatchAssembly,value)||Contains(patch.PatchType,value)||Contains(patch.PatchMethod,value));
        }

        static bool Contains(string source,string value)=>source?.IndexOf(value,StringComparison.OrdinalIgnoreCase)>=0;
        static string Signature(MethodBase method)
        {
            try {return "("+string.Join(", ",method.GetParameters().Select(parameter=>Text(parameter.ParameterType?.FullName)??Text(parameter.ParameterType?.Name)))+")";}
            catch {return "(?)";}
        }
        static int? Integer(object value)
        {
            if(value==null)return null;
            try {return Convert.ToInt32(value,System.Globalization.CultureInfo.InvariantCulture);}
            catch {return null;}
        }
        static object Read(object source,params string[] names)
        {
            if(source==null)return null;
            foreach(var name in names)
            {
                try
                {
                    var type=source.GetType();
                    var property=type.GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.IgnoreCase);
                    if(property!=null)return property.GetValue(source,null);
                    var field=type.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.IgnoreCase);
                    if(field!=null)return field.GetValue(source);
                }
                catch(Exception) { /* Non-accessible or throwing property getter on foreign object */ }
            }
            return null;
        }
        static IEnumerable<object> Values(object value)
        {
            if(value==null || value is string)return Enumerable.Empty<object>();
            var enumerable=value as IEnumerable;
            if(enumerable==null)return Enumerable.Empty<object>();
            return Enumerate(enumerable);
        }
        static IEnumerable<object> Enumerate(IEnumerable values)
        {
            IEnumerator iterator=null;
            try {iterator=values.GetEnumerator();}
            catch(Exception) { /* Collection throws on GetEnumerator */ }
            if(iterator==null)yield break;
            try
            {
                object current;
                while(Next(iterator,out current))yield return current;
            }
            finally {(iterator as IDisposable)?.Dispose();}
        }
        static bool Next(IEnumerator iterator,out object current)
        {
            current=null;
            try
            {
                if(!iterator.MoveNext())return false;
                current=iterator.Current;
                return true;
            }
            catch(Exception) { return false; /* Iterator MoveNext failed or invalid */ }
        }
        static List<string> Strings(object value,int maximum=MaximumOrderingIds,HarmonySnapshot result=null)
        {
            var values=new List<string>();
            if(value is string single) {if(!string.IsNullOrWhiteSpace(single))values.Add(Text(single));return values;}
            foreach(var item in Values(value))
            {
                if(values.Count>=maximum)
                {
                    if(result!=null) {result.Truncated=true;AddNote(result,"Ordering IDs were limited to "+maximum+" entries.");}
                    break;
                }
                var text=Text(item);
                if(!string.IsNullOrWhiteSpace(text))values.Add(text);
            }
            return values;
        }
        static string Text(object value)
        {
            if(value==null)return null;
            string text;
            try {text=value.ToString();} catch {return null;}
            if(text==null)return null;
            return text.Length>MaximumTextLength?text.Substring(0,MaximumTextLength)+"…":text;
        }
        static string Describe(Exception error)=>Text((error?.GetType().Name??"Error")+": "+(error?.Message??"Unknown error"));
        static void AddNote(HarmonySnapshot result,string value)
        {
            if(result.Notes.Count<MaximumNotes && !result.Notes.Contains(value))result.Notes.Add(value);
        }
    }
}
