using System;
using System.Collections.Generic;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    // ForgeApi preserves successful registrations even when another extension throws.
    // The game host needs that partial-success state without letting one callback end its UI tick.
    public sealed class ExtensionStartupResult
    {
        public bool Connected { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
    public static class ExtensionStartup
    {
        public static ExtensionStartupResult Connect(IForgeRegistry registry)
        {
            var result=new ExtensionStartupResult();
            try { ForgeApi.Connect(registry); }
            catch(AggregateException error)
            {
                foreach(var item in error.Flatten().InnerExceptions) result.Errors.Add(Describe(item));
            }
            catch(Exception error) { result.Errors.Add(Describe(error)); }
            result.Connected=ForgeApi.Registry!=null;
            return result;
        }
        static string Describe(Exception error)
        {
            var message=error?.Message??"Unknown extension startup error";
            return message.Length>512?message.Substring(0,512)+"…":message;
        }
    }
}
