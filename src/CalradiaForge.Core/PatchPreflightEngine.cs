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
            var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var limit=Math.Max(1,Math.Min(maximumOutcomes,MaximumOutcomes));
            foreach(var declaration in capture.Declarations??new List<PatchBlueprintDeclaration>())
            {
                if(result.Outcomes.Count>=limit)
                {
                    result.Truncated=true;
                    AddFinding(result,"Warning","patch_blueprint_limit",null,"Patch blueprint preflight reached its display limit.","Run focused providers or narrow the registered blueprint set.");
                    break;
                }
                var outcome=new PatchPreflightOutcome {Declaration=CopyDeclaration(declaration)};
                result.BlueprintCount++;
                var blueprint=outcome.Declaration.Blueprint;
                var id=blueprint?.Id;
                if(string.IsNullOrWhiteSpace(id))
                {
                    Block(result,outcome,"Invalid blueprint","A blueprint ID is required.","Give every blueprint a stable ID.");
                }
                else if(!ids.Add(id))
                {
                    Block(result,outcome,"Duplicate blueprint ID","Another provider declared the same blueprint ID: "+id+".","Use one unique ID per registered blueprint.");
                }
                else if(blueprint==null || !Enum.IsDefined(typeof(PatchHookKind),blueprint.Hook))
                {
                    Block(result,outcome,"Invalid blueprint","The declared hook kind is not supported.","Use Prefix, Postfix, Transpiler, or Finalizer.");
                }
                else
                {
                    var target=Resolve(blueprint.Target,loaded);
                    if(target.Error!=null)
                    {
                        Block(result,outcome,target.Status,target.Error,target.Suggestion);
                    }
                    else
                    {
                        outcome.Resolved=true;
                        outcome.Status="Resolved";
                        outcome.ResolvedAssembly=Safe(target.Member.DeclaringType?.Assembly.GetName().Name);
                        outcome.ResolvedType=Safe(target.Member.DeclaringType?.FullName);
                        outcome.ResolvedMember=target.Member.IsConstructor?".ctor":Safe(target.Member.Name);
                        outcome.ResolvedSignature=Signature(target.Member);
                        result.ResolvedCount++;
                        AddOrderNotes(result,outcome);
                    }
                }
                result.Outcomes.Add(outcome);
            }
            if(result.Outcomes.Count==0)
            {
                result.Status=result.ProviderCount==0
                    ? "No patch blueprint providers are registered in this session."
                    : "No patch blueprints were returned for this context.";
            }
            else
            {
                var blocked=result.Outcomes.Count(outcome=>!outcome.Resolved);
                if(blocked>0) result.Status="Blocked: "+blocked+" declared target"+(blocked==1?" needs":"s need")+" attention. No patch is applied.";
                else if(result.ReviewCount>0) result.Status="Review required: inspect the declared ordering notes. No patch is applied.";
                else result.Status="No blocking issues found. No patch is applied; this preflight does not prove runtime compatibility.";
            }
            if(result.Truncated)AddNote(result,"Results are bounded; run focused providers for a complete review.");
            return result;
        }

        static void AddOrderNotes(PatchPreflightSnapshot result,PatchPreflightOutcome outcome)
        {
            var blueprint=outcome.Declaration.Blueprint;
            var id=blueprint.Id??"";
            var before=blueprint.Before??new List<string>();
            var after=blueprint.After??new List<string>();
            if(before.Any(value=>string.Equals(value,id,StringComparison.OrdinalIgnoreCase)) || after.Any(value=>string.Equals(value,id,StringComparison.OrdinalIgnoreCase)))
            {
                result.ReviewCount++;
                AddNote(outcome,"The declared Before/After list names this blueprint itself; review the intended owner IDs.");
                AddFinding(result,"Warning","patch_order_self",outcome.Declaration.Module,"Blueprint '"+id+"' contains a self ordering declaration.","Remove the self reference or use the intended external owner ID.");
            }
            if(before.Count>0 || after.Count>0)AddNote(outcome,"Before/After are author declarations. Forge does not infer final patch execution order.");
        }

        static void Block(PatchPreflightSnapshot result,PatchPreflightOutcome outcome,string status,string message,string suggestion)
        {
            outcome.Status=status;
            AddNote(outcome,message);
            AddFinding(result,"Warning","patch_blueprint_"+Code(status),outcome.Declaration.Module,message,suggestion);
        }

        static Resolution Resolve(MethodReference reference,IEnumerable<Assembly> assemblies)
        {
            if(reference==null)return Resolution.Fail("Invalid target","A target method reference is required.","Provide an exact assembly, type, member, and parameter list.");
            if(string.IsNullOrWhiteSpace(reference.AssemblyName) || string.IsNullOrWhiteSpace(reference.DeclaringType))return Resolution.Fail("Invalid target","Target assembly and declaring type are required.","Use MethodReference.From or provide exact type metadata.");
            if(!Enum.IsDefined(typeof(PatchMemberKind),reference.MemberKind))return Resolution.Fail("Invalid target","The target member kind is not supported.","Use Method or Constructor.");
            if(reference.GenericArity<0)return Resolution.Fail("Invalid target","Generic arity cannot be negative.","Use zero for a non-generic method.");
            if(reference.MemberKind==PatchMemberKind.Method && string.IsNullOrWhiteSpace(reference.MemberName))return Resolution.Fail("Invalid target","A target method name is required.","Provide the exact member name.");
            if(!ValidTypes(reference.ParameterTypes))return Resolution.Fail("Invalid target","Each parameter needs an exact type full name.","Use TypeReference.From for every parameter.");
            if(reference.ReturnType!=null && !ValidType(reference.ReturnType))return Resolution.Fail("Invalid target","The return-type assertion is incomplete.","Use TypeReference.From or omit the optional return type.");
            var matching=assemblies.Where(assembly=>AssemblyName(assembly).Equals(reference.AssemblyName,StringComparison.OrdinalIgnoreCase)).ToList();
            if(matching.Count==0)return Resolution.Fail("Assembly not loaded","Target assembly '"+reference.AssemblyName+"' is not loaded in this session.","Load the owning module before running this preflight.");
            if(matching.Count>1)return Resolution.Fail("Ambiguous assembly","More than one loaded assembly uses the simple name '"+reference.AssemblyName+"'.","Use a session without duplicate assembly identities.");
            Type type;
            try {type=matching[0].GetType(reference.DeclaringType,false,false);}
            catch(Exception error) {return Resolution.Fail("Type unreadable","Target type could not be inspected: "+Safe(error.Message),"Check that the target assembly is compatible with this game build.");}
            if(type==null)return Resolution.Fail("Type not found","Target type '"+reference.DeclaringType+"' was not found in '"+reference.AssemblyName+"'.","Use the exact full type name for the installed game build.");
            var candidates=Members(type,reference).Where(member=>Matches(member,reference)).ToList();
            if(candidates.Count==0)return Resolution.Fail("Member not found","No loaded member matches the declared target signature.","Specify every parameter type and verify the target game version.");
            if(candidates.Count>1)return Resolution.Fail("Ambiguous target","More than one member matches the declared target signature.","Add a return type, static assertion, or generic arity to make it exact.");
            return Resolution.Success(candidates[0]);
        }

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
                Id=Text(source.Id),Name=Text(source.Name),Hook=source.Hook,Target=Copy(source.Target),PatchMethod=Copy(source.PatchMethod),Priority=source.Priority,
                Before=Strings(source.Before),After=Strings(source.After),Rationale=Text(source.Rationale)
            };
        }
        static MethodReference Copy(MethodReference source)
        {
            if(source==null)return null;
            return new MethodReference {AssemblyName=Text(source.AssemblyName),DeclaringType=Text(source.DeclaringType),MemberKind=source.MemberKind,MemberName=Text(source.MemberName),GenericArity=source.GenericArity,ParameterTypes=(source.ParameterTypes??new List<TypeReference>()).Take(MaximumList).Select(Copy).ToList(),ReturnType=Copy(source.ReturnType),IsStatic=source.IsStatic};
        }
        static TypeReference Copy(TypeReference source)=>source==null?null:new TypeReference {AssemblyName=Text(source.AssemblyName),FullName=Text(source.FullName)};
        static List<string> Strings(IEnumerable<string> values)=>values==null?new List<string>():values.Where(value=>!string.IsNullOrWhiteSpace(value)).Take(MaximumList).Select(Text).ToList();
        static string Text(string value)=>string.IsNullOrEmpty(value)?value:value.Length>MaximumText?value.Substring(0,MaximumText)+"…":value;
    }
}
