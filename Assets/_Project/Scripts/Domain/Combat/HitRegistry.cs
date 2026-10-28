using System.Collections.Generic;
using OctagonFighter.Core.Ids;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Prevents one strike from damaging more than one target, and the same target more than once.
    /// </summary>
    /// <remarks>
    /// A strike is resolved from a single fist, so it may only ever claim a single target. The first
    /// target whose volume intersects the fist's swept path owns the hit window; later contact
    /// callbacks for the same <see cref="HitId"/> are rejected even if they name the same target
    /// again or a different one. The owner must call <see cref="Release"/> when the attack leaves
    /// its state, which keeps the table bounded by the number of attacks in flight instead of
    /// growing for the lifetime of the session.
    /// </remarks>
    public sealed class HitRegistry
    {
        private readonly Dictionary<HitId, FighterId> _claimedTargets =
            new Dictionary<HitId, FighterId>();

        /// <summary>
        /// Gets the number of hit windows that currently hold a claim.
        /// </summary>
        public int Count => _claimedTargets.Count;

        /// <summary>
        /// Determines whether a hit window has already claimed a target.
        /// </summary>
        public bool IsClaimed(HitId hitId)
        {
            return _claimedTargets.ContainsKey(hitId);
        }

        /// <summary>
        /// Attempts to claim one target for one hit window.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when this call claimed the window; <see langword="false"/> when the
        /// window already damaged a target.
        /// </returns>
        public bool TryClaim(HitId hitId, FighterId targetId)
        {
            if (_claimedTargets.ContainsKey(hitId))
            {
                return false;
            }

            _claimedTargets.Add(hitId, targetId);
            return true;
        }

        /// <summary>
        /// Releases a hit window so it may be reused.
        /// </summary>
        public void Release(HitId hitId)
        {
            _claimedTargets.Remove(hitId);
        }

        /// <summary>
        /// Clears every claim. Called when the owning fighter is destroyed.
        /// </summary>
        public void Clear()
        {
            _claimedTargets.Clear();
        }
    }
}
