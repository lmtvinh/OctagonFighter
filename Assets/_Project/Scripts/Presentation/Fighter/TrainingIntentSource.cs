using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;

namespace OctagonFighter.Presentation.Fighter
{
    /// <summary>
    /// Intent source for the training opponent. Converts a pure
    /// <see cref="TrainingIntentPolicy"/> decision into a <see cref="FighterIntent"/> using the
    /// fighter's actual position, state, and cooldowns.
    /// </summary>
    /// <remarks>
    /// This is a presentation-layer adapter: the policy is pure and deterministic, while this
    /// component supplies the runtime reads (state, cooldown, position) and the movement direction
    /// toward or away from the opponent. It is Inspector-configured and separate from any future
    /// autonomous AI.
    ///
    /// Movement is combat-relative: <see cref="FighterIntent.Move"/> Y=+1 moves toward the opponent
    /// and Y=-1 moves away. The motor rotates the fighter toward the nearest opponent while the
    /// state grants facing authority, so the training fighter stays aimed at the player.
    /// </remarks>
    [RequireComponent(typeof(FighterController))]
    public sealed class TrainingIntentSource : MonoBehaviour, IFighterIntentSource
    {
        [Header("Policy")]
        [SerializeField] private TrainingIntentPolicy _policy = new TrainingIntentPolicy();

        [Header("References")]
        [Tooltip("The fighter controller this source drives. Assigned to this fighter by default.")]
        [SerializeField] private FighterController _fighter;
        [Tooltip("The opponent transform, used to measure distance and movement direction.")]
        [SerializeField] private Transform _opponent;

        private void Reset()
        {
            _fighter = _fighter ?? GetComponent<FighterController>();
        }

        private void OnValidate()
        {
            _fighter = _fighter ?? GetComponent<FighterController>();
        }

        /// <inheritdoc />
        public bool TryGetIntent(TickId tick, out FighterIntent intent)
        {
            intent = FighterIntent.None;

            if (_policy == null || _fighter == null || !_fighter.IsInitialized || _opponent == null)
            {
                return false;
            }

            var snapshot = _fighter.Snapshot;
            float distance = PlanarDistance(transform.position, _opponent.position);

            TrainingAction action = _policy.Decide(
                distance,
                snapshot.State,
                _fighter.CurrentMovementAuthority,
                _fighter.CanStartLight,
                _fighter.CanStartHeavy,
                _fighter.IsBlocking);

            return ToIntent(action, tick, out intent);
        }

        private static bool ToIntent(TrainingAction action, TickId tick, out FighterIntent intent)
        {
            intent = FighterIntent.None;

            switch (action)
            {
                case TrainingAction.None:
                    intent = FighterIntent.None;
                    return true;

                case TrainingAction.Block:
                    intent = new FighterIntent(
                        Float2.Zero,
                        FighterCommand.Block,
                        isCommandPressed: true,
                        isCommandHeld: true,
                        MovementGait.Walk,
                        tick);
                    return true;

                case TrainingAction.MoveForward:
                    // Combat-relative: Y=+1 moves toward the opponent.
                    intent = new FighterIntent(
                        new Float2(0f, 1f),
                        FighterCommand.None,
                        false,
                        false,
                        MovementGait.Walk,
                        tick);
                    return true;

                case TrainingAction.MoveBackward:
                    // Combat-relative: Y=-1 moves away from the opponent.
                    intent = new FighterIntent(
                        new Float2(0f, -1f),
                        FighterCommand.None,
                        false,
                        false,
                        MovementGait.Walk,
                        tick);
                    return true;

                case TrainingAction.Jab:
                    intent = new FighterIntent(
                        Float2.Zero,
                        FighterCommand.LightPunch,
                        true,
                        false,
                        MovementGait.Walk,
                        tick);
                    return true;

                case TrainingAction.Cross:
                    intent = new FighterIntent(
                        Float2.Zero,
                        FighterCommand.HeavyPunch,
                        true,
                        false,
                        MovementGait.Walk,
                        tick);
                    return true;

                default:
                    return false;
            }
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }
    }
}
