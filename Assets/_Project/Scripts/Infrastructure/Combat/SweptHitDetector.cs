using OctagonFighter.Application.Combat;
using OctagonFighter.Core.Math;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Health;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Combat
{
    /// <summary>
    /// Resolves hit queries with a physics sphere sweep against fighter identities.
    /// </summary>
    public sealed class SweptHitDetector : IHitDetector
    {
        private const int MaximumHits = 16;

        /// <summary>
        /// Contact height at or above which the fallback region is the head.
        /// </summary>
        private const float HeadRegionHeight = 1.3f;

        /// <summary>
        /// Contact height at or above which the fallback region is the body (below it, the leg).
        /// </summary>
        private const float BodyRegionHeight = 0.85f;

        private readonly RaycastHit[] _hits = new RaycastHit[MaximumHits];
        private readonly Collider[] _overlaps = new Collider[MaximumHits];

        /// <inheritdoc />
        public bool TryFindTarget(in HitQuery query, out HitCandidate candidate)
        {
            var origin = new Vector3(query.Origin.X, query.Origin.Y, query.Origin.Z);
            var direction = new Vector3(query.Direction.X, query.Direction.Y, query.Direction.Z);

            // SphereCast deliberately ignores colliders that already overlap the sphere at the
            // origin. That is exactly the state a fist is in once it has reached the target, so a
            // deep contact would otherwise be dropped for every tick it rests inside the capsule.
            // Resolve the overlap first; it is always nearer than anything the sweep can find.
            // The fighter body volume is authored as a trigger hitbox so combat queries read the
            // intended 0.30 m capsule instead of the CharacterController's reduced collision shell.
            int overlapCount = UnityEngine.Physics.OverlapSphereNonAlloc(
                origin,
                query.Radius,
                _overlaps,
                ~0,
                QueryTriggerInteraction.Collide);

            HitCandidate? volumeOverlap = null;
            HitCandidate? fallbackOverlap = null;

            for (int index = 0; index < overlapCount; index++)
            {
                Collider collider = _overlaps[index];
                if (collider == null || collider is CharacterController)
                {
                    continue;
                }

                FighterIdentity identity = collider.GetComponentInParent<FighterIdentity>();
                if (identity == null ||
                    !identity.IsInitialized ||
                    identity.Id == query.AttackerId)
                {
                    continue;
                }

                var overlapCandidate = new HitCandidate(
                    identity.Id,
                    query.Origin,
                    0f,
                    ResolveRegion(collider, query.Origin));

                if (collider.GetComponent<HitRegionVolume>() != null)
                {
                    // A discrete body volume is authoritative over the generic capsule.
                    volumeOverlap = overlapCandidate;
                    break;
                }

                fallbackOverlap ??= overlapCandidate;
            }

            if (volumeOverlap.HasValue)
            {
                candidate = volumeOverlap.Value;
                return true;
            }

            if (fallbackOverlap.HasValue)
            {
                candidate = fallbackOverlap.Value;
                return true;
            }

            int hitCount = UnityEngine.Physics.SphereCastNonAlloc(
                origin,
                query.Radius,
                direction,
                _hits,
                query.Distance,
                ~0,
                QueryTriggerInteraction.Collide);

            float nearestDistance = float.PositiveInfinity;
            candidate = default;
            bool found = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _hits[index];
                if (hit.collider == null ||
                    hit.collider is CharacterController ||
                    hit.distance >= nearestDistance)
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
                var contact = new Float3(hit.point.x, hit.point.y, hit.point.z);
                candidate = new HitCandidate(
                    identity.Id,
                    contact,
                    hit.distance,
                    ResolveRegion(hit.collider, contact));
                found = true;
            }

            return found;
        }

        /// <summary>
        /// Resolves the body region for a collided collider, preferring an authored
        /// <see cref="HitRegionVolume"/> and falling back to contact-height mapping otherwise.
        /// </summary>
        private static HitRegion ResolveRegion(Collider collider, Float3 contactPoint)
        {
            var volume = collider.GetComponent<HitRegionVolume>();
            if (volume != null)
            {
                return volume.Region;
            }

            float height = contactPoint.Y;
            if (height >= HeadRegionHeight)
            {
                return HitRegion.Head;
            }

            if (height >= BodyRegionHeight)
            {
                return HitRegion.Body;
            }

            return HitRegion.Leg;
        }
    }
}
