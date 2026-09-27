namespace CalradiaForge.Core
{
    /// <summary>Release metadata shared by the game module and desktop companion.</summary>
    public static class SuiteInfo
    {
        // Directory.Build.props supplies this assembly version for every Forge project.
        // Keeping it derived prevents the desktop, module, and SDK from drifting apart.
        public static string Version
        {
            get
            {
                var version = typeof(SuiteInfo).Assembly.GetName().Version;
                return version == null ? "0.0.0" : version.ToString(3);
            }
        }
        public const string TargetGameVersion = "1.4.8";
    }
}
