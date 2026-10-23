using System;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Immutable physical description of a fighter.
    /// </summary>
    public readonly struct BodyProfile
    {
        /// <summary>
        /// Gets the fighter height in meters.
        /// </summary>
        public float HeightMeters { get; }

        /// <summary>
        /// Gets the fighter reach in meters.
        /// </summary>
        public float ReachMeters { get; }

        /// <summary>
        /// Gets the fighter weight class name.
        /// </summary>
        public string WeightClass { get; }

        /// <summary>
        /// Gets the fighter stance.
        /// </summary>
        public FighterStance Stance { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyProfile"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a measurement is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when the weight class is empty or the stance is undefined.</exception>
        public BodyProfile(float heightMeters, float reachMeters, string weightClass, FighterStance stance)
        {
            if (!float.IsFinite(heightMeters) || heightMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(heightMeters), "Height must be finite and positive.");
            }

            if (!float.IsFinite(reachMeters) || reachMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(reachMeters), "Reach must be finite and positive.");
            }

            if (string.IsNullOrWhiteSpace(weightClass))
            {
                throw new ArgumentException("Weight class must be specified.", nameof(weightClass));
            }

            if (!Enum.IsDefined(typeof(FighterStance), stance))
            {
                throw new ArgumentException("Stance is not defined.", nameof(stance));
            }

            HeightMeters = heightMeters;
            ReachMeters = reachMeters;
            WeightClass = weightClass.Trim();
            Stance = stance;
        }
    }
}
