#if NET472
using TaleWorlds.Localization;

namespace CalradiaForge.Core.Text
{
    /// <summary>
    /// Utility for easily working with Bannerlord's Native Localization system.
    /// </summary>
    public static class ForgeLocalization
    {
        /// <summary>
        /// Translates a raw string by automatically generating its TextObject representation.
        /// Useful if your localization XMLs map ID tags like "{=my_id}My String".
        /// </summary>
        public static TextObject T(string textId)
        {
            return new TextObject(textId);
        }

        /// <summary>
        /// Registers a dynamic string in-game without needing an XML file.
        /// </summary>
        public static TextObject CreateDynamic(string rawText)
        {
            return new TextObject(rawText);
        }
    }
}
#endif
