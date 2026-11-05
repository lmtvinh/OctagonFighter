using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.StateMachine;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Action chosen by the <see cref="TrainingIntentPolicy"/> for one tick.
    /// </summary>
    public enum TrainingAction : byte
    {
        /// <summary>
        /// No intent.
        /// </summary>
        None = 0,

        /// <summary>
        /// Hold the defensive block.
        /// </summary>
        Block = 1,

        /// <summary>
        /// Move toward the opponent.
        /// </summary>
        MoveForward = 2,

        /// <summary>
        /// Move away from the opponent.
        /// </summary>
        MoveBackward = 3,

        /// <summary>
        /// Throw the light strike (jab).
        /// </summary>
        Jab = 4,

        /// <summary>
        /// Throw the heavy strike (cross).
        /// </summary>
        Cross = 5
    }

    /// <summary>
    /// Training opponent modes.
    /// </summary>
    public enum TrainingMode : byte
    {
        /// <summary>
        /// Stand still and never attack.
        /// </summary>
        Passive = 0,

        /// <summary>
        /// Approach until within strike range, then stop.
        /// </summary>
        Approach = 1,

        /// <summary>
        /// Retreat until outside strike range, then stop.
        /// </summary>
        Retreat = 2,

        /// <summary>
        /// Hold the guard continuously (manual training aid, not AI defense).
        /// </summary>
        HoldBlock = 3,

        /// <summary>
        /// Throw jabs when in range and off cooldown; otherwise approach.
        /// </summary>
        Jab = 4,

        /// <summary>
        /// Throw crosses when in range and off cooldown; otherwise approach.
        /// </summary>
        Cross = 5,

        /// <summary>
        /// Alternate jab and cross when in range and off cooldown; otherwise approach.
        /// </summary>
        JabCross = 6
    }

    /// <summary>
    /// Pure, deterministic decision policy for the training opponent.
    /// </summary>
    /// <remarks>
    /// The policy owns no UnityEngine types and no scene references. It receives primitive inputs
    /// each tick and returns a <see cref="TrainingAction"/>. The <see cref="TrainingIntentSource"/>
    /// MonoBehaviour adapter converts the action into a <see cref="FighterIntent"/> using the
    /// fighter's actual position and orientation.
    ///
    /// The policy never attacks while the fighter is blocking, stunned (hit reaction / guard break),
    /// or defeated, because those states are not attack-eligible. It only attacks when distance is
    /// within <see cref="StrikeRange"/>, the state is <see cref="FighterStateId.Idle"/>,
    /// <see cref="FighterStateId.Walk"/> or <see cref="FighterStateId.Run"/>, and the authored
    /// cooldown (via <see cref="AttackTracker.CanStart"/>) has elapsed.
    /// </remarks>
    public sealed class TrainingIntentPolicy
    {
        private bool _nextIsCross;

        /// <summary>
        /// Gets or sets the training mode.
        /// </summary>
        public TrainingMode Mode { get; set; } = TrainingMode.Passive;

        /// <summary>
        /// Gets or sets the maximum distance at which the policy will attempt a strike.
        /// </summary>
        public float StrikeRange { get; set; } = 1.05f;

        /// <summary>
        /// Gets or sets the target distance maintained by Approach and Retreat modes.
        /// </summary>
        public float ApproachDistance { get; set; } = 1.2f;

        /// <summary>
        /// Decides the action for one tick.
        /// </summary>
        /// <param name="distanceToOpponent">Planar distance to the opponent in meters.</param>
        /// <param name="state">Current fighter state.</param>
        /// <param name="movementAuthority">Current movement authority.</param>
        /// <param name="canStartLight"><see cref="AttackTracker.CanStart"/> for the light slot.</param>
        /// <param name="canStartHeavy"><see cref="AttackTracker.CanStart"/> for the heavy slot.</param>
        /// <param name="isBlocking">Whether the fighter is currently guarding.</param>
        /// <returns>The action to take this tick.</returns>
        public TrainingAction Decide(
            float distanceToOpponent,
            FighterStateId state,
            MovementAuthority movementAuthority,
            bool canStartLight,
            bool canStartHeavy,
            bool isBlocking)
        {
            bool attackEligible =
                state == FighterStateId.Idle ||
                state == FighterStateId.Walk ||
                state == FighterStateId.Run;

            switch (Mode)
            {
                case TrainingMode.Passive:
                    return TrainingAction.None;

                case TrainingMode.HoldBlock:
                    return TrainingAction.Block;

                case TrainingMode.Approach:
                    return distanceToOpponent > ApproachDistance
                        ? TrainingAction.MoveForward
                        : TrainingAction.None;

                case TrainingMode.Retreat:
                    return distanceToOpponent < ApproachDistance
                        ? TrainingAction.MoveBackward
                        : TrainingAction.None;

                case TrainingMode.Jab:
                    return DecideStrike(
                        AttackSlot.Light,
                        canStartLight,
                        attackEligible,
                        distanceToOpponent,
                        isBlocking,
                        TrainingAction.Jab);

                case TrainingMode.Cross:
                    return DecideStrike(
                        AttackSlot.Heavy,
                        canStartHeavy,
                        attackEligible,
                        distanceToOpponent,
                        isBlocking,
                        TrainingAction.Cross);

                case TrainingMode.JabCross:
                    return DecideJabCross(
                        canStartLight,
                        canStartHeavy,
                        attackEligible,
                        distanceToOpponent,
                        isBlocking);
            }

            return TrainingAction.None;
        }

        private TrainingAction DecideStrike(
            AttackSlot slot,
            bool canStart,
            bool attackEligible,
            float distanceToOpponent,
            bool isBlocking,
            TrainingAction strikeAction)
        {
            // Never strike while blocking, stunned, defeated, out of range, or on cooldown.
            if (attackEligible && !isBlocking && canStart && distanceToOpponent <= StrikeRange)
            {
                return strikeAction;
            }

            // When not striking, close the distance if we are outside the approach threshold.
            return distanceToOpponent > ApproachDistance
                ? TrainingAction.MoveForward
                : TrainingAction.None;
        }

        private TrainingAction DecideJabCross(
            bool canStartLight,
            bool canStartHeavy,
            bool attackEligible,
            float distanceToOpponent,
            bool isBlocking)
        {
            // Only alternate strikes while actually landing one. Cooldown, range, block, stun and
            // defeat all suppress the strike without consuming the alternation turn.
            if (attackEligible && !isBlocking && distanceToOpponent <= StrikeRange)
            {
                bool useCross = _nextIsCross;
                bool canUse = useCross ? canStartHeavy : canStartLight;
                if (canUse)
                {
                    _nextIsCross = !_nextIsCross;
                    return useCross ? TrainingAction.Cross : TrainingAction.Jab;
                }
            }

            return distanceToOpponent > ApproachDistance
                ? TrainingAction.MoveForward
                : TrainingAction.None;
        }
    }
}
