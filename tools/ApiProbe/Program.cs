using System;
using System.IO;
using System.Linq;
using System.Reflection;
class Program {
 static void Main(string[] args) {
 var dir = args[0];
 AppDomain.CurrentDomain.AssemblyResolve += (s,e) => { var p=Path.Combine(dir,new AssemblyName(e.Name).Name+".dll"); return File.Exists(p)?Assembly.LoadFrom(p):null; };
 foreach(var path in Directory.GetFiles(dir,"TaleWorlds*.dll")) {
 try {var asm=Assembly.LoadFrom(path); foreach(var t in asm.GetTypes().Where(t=>args.Skip(1).Contains(t.Name))) {
 Console.WriteLine("TYPE "+t.FullName+" : "+t.BaseType);
 foreach(var m in t.GetMembers(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).Where(m=>m.MemberType != MemberTypes.Field)) {Console.WriteLine("  "+m); if(m is MethodBase method) foreach(var p in method.GetParameters().Where(p=>p.HasDefaultValue)) Console.WriteLine("    default "+p.Name+"="+p.DefaultValue);}
 }} catch(ReflectionTypeLoadException) {} catch(BadImageFormatException) {} }
 }
}
