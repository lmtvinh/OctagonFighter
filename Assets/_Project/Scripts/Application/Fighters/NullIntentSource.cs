using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Application.Fighters
{
    /// <summary>
    /// Intent source that produces no input.
    /// </summary>
    public sealed class NullIntentSource : IFighterIntentSource
    {
        /// <summary>
        /// Gets the shared stateless instance.
        /// </summary>
        public static NullIntentSource Instance { get; } = new NullIntentSource();

        private NullIntentSource()
        {
        }

        /// <inheritdoc />
        public bool TryGetIntent(TickId tick, out FighterIntent intent)
        {
            intent = FighterIntent.None;
            return false;
        }
    }
}
