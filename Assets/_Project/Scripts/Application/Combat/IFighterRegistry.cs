using OctagonFighter.Core.Ids;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Application.Combat
{
    /// <summary>
    /// Resolves fighter identifiers to damage receivers.
    /// </summary>
    public interface IFighterRegistry
    {
        /// <summary>
        /// Attempts to resolve the damage receiver for a fighter.
        /// </summary>
        bool TryGetDamageable(FighterId fighterId, out IDamageable damageable);
    }
}
