using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Input
{
    /// <summary>
    /// Intent source for a non-player-controlled fighter that is passive by default.
    /// </summary>
    /// <remarks>
    /// This is a presentation-layer placeholder for opponents until an AI intent source exists.
    /// <see cref="HoldBlock"/> is a manual test aid: it keeps a guard raised so the defensive
    /// flows can be exercised in the shipped scene. It never chooses, times, or releases a block
    /// on its own, so it is not AI defensive behavior.
    /// </remarks>
    public sealed class PassiveIntentSource : MonoBehaviour, IFighterIntentSource
    {
        [SerializeField]
        [Tooltip("Manual test aid: hold the guard continuously so blocked strikes can be observed.")]
        private bool _holdBlock;

        /// <summary>
        /// Gets or sets a value indicating whether the fighter should hold a guard.
        /// </summary>
        public bool HoldBlock
        {
            get => _holdBlock;
            set => _holdBlock = value;
        }

        /// <inheritdoc />
        public bool TryGetIntent(TickId tick, out FighterIntent intent)
        {
            if (_holdBlock)
            {
                intent = new FighterIntent(
                    Float2.Zero,
                    FighterCommand.Block,
                    true,
                    true,
                    MovementGait.Walk,
                    tick);
                return true;
            }

            intent = FighterIntent.None;
            return false;
        }
    }
}
