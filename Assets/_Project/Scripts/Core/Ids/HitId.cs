using System;

namespace OctagonFighter.Core.Ids
{
    /// <summary>
    /// Identifies one active hit window produced by one attacker.
    /// </summary>
    public readonly struct HitId : IEquatable<HitId>
    {
        /// <summary>
        /// Gets the zero hit identifier used before any hit is produced.
        /// </summary>
        public static HitId Zero => new HitId(0);

        /// <summary>
        /// Gets the numeric hit value.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitId"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
        public HitId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Hit ID must not be negative.");
            }

            Value = value;
        }

        /// <summary>
        /// Gets a value indicating whether this identifier has no value.
        /// </summary>
        public bool IsEmpty => Value == 0;

        /// <inheritdoc />
        public bool Equals(HitId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is HitId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Compares two hit identifiers for equality.
        /// </summary>
        public static bool operator ==(HitId left, HitId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two hit identifiers for inequality.
        /// </summary>
        public static bool operator !=(HitId left, HitId right)
        {
            return !left.Equals(right);
        }
    }
}
