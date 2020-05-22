namespace GameHost.Input
{
    public struct InputState
    {
        /// <summary>
        /// Used for axis measurement
        /// </summary>
        public float Real;

        /// <summary>
        /// How much time was this input pressed or released?
        /// </summary>
        public uint Down, Up;

        /// <summary>
        /// Is  this input currently active?
        /// </summary>
        public bool Active;
    }
}
