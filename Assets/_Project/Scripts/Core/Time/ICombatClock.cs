namespace OctagonFighter.Core.Time
{
    /// <summary>
    /// Supplies the current fixed simulation tick.
    /// </summary>
    public interface ICombatClock
    {
        /// <summary>
        /// Gets the tick currently being simulated.
        /// </summary>
        TickId CurrentTick { get; }
    }
}
