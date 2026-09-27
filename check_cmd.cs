using System;
using System.Reflection;
class Program {
    static void Main() {
        try {
            var asm = Assembly.LoadFrom(@"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.Library.dll");
            var type = asm.GetType("TaleWorlds.Library.CommandLineFunctionality");
            var method = type.GetMethod("CallFunction");
            Console.WriteLine(method.ToString());
        } catch {}
    }
}
