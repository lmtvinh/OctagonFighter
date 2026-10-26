using System;

namespace OctagonFighter.Core.Math
{
    /// <summary>
    /// Engine-independent three-component vector used at domain boundaries.
    /// </summary>
    public readonly struct Float3 : IEquatable<Float3>
    {
        /// <summary>
        /// Gets the zero vector.
        /// </summary>
        public static Float3 Zero => new Float3(0f, 0f, 0f);

        /// <summary>
        /// Gets the X component.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the Y component.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets the Z component.
        /// </summary>
        public float Z { get; }

        /// <summary>
        /// Gets the squared magnitude of the vector.
        /// </summary>
        public float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);

        /// <summary>
        /// Gets the magnitude of the vector.
        /// </summary>
        public float Magnitude => MathF.Sqrt(SqrMagnitude);

        /// <summary>
        /// Gets a value indicating whether every component is finite.
        /// </summary>
        public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);

        /// <summary>
        /// Initializes a new instance of the <see cref="Float3"/> struct.
        /// </summary>
        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Returns a unit-length copy of the vector.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the vector is too short to normalize.</exception>
        public Float3 Normalized()
        {
            float magnitude = Magnitude;
            if (magnitude <= 0.000001f)
            {
                throw new InvalidOperationException("Cannot normalize a zero-length vector.");
            }

            return new Float3(X / magnitude, Y / magnitude, Z / magnitude);
        }

        /// <inheritdoc />
        public bool Equals(Float3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Float3 other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({X:0.###}, {Y:0.###}, {Z:0.###})";
        }

        /// <summary>
        /// Compares two vectors for equality.
        /// </summary>
        public static bool operator ==(Float3 left, Float3 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two vectors for inequality.
        /// </summary>
        public static bool operator !=(Float3 left, Float3 right)
        {
            return !left.Equals(right);
        }
    }
}
