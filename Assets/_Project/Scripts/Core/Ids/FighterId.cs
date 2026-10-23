using System;

namespace OctagonFighter.Core.Ids
{
    /// <summary>
    /// Stable identifier for a fighter across data assets, runtime models, and events.
    /// </summary>
    public readonly struct FighterId : IEquatable<FighterId>
    {
        /// <summary>
        /// Gets the authored identifier value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets a value indicating whether this identifier has no value.
        /// </summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterId"/> struct.
        /// </summary>
        /// <param name="value">Non-empty identifier value.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
        public FighterId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Fighter ID must not be empty.", nameof(value));
            }

            Value = value.Trim();
        }

        /// <inheritdoc />
        public bool Equals(FighterId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FighterId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        /// <summary>
        /// Compares two fighter identifiers for equality.
        /// </summary>
        public static bool operator ==(FighterId left, FighterId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two fighter identifiers for inequality.
        /// </summary>
        public static bool operator !=(FighterId left, FighterId right)
        {
            return !left.Equals(right);
        }
    }
}
