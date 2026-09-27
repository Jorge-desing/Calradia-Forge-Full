using System;
using System.Security.Cryptography;
using System.Text;

class Program {
    static void Main() {
        string[] strings = {
            "Memory Inspector: Enter an object ID in the search bar. Use [Pin] to save state, and [Compare] to diff against snapshots.",
            "Suggestions: Native, SandBox, StoryMode, CalradiaForge",
            "Copy to clipboard",
            "Suggestions: campaign.add_gold_to_hero, help"
        };
        
        using(var hash=SHA256.Create()) {
            foreach(var key in strings) {
                var bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(key));
                var identifier=new StringBuilder("forge_");
                for(var i=0;i<6;i++)identifier.Append(bytes[i].ToString("x2"));
                Console.WriteLine(identifier.ToString() + " -> " + key);
            }
        }
    }
}
