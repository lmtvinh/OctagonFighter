using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Math;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Describes a fighter volume overlapped by a hit query.
    /// </summary>
    public readonly struct HitCandidate
    {
        /// <summary>
        /// Gets the overlapped fighter identifier.
        /// </summary>
        public FighterId TargetId { get; }

        /// <summary>
        /// Gets the world-space contact point.
        /// </summary>
        public Float3 Point { get; }

        /// <summary>
        /// Gets the distance from the sweep origin in meters.
        /// </summary>
        public float Distance { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitCandidate"/> struct.
        /// </summary>
        public HitCandidate(FighterId targetId, Float3 point, float distance)
        {
            TargetId = targetId;
            Point = point;
            Distance = distance;
        }
    }
}
