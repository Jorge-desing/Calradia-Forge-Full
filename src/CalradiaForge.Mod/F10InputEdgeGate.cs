namespace CalradiaForge.Mod
{
    internal struct F10InputEdgeGate
    {
        private bool signalWasPresent;

        internal bool Poll(bool keyPressed, bool keyDown, bool keyDownImmediate)
        {
            var signalIsPresent = keyPressed || keyDown || keyDownImmediate;
            var isNewPress = signalIsPresent && !signalWasPresent;
            signalWasPresent = signalIsPresent;
            return isNewPress;
        }

        internal void Reset()
        {
            signalWasPresent = false;
        }
    }
}
