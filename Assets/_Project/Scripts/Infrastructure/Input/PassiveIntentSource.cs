using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Input
{
    /// <summary>
    /// Intent source for a non-player-controlled fighter that never requests movement or commands.
    /// </summary>
    /// <remarks>
    /// This is a presentation-layer placeholder for opponents until an AI intent source exists.
    /// </remarks>
    public sealed class PassiveIntentSource : MonoBehaviour, IFighterIntentSource
    {
        /// <inheritdoc />
        public bool TryGetIntent(TickId tick, out FighterIntent intent)
        {
            intent = FighterIntent.None;
            return false;
        }
    }
}
