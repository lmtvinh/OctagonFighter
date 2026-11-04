namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Exposes whether a combatant is currently guarding incoming strikes.
    /// </summary>
    /// <remarks>
    /// Combat resolution queries this before applying damage so a blocked strike can be consumed
    /// by the hit registry without reaching health or starting a hit reaction.
    /// </remarks>
    public interface IGuardState
    {
        /// <summary>
        /// Gets a value indicating whether the combatant is currently blocking.
        /// </summary>
        bool IsBlocking { get; }
    }
}
