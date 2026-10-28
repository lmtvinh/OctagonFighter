using System;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Describes a swept volume used to find hit targets.
    /// </summary>
    public readonly struct HitQuery
    {
        /// <summary>
        /// Gets the identifier of this hit window.
        /// </summary>
        public HitId HitId { get; }

        /// <summary>
        /// Gets the attacking fighter identifier.
        /// </summary>
        public FighterId AttackerId { get; }

        /// <summary>
        /// Gets the sweep origin.
        /// </summary>
        public Float3 Origin { get; }

        /// <summary>
        /// Gets the normalized sweep direction.
        /// </summary>
        public Float3 Direction { get; }

        /// <summary>
        /// Gets the sweep distance in meters.
        /// </summary>
        public float Distance { get; }

        /// <summary>
        /// Gets the sweep radius in meters.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// Gets the tick that produced the query.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitQuery"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric value is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when a required value is missing.</exception>
        public HitQuery(
            HitId hitId,
            FighterId attackerId,
            Float3 origin,
            Float3 direction,
            float distance,
            float radius,
            TickId tick)
        {
            if (hitId.IsEmpty)
            {
                throw new ArgumentException("Hit ID must be specified.", nameof(hitId));
            }

            if (attackerId.IsEmpty)
            {
                throw new ArgumentException("Attacker ID must be specified.", nameof(attackerId));
            }

            if (!origin.IsFinite)
            {
                throw new ArgumentOutOfRangeException(nameof(origin), "Hit origin must be finite.");
            }

            if (!direction.IsFinite || direction.SqrMagnitude <= 0.000001f)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Hit direction must be finite and non-zero.");
            }

            if (!float.IsFinite(distance) || distance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distance), "Hit distance must be finite and non-negative.");
            }

            if (!float.IsFinite(radius) || radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Hit radius must be finite and positive.");
            }

            HitId = hitId;
            AttackerId = attackerId;
            Origin = origin;
            Direction = direction.Normalized();
            Distance = distance;
            Radius = radius;
            Tick = tick;
        }

        /// <summary>
        /// Builds a query that sweeps the striking fist from its previous sample to its current one.
        /// </summary>
        /// <remarks>
        /// A fist that has not moved (or has no previous sample) still probes a short distance along
        /// the attacker's facing so a resting contact is not silently skipped. The sweep is clamped
        /// to <paramref name="minimumDistance"/> to keep zero-length motion a valid physics query.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the minimum distance is invalid.</exception>
        public static HitQuery FromHandSweep(
            HitId hitId,
            FighterId attackerId,
            Float3 previousHandPosition,
            Float3 currentHandPosition,
            Float3 facing,
            float radius,
            TickId tick,
            float minimumDistance = 0.02f)
        {
            if (!float.IsFinite(minimumDistance) || minimumDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumDistance),
                    "Minimum sweep distance must be finite and positive.");
            }

            float deltaX = currentHandPosition.X - previousHandPosition.X;
            float deltaY = currentHandPosition.Y - previousHandPosition.Y;
            float deltaZ = currentHandPosition.Z - previousHandPosition.Z;
            float distance = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));
            Float3 direction = distance > 0.0001f
                ? new Float3(deltaX / distance, deltaY / distance, deltaZ / distance)
                : facing;
            return new HitQuery(
                hitId,
                attackerId,
                previousHandPosition,
                direction,
                MathF.Max(distance, minimumDistance),
                radius,
                tick);
        }
    }
}
