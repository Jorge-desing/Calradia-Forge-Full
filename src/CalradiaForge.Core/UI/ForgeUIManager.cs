#if NET472
using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaForge.Core.UI
{
    /// <summary>
    /// A helper utility designed for new modders to easily load and display Gauntlet UI movies
    /// without having to manually manage ScreenManager layers and input scopes.
    /// </summary>
    public static class ForgeUIManager
    {
        public class ForgeUIContext
        {
            public GauntletLayer Layer { get; internal set; }
            public object Movie { get; internal set; }
            public ViewModel ViewModel { get; internal set; }

            /// <summary>
            /// Closes and removes the UI from the screen.
            /// </summary>
            public void Close()
            {
                if (Layer != null)
                {
                    ScreenManager.TopScreen?.RemoveLayer(Layer);
                    Layer = null;
                }
            }
        }

        /// <summary>
        /// Instantiates a Gauntlet XML UI with the provided ViewModel and displays it on the screen.
        /// </summary>
        /// <param name="movieName">The name of the XML file in GUI/Prefabs (without .xml)</param>
        /// <param name="viewModel">Your ViewModel instance</param>
        /// <param name="catchInput">If true, the UI will consume mouse and keyboard input (like a pause menu).</param>
        /// <returns>A context object you can use to Close() the UI later.</returns>
        public static ForgeUIContext Show(string movieName, ViewModel viewModel, bool catchInput = false)
        {
            if (ScreenManager.TopScreen == null)
            {
                Diagnostics.ForgeLogger.PrintError("Cannot show UI: TopScreen is null.");
                return null;
            }

            var layer = new GauntletLayer("GauntletLayer", 100);
            var movie = layer.LoadMovie(movieName, viewModel);

            if (catchInput)
            {
                layer.InputRestrictions.SetInputRestrictions(true, TaleWorlds.Library.InputUsageMask.All);
            }

            ScreenManager.TopScreen.AddLayer(layer);

            return new ForgeUIContext
            {
                Layer = layer,
                Movie = movie,
                ViewModel = viewModel
            };
        }
    }
}
#endif
