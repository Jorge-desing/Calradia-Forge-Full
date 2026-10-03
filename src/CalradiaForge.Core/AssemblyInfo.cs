using System.Runtime.CompilerServices;

// The game module coordinates runtime shutdown through this internal lifecycle hook;
// keep it out of the public Core API surface.
[assembly: InternalsVisibleTo("CalradiaForge.Mod")]
[assembly: InternalsVisibleTo("CalradiaForge.Tests")]
