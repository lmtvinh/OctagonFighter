using System;

namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Provides read-only health state and change notifications.
    /// </summary>
    public interface IHealthSource
    {
        /// <summary>
        /// Gets the current health value.
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// Gets the maximum health value.
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// Gets the accumulated head trauma.
        /// </summary>
        float HeadTrauma { get; }

        /// <summary>
        /// Gets the accumulated body trauma.
        /// </summary>
        float BodyTrauma { get; }

        /// <summary>
        /// Gets a value indicating whether the fighter still has health.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets a value indicating whether the fighter is knocked out.
        /// </summary>
        bool IsKnockedOut { get; }

        /// <summary>
        /// Occurs when health or regional trauma changes.
        /// </summary>
        event Action<HealthChangedEvent> Changed;

        /// <summary>
        /// Occurs when health reaches zero.
        /// </summary>
        event Action<HealthDepletedEvent> Depleted;

        /// <summary>
        /// Occurs once when the fighter is knocked out.
        /// </summary>
        event Action<FighterKnockedOutEvent> KnockedOut;
    }
}
