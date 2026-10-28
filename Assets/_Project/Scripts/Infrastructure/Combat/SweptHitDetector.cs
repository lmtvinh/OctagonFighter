using OctagonFighter.Application.Combat;
using OctagonFighter.Core.Math;
using OctagonFighter.Domain.Combat;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Combat
{
    /// <summary>
    /// Resolves hit queries with a physics sphere sweep against fighter identities.
    /// </summary>
    public sealed class SweptHitDetector : IHitDetector
    {
        private const int MaximumHits = 16;

        private readonly RaycastHit[] _hits = new RaycastHit[MaximumHits];

        /// <inheritdoc />
        public bool TryFindTarget(in HitQuery query, out HitCandidate candidate)
        {
            var origin = new Vector3(query.Origin.X, query.Origin.Y, query.Origin.Z);
            var direction = new Vector3(query.Direction.X, query.Direction.Y, query.Direction.Z);

            int hitCount = UnityEngine.Physics.SphereCastNonAlloc(
                origin,
                query.Radius,
                direction,
                _hits,
                query.Distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            candidate = default;
            bool found = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _hits[index];
                if (hit.collider == null || hit.distance >= nearestDistance)
                {
                    continue;
                }

                FighterIdentity identity = hit.collider.GetComponentInParent<FighterIdentity>();
                if (identity == null ||
                    !identity.IsInitialized ||
                    identity.Id == query.AttackerId)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                candidate = new HitCandidate(
                    identity.Id,
                    new Float3(hit.point.x, hit.point.y, hit.point.z),
                    hit.distance);
                found = true;
            }

            return found;
        }
    }
}
