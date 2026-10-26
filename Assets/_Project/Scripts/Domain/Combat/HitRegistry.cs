using System.Collections.Generic;
using OctagonFighter.Core.Ids;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Prevents one hit window from damaging the same target more than once.
    /// </summary>
    public sealed class HitRegistry
    {
        private readonly Dictionary<HitId, FighterId> _claimedTargets =
            new Dictionary<HitId, FighterId>();

        /// <summary>
        /// Attempts to claim one target for one hit window.
        /// </summary>
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
        /// Clears every claim.
        /// </summary>
        public void Clear()
        {
            _claimedTargets.Clear();
        }
    }
}
