namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Movement gait requested by an intent source.
    /// </summary>
    public enum MovementGait : byte
    {
        /// <summary>
        /// Controlled walking movement.
        /// </summary>
        Walk = 0,

        /// <summary>
        /// Committed running movement.
        /// </summary>
        Run = 1
    }
}
