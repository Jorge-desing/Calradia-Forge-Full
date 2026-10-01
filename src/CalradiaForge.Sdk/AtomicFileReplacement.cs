using System;
using System.IO;
using System.Threading;

namespace CalradiaForge.Sdk
{
    // Win32 1175 leaves both names intact. Retry that specific refusal only;
    // never substitute delete/move or retry errors with uncertain file outcomes.
    internal static class AtomicFileReplacement
    {
        internal static void Replace(string source, string destination,
            Action<string, string> replace = null, Action<int> wait = null)
        {
            replace = replace ?? ((from, to) => File.Replace(from, to, null));
            wait = wait ?? Thread.Sleep;
            for (int attempt = 0; ; attempt++)
            {
                try { replace(source, destination); return; }
                catch (IOException error) when (error.HResult == unchecked((int)0x80070497) &&
                    attempt < 3 && File.Exists(source) && File.Exists(destination))
                {
                    wait(20 * (attempt + 1));
                }
            }
        }
    }
}
