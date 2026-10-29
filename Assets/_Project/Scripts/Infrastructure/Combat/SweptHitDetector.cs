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

            for (int index = 0; index < overlapCount; index++)
            {
                Collider collider = _overlaps[index];
                if (collider == null || collider is CharacterController)
                {
                    // The CharacterController is a movement shell whose effective query shape is
                    // inset from the authored body; combat uses the dedicated trigger hitbox.
                    continue;
                }

                FighterIdentity identity = collider.GetComponentInParent<FighterIdentity>();
                if (identity == null ||
                    !identity.IsInitialized ||
                    identity.Id == query.AttackerId)
                {
                    continue;
                }

                candidate = new HitCandidate(identity.Id, query.Origin, 0f);
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
