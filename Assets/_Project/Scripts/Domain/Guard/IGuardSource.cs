using System;

namespace OctagonFighter.Domain.Guard
{
    /// <summary>
    /// Provides read-only guard state and change notifications.
    /// </summary>
    public interface IGuardSource
    {
        /// <summary>
        /// Gets the current guard value.
        /// </summary>
        float CurrentGuard { get; }

        /// <summary>
        /// Gets the maximum guard value.
        /// </summary>
        float MaxGuard { get; }

        /// <summary>
        /// Gets a value indicating whether the guard is currently broken.
        /// </summary>
        bool IsBroken { get; }

        /// <summary>
        /// Occurs when the current guard changes.
        /// </summary>
        event Action<GuardChangedEvent> Changed;

        /// <summary>
        /// Occurs when the guard is depleted by a blocked strike.
        /// </summary>
        event Action<GuardBrokenEvent> Broken;
    }
}
