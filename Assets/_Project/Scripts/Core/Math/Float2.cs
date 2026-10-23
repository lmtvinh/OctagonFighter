using System;

namespace OctagonFighter.Core.Math
{
    /// <summary>
    /// Engine-independent two-component floating-point vector.
    /// </summary>
    public readonly struct Float2 : IEquatable<Float2>
    {
        /// <summary>
        /// Gets the zero vector.
        /// </summary>
        public static Float2 Zero => new Float2(0f, 0f);

        /// <summary>
        /// Gets the horizontal component.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the vertical component.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets the squared magnitude of the vector.
        /// </summary>
        public float SqrMagnitude => (X * X) + (Y * Y);

        /// <summary>
        /// Gets the magnitude of the vector.
        /// </summary>
        public float Magnitude => MathF.Sqrt(SqrMagnitude);

        /// <summary>
        /// Gets a value indicating whether every component is finite.
        /// </summary>
        public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);

        /// <summary>
        /// Initializes a new instance of the <see cref="Float2"/> struct.
        /// </summary>
        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns a vector clamped to a maximum magnitude.
        /// </summary>
        public Float2 ClampMagnitude(float maxMagnitude)
        {
            if (maxMagnitude < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMagnitude));
            }

            float sqrMagnitude = SqrMagnitude;
            if (sqrMagnitude <= maxMagnitude * maxMagnitude)
            {
                return this;
            }

            float scale = maxMagnitude / MathF.Sqrt(sqrMagnitude);
            return new Float2(X * scale, Y * scale);
        }

        /// <inheritdoc />
        public bool Equals(Float2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Float2 other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        public static Float2 operator +(Float2 left, Float2 right)
        {
            return new Float2(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Subtracts one vector from another.
        /// </summary>
        public static Float2 operator -(Float2 left, Float2 right)
        {
            return new Float2(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Negates a vector.
        /// </summary>
        public static Float2 operator -(Float2 value)
        {
            return new Float2(-value.X, -value.Y);
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        public static Float2 operator *(Float2 value, float scalar)
        {
            return new Float2(value.X * scalar, value.Y * scalar);
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        public static Float2 operator *(float scalar, Float2 value)
        {
            return value * scalar;
        }

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        public static Float2 operator /(Float2 value, float scalar)
        {
            if (scalar == 0f)
            {
                throw new DivideByZeroException();
            }

            return new Float2(value.X / scalar, value.Y / scalar);
        }

        /// <summary>
        /// Compares two vectors for equality.
        /// </summary>
        public static bool operator ==(Float2 left, Float2 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two vectors for inequality.
        /// </summary>
        public static bool operator !=(Float2 left, Float2 right)
        {
            return !left.Equals(right);
        }
    }
}
