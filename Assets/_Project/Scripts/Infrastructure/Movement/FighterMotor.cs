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
        [SerializeField] private float _gravity = -20f;
        [SerializeField, Range(0f, 1f)] private float _limitedAuthorityScale = 0.45f;

        [Header("Facing")]
        [SerializeField] private bool _faceNearestOpponent = true;
        [SerializeField, Min(0f)] private float _facingSearchInterval = 0.5f;

        private CharacterController _characterController;
        private MovementProfile _profile;
        private float _verticalVelocity;
        private bool _isInitialized;
        private Transform _facingTarget;
        private float _facingSearchTimer;

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
        public void Tick(
            Float2 move,
            MovementGait gait,
            MovementAuthority authority,
            float deltaSeconds)
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
                float gaitScale = authority == MovementAuthority.Full && gait == MovementGait.Run
                    ? 1f
                    : _profile.WalkSpeedMultiplier;
                float maximumSpeed = Mathf.Max(_profile.ForwardSpeed, _profile.LateralSpeed) * gaitScale;

                var desired = new Vector3(
                    move.X * _profile.LateralSpeed,
                    0f,
                    move.Y * forwardSpeed) * gaitScale;

                PlanarVelocity = Vector3.ClampMagnitude(desired, maximumSpeed) * authorityScale;
            }

            UpdateFacing(deltaSeconds);

            UpdateVerticalVelocity(deltaSeconds);

            Vector3 displacement = (PlanarVelocity + (Vector3.up * _verticalVelocity)) * deltaSeconds;
            _characterController.Move(displacement);
        }

        /// <summary>
        /// Keeps the fighter turned toward the nearest opponent, falling back to the movement
        /// direction while no opponent is available.
        /// </summary>
        private void UpdateFacing(float deltaSeconds)
        {
            if (!_faceNearestOpponent)
            {
                RotateTowards(PlanarVelocity, deltaSeconds);
                return;
            }

            if (_facingTarget == null || !_facingTarget.gameObject.activeInHierarchy)
            {
                _facingSearchTimer -= deltaSeconds;
                if (_facingSearchTimer > 0f)
                {
                    RotateTowards(PlanarVelocity, deltaSeconds);
                    return;
                }

                _facingSearchTimer = _facingSearchInterval;
                _facingTarget = FindNearestOpponent();
            }

            if (_facingTarget == null)
            {
                RotateTowards(PlanarVelocity, deltaSeconds);
                return;
            }

            Vector3 direction = _facingTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            RotateTowardsDirection(direction.normalized, deltaSeconds);
        }

        private Transform FindNearestOpponent()
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (FighterMotor other in FindObjectsByType<FighterMotor>(FindObjectsSortMode.None))
            {
                if (other == null || other == this || !other.isActiveAndEnabled)
                {
                    continue;
                }

                float distance = (other.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = other.transform;
                }
            }

            return nearest;
        }

        private void RotateTowards(Vector3 velocity, float deltaSeconds)
        {
            Vector3 direction = new Vector3(velocity.x, 0f, velocity.z);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            RotateTowardsDirection(direction.normalized, deltaSeconds);
        }

        private void RotateTowardsDirection(Vector3 direction, float deltaSeconds)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _profile.RotationDegreesPerSecond * deltaSeconds);
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
