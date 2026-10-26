using OctagonFighter.Domain.Combat;

namespace OctagonFighter.Application.Combat
{
    /// <summary>
    /// Resolves a swept hit query against the combat world.
    /// </summary>
    public interface IHitDetector
    {
        /// <summary>
        /// Attempts to find the nearest valid target for a hit query.
        /// </summary>
        bool TryFindTarget(in HitQuery query, out HitCandidate candidate);
    }
}
