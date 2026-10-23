using System;
using OctagonFighter.Core.Math;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Movement
{
    /// <summary>
    /// Unity movement adapter for one fighter. Applies state-authorized planar movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FighterMotor : MonoBehaviour
    {
        [Header("Adapter")]
        [SerializeField, Min(0f)] private float _rotationDegreesPerSecond = 720f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField, Range(0f, 1f)] private float _limitedAuthorityScale = 0.45f;

        private CharacterController _characterController;
        private MovementProfile _profile;
        private float _verticalVelocity;
        private bool _isInitialized;

        /// <summary>
        /// Gets the planar velocity applied during the most recent tick.
        /// </summary>
        public Vector3 PlanarVelocity { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the character is grounded.
        /// </summary>
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;

        /// <summary>
        /// Gets a value indicating whether the motor has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Applies the fighter's movement data to the motor.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the character controller is missing.</exception>
        public void Initialize(MovementProfile profile)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Fighter motor is already initialized.");
            }

            _profile = profile;
            if (!TryGetComponent(out _characterController))
            {
                throw new MissingComponentException(
                    $"{nameof(FighterMotor)} requires a {nameof(CharacterController)} component.");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Advances movement for one fixed tick.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the motor has not been initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the delta time is invalid.</exception>
        public void Tick(Float2 move, MovementAuthority authority, float deltaSeconds)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Fighter motor has not been initialized.");
            }

            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            PlanarVelocity = Vector3.zero;

            if (authority != MovementAuthority.None && move.SqrMagnitude > 0f)
            {
                float forwardSpeed = move.Y >= 0f ? _profile.ForwardSpeed : _profile.BackwardSpeed;
                float authorityScale = authority == MovementAuthority.Limited ? _limitedAuthorityScale : 1f;
                float maximumSpeed = Mathf.Max(_profile.ForwardSpeed, _profile.LateralSpeed);

                var desired = new Vector3(
                    move.X * _profile.LateralSpeed,
                    0f,
                    move.Y * forwardSpeed);

                PlanarVelocity = Vector3.ClampMagnitude(desired, maximumSpeed) * authorityScale;
                RotateTowards(PlanarVelocity, deltaSeconds);
            }

            UpdateVerticalVelocity(deltaSeconds);

            Vector3 displacement = (PlanarVelocity + (Vector3.up * _verticalVelocity)) * deltaSeconds;
            _characterController.Move(displacement);
        }

        private void RotateTowards(Vector3 velocity, float deltaSeconds)
        {
            Vector3 direction = new Vector3(velocity.x, 0f, velocity.z);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationDegreesPerSecond * deltaSeconds);
        }

        private void UpdateVerticalVelocity(float deltaSeconds)
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
                return;
            }

            _verticalVelocity += _gravity * deltaSeconds;
        }
    }
}
