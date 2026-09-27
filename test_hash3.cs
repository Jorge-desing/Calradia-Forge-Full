using System;
using System.Security.Cryptography;
using System.Text;
class Program {
    static void Main() {
        var key = "Unknown test";
        using(var hash=SHA256.Create()) {
            var bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(key));
            var identifier=new StringBuilder("forge_");
            for(var i=0;i<6;i++)identifier.Append(bytes[i].ToString("x2"));
            Console.WriteLine(identifier.ToString());
        }
    }
}
