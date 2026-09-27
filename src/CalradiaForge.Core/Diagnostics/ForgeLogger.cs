#if NET472
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CalradiaForge.Core.Diagnostics
{
    /// <summary>
    /// A simplified logging utility designed for new modders to easily output information
    /// to the Bannerlord in-game message log without dealing with Colors and InformationManager syntax.
    /// </summary>
    public static class ForgeLogger
    {
        public static void Print(string message, string colorHex = "#FFFFFFFF")
        {
            try
            {
                var color = Color.ConvertStringToColor(colorHex);
                InformationManager.DisplayMessage(new InformationMessage(message, color));
            }
            catch
            {
                // Fallback in case of parsing error
                InformationManager.DisplayMessage(new InformationMessage(message));
            }
        }

        public static void PrintSuccess(string message) => Print("[Forge] " + message, "#00FF00FF"); // Green
        public static void PrintWarning(string message) => Print("[Forge] " + message, "#FFFF00FF"); // Yellow
        public static void PrintError(string message) => Print("[Forge Error] " + message, "#FF0000FF"); // Red
        public static void PrintInfo(string message) => Print("[Forge] " + message, "#00FFFFFF"); // Cyan
    }
}
#endif
