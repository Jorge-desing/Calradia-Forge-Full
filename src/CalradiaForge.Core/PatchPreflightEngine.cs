using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    // Resolves author-declared patch blueprints against assemblies that are already loaded.
    // It never loads assemblies, calls a patch API, or invokes a declared method.
    public static class PatchPreflightEngine
    {
        const int MaximumOutcomes=200;
        const int MaximumFindings=200;
        public static PatchPreflightSnapshot Inspect(PatchBlueprintCapture capture,IEnumerable<Assembly> assemblies,string context=null,int maximumOutcomes=MaximumOutcomes)
        {
            capture=capture??new PatchBlueprintCapture();
            var result=new PatchPreflightSnapshot {
                CapturedAt=DateTime.UtcNow.ToString("O"),
                Context=context??"Any",
                ProviderCount=capture.ProviderCount,
                Truncated=capture.Truncated
            };
            foreach(var finding in capture.Findings??new List<Finding>()) if(finding!=null && result.Findings.Count<MaximumFindings) result.Findings.Add(CopyFinding(finding));
            var loaded=LoadedAssemblies(assemblies);
            var limit=Math.Max(1,Math.Min(maximumOutcomes,MaximumOutcomes));
            var declarations=capture.Declarations??new List<PatchBlueprintDeclaration>();
            var idCounts=CountIds(declarations);
            var resolved=new List<ResolvedBlueprint>();
            foreach(var source in declarations)
            {
                if(result.Outcomes.Count>=limit)
                {
                    result.Truncated=true;
                    AddFinding(result,"Warning","patch_blueprint_limit",null,"Patch blueprint preflight reached its display limit.","Run focused providers or narrow the registered blueprint set.");
                    break;
                }
                var outcome=new PatchPreflightOutcome {Declaration=CopyDeclaration(source)};
                result.BlueprintCount++;
                var blueprint=outcome.Declaration.Blueprint;
                var id=blueprint?.Id;
                if(string.IsNullOrWhiteSpace(id))
                {
                    Block(result,outcome,"Invalid blueprint","A blueprint ID is required.","Give every blueprint a stable ID.");
                }
                else if(!string.Equals(id,id.Trim(),StringComparison.Ordinal))
                {
                    Block(result,outcome,"Invalid blueprint","A blueprint ID cannot have leading or trailing whitespace.","Use a stable ID without surrounding whitespace.");
                }
                else if(id.Length>512)
                {
                    Block(result,outcome,"Invalid blueprint","A blueprint ID cannot exceed 512 characters.","Use a stable ID of at most 512 characters.");
                }
                else if(idCounts.TryGetValue(id,out var idCount) && idCount>1)
                {
                    Block(result,outcome,"Duplicate blueprint ID","More than one provider declared the blueprint ID: "+id+".","Use one unique ID per registered blueprint.");
                }
                else if(blueprint==null || !Enum.IsDefined(typeof(PatchHookKind),blueprint.Hook))
                {
                    Block(result,outcome,"Invalid blueprint","The declared hook kind is not supported.","Use Prefix, Postfix, Transpiler, or Finalizer.");
                }
                else
                {
                    var target=Resolve(blueprint.Target,loaded,"target");
                    if(target.Error!=null)
                    {
                        Block(result,outcome,target.Status,target.Error,target.Suggestion);
                    }
                    else
                    {
                        SetResolvedTarget(outcome,target.Member);
                        resolved.Add(new ResolvedBlueprint(outcome,target.Member));
                        if(blueprint.PatchMethod==null)
                        {
                            Block(result,outcome,"Callback not declared","A patch callback method reference is required for this declaration.","Declare the callback's exact assembly, type, method, generic arity, parameters, and return type.");
                        }
                        else if(blueprint.PatchMethod.MemberKind!=PatchMemberKind.Method)
                        {
                            Block(result,outcome,"Invalid callback","A patch callback must reference a method, not a constructor.","Declare a method with an exact loaded-assembly signature.");
                        }
                        else
                        {
                            var callback=Resolve(blueprint.PatchMethod,loaded,"callback");
                            if(callback.Error!=null)
                            {
                                Block(result,outcome,"Callback "+callback.Status,"Patch callback could not be resolved: "+callback.Error,"Declare an exact callback reference and ensure its assembly is already loaded.");
                            }
                            else
                            {
                                SetResolvedCallback(outcome,callback.Member);
                                AddNote(outcome,"The callback reference resolves exactly. Hook-specific runtime adaptation is not validated; this preflight does not apply hooks.");
                                outcome.Resolved=true;
                                outcome.Status="Resolved";
                            }
                        }
                        if(outcome.Resolved)result.ResolvedCount++;
                    }
                }
                result.Outcomes.Add(outcome);
            }
            AnalyzeTargetConflicts(result,resolved);
            if(!result.Truncated)AnalyzeOrdering(result);
            else AddNote(result,"Ordering references and cycles were not fully analyzed because the displayed blueprint set was truncated.");
            if(result.Outcomes.Count==0)
            {
                result.Status=result.ProviderCount==0
                    ? "No patch blueprint providers are registered in this session."
                    : "No patch blueprints were returned for this context.";
            }
            else
            {
                var blocked=result.Outcomes.Count(outcome=>!outcome.Resolved);
                if(blocked>0) result.Status="Blocked: "+blocked+" blueprint entr"+(blocked==1?"y needs":"ies need")+" target or callback attention. No patch is applied.";
                else if(result.ReviewCount>0) result.Status="Review required: inspect target conflicts and declared ordering. No patch is applied.";
                else result.Status="No blocking issues found. No patch is applied; this preflight does not prove runtime compatibility.";
            }
            if(result.Truncated)AddNote(result,"Results are bounded; run focused providers for a complete review.");
            return result;
        }

        static Dictionary<string,int> CountIds(IEnumerable<PatchBlueprintDeclaration> declarations)
        {
            var counts=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach(var declaration in declarations??Enumerable.Empty<PatchBlueprintDeclaration>())
            {
                var id=declaration?.Blueprint?.Id;
                if(string.IsNullOrWhiteSpace(id))continue;
                counts.TryGetValue(id,out var count);
                counts[id]=count+1;
            }
            return counts;
        }

        static void SetResolvedTarget(PatchPreflightOutcome outcome,MethodBase member)
        {
            outcome.ResolvedAssembly=Safe(member.DeclaringType?.Assembly.GetName().Name);
            outcome.ResolvedType=Safe(member.DeclaringType?.FullName);
            outcome.ResolvedMember=MemberName(member);
            outcome.ResolvedSignature=Signature(member);
        }

        static void SetResolvedCallback(PatchPreflightOutcome outcome,MethodBase member)
        {
            outcome.CallbackResolved=true;
            outcome.ResolvedCallbackAssembly=Safe(member.DeclaringType?.Assembly.GetName().Name);
            outcome.ResolvedCallbackType=Safe(member.DeclaringType?.FullName);
            outcome.ResolvedCallbackMember=MemberName(member);
            outcome.ResolvedCallbackSignature=Signature(member);
        }

        static void AnalyzeTargetConflicts(PatchPreflightSnapshot result,List<ResolvedBlueprint> resolved)
        {
            foreach(var group in resolved.GroupBy(item=>MemberIdentity(item.Target),StringComparer.Ordinal))
            {
                var entries=group.ToList();
                if(entries.Count<2)continue;
                var ids=string.Join(", ",entries.Select(item=>item.Outcome.Declaration.Blueprint.Id));
                foreach(var entry in entries)Review(result,entry.Outcome,"patch_target_conflict","Multiple blueprints declare the same target: "+ids+".","Review the declarations together; preflight does not define their runtime composition.");
            }
        }

        static void AnalyzeOrdering(PatchPreflightSnapshot result)
        {
            var idCounts=CountIds(result.Outcomes.Select(outcome=>outcome.Declaration));
            var byId=new Dictionary<string,PatchPreflightOutcome>(StringComparer.OrdinalIgnoreCase);
            foreach(var outcome in result.Outcomes)
            {
                var id=outcome.Declaration?.Blueprint?.Id;
                if(string.IsNullOrWhiteSpace(id))continue;
                if(!idCounts.TryGetValue(id,out var count) || count!=1)continue;
                if(!byId.ContainsKey(id))byId.Add(id,outcome);
            }
            var uniqueIds=new HashSet<string>(byId.Keys,StringComparer.OrdinalIgnoreCase);
            foreach(var outcome in result.Outcomes)
            {
                var blueprint=outcome.Declaration?.Blueprint;
                if(blueprint==null || string.IsNullOrWhiteSpace(blueprint.Id) || !uniqueIds.Contains(blueprint.Id))continue;
                InspectOrderList(result,outcome,blueprint.Id,blueprint.Before,true,byId);
                InspectOrderList(result,outcome,blueprint.Id,blueprint.After,false,byId);
            }
            var edges=new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach(var pair in byId)edges[pair.Key]=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var outcome in result.Outcomes)
            {
                var blueprint=outcome.Declaration?.Blueprint;
                if(blueprint==null || string.IsNullOrWhiteSpace(blueprint.Id) || !byId.ContainsKey(blueprint.Id))continue;
                foreach(var reference in blueprint.Before??new List<string>())if(byId.ContainsKey(reference) && !string.Equals(reference,blueprint.Id,StringComparison.OrdinalIgnoreCase))edges[blueprint.Id].Add(reference);
                foreach(var reference in blueprint.After??new List<string>())if(byId.ContainsKey(reference) && !string.Equals(reference,blueprint.Id,StringComparison.OrdinalIgnoreCase))edges[reference].Add(blueprint.Id);
            }
            FindOrderingCycles(result,byId,edges);
            if(result.Outcomes.Any(outcome=>(outcome.Declaration?.Blueprint?.Before?.Count??0)>0 || (outcome.Declaration?.Blueprint?.After?.Count??0)>0))
                AddNote(result,"Before/After references are checked against unique blueprint IDs. Their declarations remain advisory; Forge does not execute or infer patch order.");
        }

        static void InspectOrderList(PatchPreflightSnapshot result,PatchPreflightOutcome outcome,string id,IEnumerable<string> references,bool before,Dictionary<string,PatchPreflightOutcome> byId)
        {
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var referenceIndex=0;
            foreach(var reference in references??Enumerable.Empty<string>())
            {
                if(referenceIndex++>=32)
                {
                    Review(result,outcome,"patch_order_limit","Blueprint '"+id+"' contains more than 32 ordering references in "+(before?"Before":"After")+"; the remainder was not inspected.","Keep at most 32 Before/After references and review the complete declaration.");
                    break;
                }
                if(string.IsNullOrWhiteSpace(reference))
                {
                    Review(result,outcome,"patch_order_invalid","Blueprint '"+id+"' contains an empty ordering reference in "+(before?"Before":"After")+".","Remove blank entries from Before/After.");
                    continue;
                }
                if(!seen.Add(reference))
                {
                    Review(result,outcome,"patch_order_duplicate","Blueprint '"+id+"' repeats ordering reference '"+reference+"'.","Keep each Before/After blueprint ID once.");
                    continue;
                }
                if(string.Equals(reference,id,StringComparison.OrdinalIgnoreCase))
                {
                    Review(result,outcome,"patch_order_self","Blueprint '"+id+"' orders itself.","Remove the self reference or use the intended blueprint ID.");
                    continue;
                }
                var declarationCount=result.Outcomes.Count(row=>string.Equals(row.Declaration?.Blueprint?.Id,reference,StringComparison.OrdinalIgnoreCase));
                if(declarationCount==0)
                {
                    Review(result,outcome,"patch_order_missing","Blueprint '"+id+"' references unknown blueprint ID '"+reference+"' in "+(before?"Before":"After")+".","Use an ID declared in the same preflight capture.");
                    continue;
                }
                if(declarationCount>1)
                {
                    Review(result,outcome,"patch_order_ambiguous","Blueprint '"+id+"' references duplicated blueprint ID '"+reference+"'.","Resolve the duplicate ID before reviewing ordering.");
                }
            }
        }

        static void FindOrderingCycles(PatchPreflightSnapshot result,Dictionary<string,PatchPreflightOutcome> byId,Dictionary<string,HashSet<string>> edges)
        {
            var states=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            var stack=new List<string>();
            var cyclic=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Action<string> visit=null;
            visit=id=>
            {
                states[id]=1;stack.Add(id);
                foreach(var next in edges[id])
                {
                    states.TryGetValue(next,out var state);
                    if(state==0)visit(next);
                    else if(state==1)
                    {
                        var start=stack.FindLastIndex(value=>string.Equals(value,next,StringComparison.OrdinalIgnoreCase));
                        for(var index=Math.Max(0,start);index<stack.Count;index++)cyclic.Add(stack[index]);
                    }
                }
                stack.RemoveAt(stack.Count-1);states[id]=2;
            };
            foreach(var id in byId.Keys)if(!states.ContainsKey(id))visit(id);
            if(cyclic.Count==0)return;
            var ids=string.Join(", ",cyclic.OrderBy(value=>value,StringComparer.OrdinalIgnoreCase));
            foreach(var id in cyclic)Review(result,byId[id],"patch_order_cycle","Ordering declarations contain a cycle among: "+ids+".","Break the cycle; preflight does not reorder or apply patches.");
        }

        static void Review(PatchPreflightSnapshot result,PatchPreflightOutcome outcome,string code,string message,string suggestion)
        {
            result.ReviewCount++;
            AddNote(outcome,message);
            AddFinding(result,"Warning",code,outcome.Declaration?.Module,message,suggestion);
        }

        static string MemberIdentity(MethodBase member)
        {
            try
            {
                var info=member as MethodInfo;
                var parameters=string.Join("|",member.GetParameters().Select(parameter=>TypeIdentity(parameter.ParameterType)));
                return TypeIdentity(member.DeclaringType)+"|"+MemberName(member)+"|"+(info?.IsGenericMethod==true?info.GetGenericArguments().Length:0)+"|"+parameters+"|"+(info==null?"":TypeIdentity(info.ReturnType))+"|"+member.IsStatic;
            }
            catch{return "unreadable-member:"+member.GetHashCode();}
        }

        static string TypeIdentity(Type type)
        {
            try{return type?.AssemblyQualifiedName??type?.FullName??type?.Name??"";}
            catch{return type?.FullName??type?.Name??"";}
        }

        static string MemberName(MethodBase member)=>member.IsConstructor?(member.IsStatic?".cctor":".ctor"):Safe(member.Name);

        sealed class ResolvedBlueprint
        {
            public PatchPreflightOutcome Outcome { get; }
            public MethodBase Target { get; }
            public ResolvedBlueprint(PatchPreflightOutcome outcome,MethodBase target){Outcome=outcome;Target=target;}
        }

        static void Block(PatchPreflightSnapshot result,PatchPreflightOutcome outcome,string status,string message,string suggestion)
        {
            outcome.Status=status;
            AddNote(outcome,message);
            AddFinding(result,"Warning","patch_blueprint_"+Code(status),outcome.Declaration.Module,message,suggestion);
        }

        static Resolution Resolve(MethodReference reference,IEnumerable<Assembly> assemblies,string role="target")
        {
            var label=string.Equals(role,"callback",StringComparison.OrdinalIgnoreCase)?"callback":"target";
            if(reference==null)return Resolution.Fail("Invalid "+label,"A "+label+" method reference is required.","Provide an exact assembly, type, member, and parameter list.");
            if(string.IsNullOrWhiteSpace(reference.AssemblyName) || string.IsNullOrWhiteSpace(reference.DeclaringType))return Resolution.Fail("Invalid "+label,Cap(label)+" assembly and declaring type are required.","Use MethodReference.From or provide exact type metadata.");
            if(!Enum.IsDefined(typeof(PatchMemberKind),reference.MemberKind))return Resolution.Fail("Invalid "+label,"The "+label+" member kind is not supported.","Use Method or Constructor.");
            if(reference.GenericArity<0)return Resolution.Fail("Invalid "+label,"Generic arity cannot be negative.","Use zero for a non-generic method.");
            if(reference.MemberKind==PatchMemberKind.Method && string.IsNullOrWhiteSpace(reference.MemberName))return Resolution.Fail("Invalid "+label,"A "+label+" method name is required.","Provide the exact member name.");
            if(!ValidTypes(reference.ParameterTypes))return Resolution.Fail("Invalid "+label,"Each parameter needs an exact type full name.","Use TypeReference.From for every parameter.");
            if(reference.ReturnType!=null && !ValidType(reference.ReturnType))return Resolution.Fail("Invalid "+label,"The return-type assertion is incomplete.","Use TypeReference.From or omit the optional return type.");
            var matching=assemblies.Where(assembly=>AssemblyName(assembly).Equals(reference.AssemblyName,StringComparison.OrdinalIgnoreCase)).ToList();
            if(matching.Count==0)return Resolution.Fail("Assembly not loaded",Cap(label)+" assembly '"+reference.AssemblyName+"' is not loaded in this session.","Load the owning module before running this preflight.");
            if(matching.Count>1)return Resolution.Fail("Ambiguous assembly","More than one loaded assembly uses the simple name '"+reference.AssemblyName+"'.","Use a session without duplicate assembly identities.");
            Type type;
            try {type=matching[0].GetType(reference.DeclaringType,false,false);}
            catch(Exception error) {return Resolution.Fail("Type unreadable",Cap(label)+" type could not be inspected: "+Safe(error.Message),"Check that the target assembly is compatible with this game build.");}
            if(type==null)return Resolution.Fail("Type not found",Cap(label)+" type '"+reference.DeclaringType+"' was not found in '"+reference.AssemblyName+"'.","Use the exact full type name for the installed game build.");
            var candidates=Members(type,reference).Where(member=>Matches(member,reference)).ToList();
            if(candidates.Count==0)return Resolution.Fail("Member not found","No loaded member matches the declared "+label+" signature.","Specify every parameter type and verify the target game version.");
            if(candidates.Count>1)return Resolution.Fail("Ambiguous "+label,"More than one member matches the declared "+label+" signature.","Add a return type, static assertion, or generic arity to make it exact.");
            return Resolution.Success(candidates[0]);
        }

        static string Cap(string value)=>string.IsNullOrEmpty(value)?value:char.ToUpperInvariant(value[0])+value.Substring(1);

        static IEnumerable<MethodBase> Members(Type type,MethodReference reference)
        {
            const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
            try
            {
                var values=new List<MethodBase>();
                if(reference.MemberKind==PatchMemberKind.Constructor)
                {
                    if(reference.IsStatic==true)
                    {
                        if(type.TypeInitializer!=null)values.Add(type.TypeInitializer);
                        return values;
                    }
                    values.AddRange(type.GetConstructors(flags));
                    return values;
                }
                values.AddRange(type.GetMethods(flags).Where(method=>string.Equals(method.Name,reference.MemberName,StringComparison.Ordinal)));
                return values;
            }
            catch { return Enumerable.Empty<MethodBase>(); }
        }

        static bool Matches(MethodBase member,MethodReference reference)
        {
            var info=member as MethodInfo;
            if(reference.MemberKind==PatchMemberKind.Method && (info==null || (info.IsGenericMethod?info.GetGenericArguments().Length:0)!=reference.GenericArity))return false;
            if(reference.MemberKind==PatchMemberKind.Constructor && reference.GenericArity!=0)return false;
            if(reference.IsStatic.HasValue && member.IsStatic!=reference.IsStatic.Value)return false;
            var parameters=member.GetParameters();
            var declared=reference.ParameterTypes??new List<TypeReference>();
            if(parameters.Length!=declared.Count)return false;
            for(var index=0;index<parameters.Length;index++)if(!Matches(parameters[index].ParameterType,declared[index]))return false;
            return reference.ReturnType==null || (info!=null && Matches(info.ReturnType,reference.ReturnType));
        }

        static bool Matches(Type actual,TypeReference declared)
        {
            if(actual==null || declared==null)return false;
            // Reflection reports the runtime implementation assembly for a generic
            // parameter on some CLR versions. Its declaring module is the stable source
            // identity captured by MethodReference.From.
            if(actual.IsGenericParameter)
            {
                // Names can collide between type-scoped and method-scoped parameters
                // (for example, both may be named T). Compare CLR owner-kind and
                // position tokens instead of accepting a potentially ambiguous name.
                string positionalName=(actual.DeclaringMethod==null?"!":"!!")+actual.GenericParameterPosition;
                return string.Equals(positionalName,declared.FullName,StringComparison.Ordinal);
            }
            if(!string.Equals(actual.FullName??actual.Name,declared.FullName,StringComparison.Ordinal))return false;
            return string.IsNullOrWhiteSpace(declared.AssemblyName) || string.Equals(actual.Assembly.GetName().Name,declared.AssemblyName,StringComparison.OrdinalIgnoreCase);
        }
        static bool ValidTypes(IEnumerable<TypeReference> values)=>values!=null && values.All(ValidType);
        static bool ValidType(TypeReference value)=>value!=null && !string.IsNullOrWhiteSpace(value.FullName);
        static string AssemblyName(Assembly assembly)
        {
            try{return assembly?.GetName().Name??"";}
            catch{return "";}
        }
        static List<Assembly> LoadedAssemblies(IEnumerable<Assembly> assemblies)
        {
            var result=new List<Assembly>();
            foreach(var assembly in assemblies??Enumerable.Empty<Assembly>()) if(assembly!=null && !result.Contains(assembly))result.Add(assembly);
            return result;
        }
        static string Signature(MethodBase member)
        {
            try{return "("+string.Join(", ",member.GetParameters().Select(parameter=>Safe(parameter.ParameterType?.FullName??parameter.ParameterType?.Name)))+")";}
            catch{return "(?)";}
        }
        static string Safe(string value)=>string.IsNullOrEmpty(value)?"":value.Length>512?value.Substring(0,512)+"…":value;
        static string Code(string value)
        {
            var characters=(value??"invalid").ToLowerInvariant().Select(character=>char.IsLetterOrDigit(character)?character:'_').ToArray();
            return new string(characters).Trim('_');
        }
        static void AddFinding(PatchPreflightSnapshot result,string level,string code,string module,string message,string suggestion)
        {
            if(result.Findings.Count<MaximumFindings)result.Findings.Add(new Finding {Level=level,Code=code,Module=Safe(module),Message=Safe(message),Suggestion=Safe(suggestion)});
        }
        static void AddNote(PatchPreflightSnapshot result,string note)
        {
            if(result.Notes.Count<50 && !result.Notes.Contains(note))result.Notes.Add(Safe(note));
        }
        static void AddNote(PatchPreflightOutcome outcome,string note)
        {
            if(outcome.Notes.Count<30 && !outcome.Notes.Contains(note))outcome.Notes.Add(Safe(note));
        }
        static Finding CopyFinding(Finding source)=>new Finding {Level=Safe(source.Level),Code=Safe(source.Code),Module=Safe(source.Module),File=Safe(source.File),Message=Safe(source.Message),Suggestion=Safe(source.Suggestion)};
        static PatchBlueprintDeclaration CopyDeclaration(PatchBlueprintDeclaration source)
        {
            if(source==null)return new PatchBlueprintDeclaration {Blueprint=new PatchBlueprint()};
            return new PatchBlueprintDeclaration {ProviderId=Safe(source.ProviderId),Module=Safe(source.Module),ProviderName=Safe(source.ProviderName),Context=source.Context,Blueprint=PatchBlueprintCopies.Copy(source.Blueprint)};
        }
        sealed class Resolution
        {
            public MethodBase Member { get; private set; }
            public string Status { get; private set; }
            public string Error { get; private set; }
            public string Suggestion { get; private set; }
            public static Resolution Success(MethodBase member)=>new Resolution {Member=member};
            public static Resolution Fail(string status,string error,string suggestion)=>new Resolution {Status=status,Error=error,Suggestion=suggestion};
        }
    }

    internal static class PatchBlueprintCopies
    {
        const int MaximumText=512;
        const int MaximumList=32;
        internal static PatchBlueprint Copy(PatchBlueprint source)
        {
            if(source==null)return new PatchBlueprint();
            return new PatchBlueprint {
                Id=Identifier(source.Id),Name=Text(source.Name),Hook=source.Hook,Target=Copy(source.Target),PatchMethod=Copy(source.PatchMethod),Priority=source.Priority,
                Before=Strings(source.Before),After=Strings(source.After),Rationale=Text(source.Rationale)
            };
        }
        static MethodReference Copy(MethodReference source)
        {
            if(source==null)return null;
            return new MethodReference {AssemblyName=Text(source.AssemblyName),DeclaringType=Text(source.DeclaringType),MemberKind=source.MemberKind,MemberName=Text(source.MemberName),GenericArity=source.GenericArity,ParameterTypes=(source.ParameterTypes??new List<TypeReference>()).Take(MaximumList).Select(Copy).ToList(),ReturnType=Copy(source.ReturnType),IsStatic=source.IsStatic};
        }
        static TypeReference Copy(TypeReference source)=>source==null?null:new TypeReference {AssemblyName=Text(source.AssemblyName),FullName=Text(source.FullName)};
        // Preserve blank entries for validation and one overflow sentinel for bounded diagnostics.
        static List<string> Strings(IEnumerable<string> values)=>values==null?new List<string>():values.Take(MaximumList+1).Select(Text).ToList();
        static string Text(string value)=>string.IsNullOrEmpty(value)?value:value.Length>MaximumText?value.Substring(0,MaximumText)+"…":value;
        // Preserve a detectable over-limit sentinel so preflight can reject rather than
        // silently alias two distinct long IDs after display-text truncation.
        static string Identifier(string value)=>string.IsNullOrEmpty(value)?value:value.Length>MaximumText?value.Substring(0,MaximumText+1):value;
    }
}
