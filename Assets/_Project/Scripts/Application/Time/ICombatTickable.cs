namespace OctagonFighter.Application.Time
{
    /// <summary>
    /// Receives fixed combat ticks from the tick driver.
    /// </summary>
    public interface ICombatTickable
    {
        /// <summary>
        /// Advances the receiver by one fixed tick.
        /// </summary>
        void Tick(float deltaSeconds);
    }
}
