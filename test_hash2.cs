using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

class Program {
    static void Main() {
        var file = File.ReadAllText("src\\CalradiaForge.Mod\\PanelViewModel.cs");
        var start = file.IndexOf("Memory Inspector: Enter");
        var end = file.IndexOf(", start);
        var key = file.Substring(start, end - start);
        
        using(var hash=SHA256.Create()) {
            var bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(key));
            var identifier=new StringBuilder("forge_");
            for(var i=0;i<6;i++)identifier.Append(bytes[i].ToString("x2"));
            Console.WriteLine(identifier.ToString() + " -> " + key + "");
        }
    }
}
