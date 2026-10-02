using System.Diagnostics;

namespace CalradiaForge.Mod
{
    /// <summary>
    /// Coalesces the F10 pressed/down signals into one activation per physical press.
    /// A short all-false gap is tolerated before re-arming because the game input APIs
    /// can report the same held press through different signals or ticks.
    /// </summary>
    internal static class F10InputEdge
    {
        internal static readonly long ReleaseDebounceTicks = Stopwatch.Frequency / 20;

        internal static bool Update(bool signalActive, long timestamp, ref bool wasActive, ref long inactiveSinceTimestamp)
        {
            if (signalActive)
            {
                inactiveSinceTimestamp = 0;
                bool risingEdge = !wasActive;
                wasActive = true;
                return risingEdge;
            }

            if (!wasActive)
            {
                inactiveSinceTimestamp = 0;
                return false;
            }

            if (inactiveSinceTimestamp == 0)
            {
                inactiveSinceTimestamp = timestamp;
                return false;
            }

            if (timestamp - inactiveSinceTimestamp < ReleaseDebounceTicks)
                return false;

            wasActive = false;
            inactiveSinceTimestamp = 0;
            return false;
        }
    }
}
