using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Decides whether an intent or buffered input starts an attack and which state it targets.
    /// </summary>
    public static class AttackSelection
    {
        /// <summary>
        /// Attempts to select an attack state for the supplied intent.
        /// </summary>
        /// <remarks>
        /// A pressed punch command takes priority. When no punch is pressed this tick, a
        /// previously buffered punch is consumed instead, which lets an input pressed during
        /// a committed action start as soon as the fighter is able to strike.
        /// </remarks>
        public static bool TrySelect(
            in FighterIntent intent,
            in FighterStateContext context,
            out FighterStateId targetState)
        {
            targetState = default;

            AttackSlot slot = AttackSlot.None;
            if (intent.IsCommandPressed)
            {
                switch (intent.Command)
                {
                    case FighterCommand.LightPunch:
                        slot = AttackSlot.Light;
                        break;

                    case FighterCommand.HeavyPunch:
                        slot = AttackSlot.Heavy;
                        break;
                }
            }

            if (slot == AttackSlot.None && !context.Attacks.TryGetBufferedAttack(out slot))
            {
                return false;
            }

            if (!context.Data.Attacks.TryGet(slot, out AttackDefinition attack))
            {
                return false;
            }

            if (!context.Attacks.CanStart(slot))
            {
                return false;
            }

            if (!context.Stamina.CanAfford(new StaminaCost(attack.StaminaCost)))
            {
                return false;
            }

            targetState = slot == AttackSlot.Light
                ? FighterStateId.LightPunch
                : FighterStateId.HeavyPunch;
            return true;
        }
    }
}
