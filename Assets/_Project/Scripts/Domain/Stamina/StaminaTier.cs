namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Fatigue tier derived from current stamina.
    /// </summary>
    public enum StaminaTier : byte
    {
        /// <summary>
        /// Fully fresh.
        /// </summary>
        Fresh = 0,

        /// <summary>
        /// Normal working capacity.
        /// </summary>
        Working = 1,

        /// <summary>
        /// Visibly fatigued.
        /// </summary>
        Tired = 2,

        /// <summary>
        /// Severely depleted.
        /// </summary>
        Gassed = 3
    }
}
