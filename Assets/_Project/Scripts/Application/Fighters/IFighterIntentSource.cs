using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Application.Fighters
{
    /// <summary>
    /// Produces fighter intents for the simulation.
    /// </summary>
    public interface IFighterIntentSource
    {
        /// <summary>
        /// Attempts to read the intent for a specific tick.
        /// </summary>
        bool TryGetIntent(TickId tick, out FighterIntent intent);
    }
}
