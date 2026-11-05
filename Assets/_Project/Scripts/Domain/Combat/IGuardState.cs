using OctagonFighter.Domain.Guard;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Exposes a combatant's guard resource and resolves blocked strikes.
    /// </summary>
    /// <remarks>
    /// Combat resolution queries <see cref="IsBlocking"/> before applying damage. A blocked strike
    /// is then passed to <see cref="ApplyBlock(AttackDefinition, DamageEvent)"/> so the guard,
    /// chip health damage, stamina cost, and block reaction are resolved together.
    /// </remarks>
    public interface IGuardState : IGuardSource
    {
        /// <summary>
        /// Gets a value indicating whether the combatant is currently guarding incoming strikes.
        /// </summary>
        bool IsBlocking { get; }

        /// <summary>
        /// Resolves a blocked strike that carries authored guard values.
        /// </summary>
        BlockResolution ApplyBlock(in AttackDefinition attack, in DamageEvent damage);

        /// <summary>
        /// Resolves a blocked hit that has no authored strike metadata.
        /// </summary>
        BlockResolution ApplyBlock(in DamageEvent damage);
    }
}
