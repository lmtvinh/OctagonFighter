using System;

namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Provides read-only stamina state and change notifications.
    /// </summary>
    public interface IStaminaSource
    {
        /// <summary>
        /// Gets the current stamina value.
        /// </summary>
        float CurrentStamina { get; }

        /// <summary>
        /// Gets the maximum stamina value.
        /// </summary>
        float MaxStamina { get; }

        /// <summary>
        /// Gets the current fatigue tier.
        /// </summary>
        StaminaTier Tier { get; }

        /// <summary>
        /// Determines whether a stamina cost can be paid.
        /// </summary>
        bool CanAfford(in StaminaCost cost);

        /// <summary>
        /// Attempts to spend stamina for a committed action.
        /// </summary>
        bool TrySpend(in StaminaCost cost);

        /// <summary>
        /// Occurs when current stamina changes.
        /// </summary>
        event Action<StaminaChangedEvent> Changed;

        /// <summary>
        /// Occurs when the fatigue tier changes.
        /// </summary>
        event Action<StaminaTierChangedEvent> TierChanged;

        /// <summary>
        /// Occurs when stamina reaches zero.
        /// </summary>
        event Action<StaminaDepletedEvent> Depleted;
    }
}
