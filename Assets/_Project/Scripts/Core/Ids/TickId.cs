using System;

namespace OctagonFighter.Core.Time
{
    /// <summary>
    /// Monotonically increasing identifier for one fixed simulation tick.
    /// </summary>
    public readonly struct TickId : IEquatable<TickId>, IComparable<TickId>
    {
        /// <summary>
        /// Gets the zero tick used before simulation starts.
        /// </summary>
        public static TickId Zero => new TickId(0L);

        /// <summary>
        /// Gets the numeric tick value.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickId"/> struct.
        /// </summary>
        /// <param name="value">Non-negative tick value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
        public TickId(long value)
        {
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Tick ID must not be negative.");
            }

            Value = value;
        }

        /// <summary>
        /// Returns the next tick identifier.
        /// </summary>
        public TickId Next()
        {
            return new TickId(Value + 1L);
        }

        /// <inheritdoc />
        public bool Equals(TickId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is TickId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc />
        public int CompareTo(TickId other)
        {
            return Value.CompareTo(other.Value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Compares two tick identifiers for equality.
        /// </summary>
        public static bool operator ==(TickId left, TickId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two tick identifiers for inequality.
        /// </summary>
        public static bool operator !=(TickId left, TickId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether one tick occurs before another.
        /// </summary>
        public static bool operator <(TickId left, TickId right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one tick occurs after another.
        /// </summary>
        public static bool operator >(TickId left, TickId right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one tick occurs before or at the same time as another.
        /// </summary>
        public static bool operator <=(TickId left, TickId right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one tick occurs after or at the same time as another.
        /// </summary>
        public static bool operator >=(TickId left, TickId right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
