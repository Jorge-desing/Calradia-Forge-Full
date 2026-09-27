using System;

namespace CalradiaForge.Sdk
{
    public enum ForgeLogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class ForgeLogger
    {
        public static event Action<ForgeLogLevel, string, Exception> OnLogEmitted;

        public static void Log(string message, ForgeLogLevel level = ForgeLogLevel.Info)
        {
            OnLogEmitted?.Invoke(level, message, null);
        }

        public static void LogError(string message, Exception ex = null)
        {
            OnLogEmitted?.Invoke(ForgeLogLevel.Error, message, ex);
        }

        public static void LogWarning(string message)
        {
            OnLogEmitted?.Invoke(ForgeLogLevel.Warning, message, null);
        }
    }
}
