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

        [Header("Acceleration")]
        [Tooltip("Seconds to reach full speed from rest. Zero makes acceleration instant.")]
        [SerializeField, Min(0f)] private float _accelerationSeconds = 0.08f;
        [Tooltip("Seconds to coast to a stop after input or authority is released. Zero stops instantly.")]
        [SerializeField, Min(0f)] private float _decelerationSeconds = 0.1f;

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

            Vector3 targetVelocity = Vector3.zero;
            if (authority != MovementAuthority.None && move.SqrMagnitude > 0f)
            {
                float forwardSpeed = move.Y >= 0f ? _profile.ForwardSpeed : _profile.BackwardSpeed;
                float authorityScale = authority == MovementAuthority.Limited ? _limitedAuthorityScale : 1f;
                float gaitScale = authority == MovementAuthority.Full && gait == MovementGait.Run
                    ? 1f
                    : _profile.WalkSpeedMultiplier;
                float maximumSpeed = Mathf.Max(_profile.ForwardSpeed, _profile.LateralSpeed) * gaitScale;

                // Input is combat-relative: X is the fighter's right, Y is toward the opponent.
                // Rotating it into the fighter's frame keeps the lateral animation on its lateral
                // axis while the body turns to track the opponent, so the blend stays clean.
                var localDesired = new Vector3(
                    move.X * _profile.LateralSpeed,
                    0f,
                    move.Y * forwardSpeed) * gaitScale;
                Vector3 desired = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * localDesired;

                targetVelocity = Vector3.ClampMagnitude(desired, maximumSpeed) * authorityScale;
            }

            // Ramps keep starts, stops, and direction changes from snapping while still letting
            // combat states bleed off residual speed in a predictable, short window.
            ApplyVelocityRamp(targetVelocity, deltaSeconds);

            UpdateFacing(deltaSeconds);

            UpdateVerticalVelocity(deltaSeconds);

            Vector3 displacement = (PlanarVelocity + (Vector3.up * _verticalVelocity)) * deltaSeconds;
            _characterController.Move(displacement);
        }

        /// <summary>
        /// Ramps the applied planar velocity toward the target so starts, stops, and direction
        /// changes take a fixed time instead of snapping. Time-based ramps stay frame-rate
        /// independent and match the animator's parameter damping across directions.
        /// </summary>
        private void ApplyVelocityRamp(Vector3 targetVelocity, float deltaSeconds)
        {
            float targetSpeed = targetVelocity.magnitude;
            bool accelerating = targetSpeed > 0.0001f;
            float seconds = accelerating ? _accelerationSeconds : _decelerationSeconds;
            if (seconds <= 0f)
            {
                PlanarVelocity = targetVelocity;
                return;
            }

            float rampSpeed = accelerating ? targetSpeed : PlanarVelocity.magnitude;
            float maximumDelta = (rampSpeed / seconds) * deltaSeconds;
            PlanarVelocity = Vector3.MoveTowards(PlanarVelocity, targetVelocity, maximumDelta);
        }

        /// <summary>
        /// Keeps the fighter turned toward the nearest opponent. Because movement is combat-relative,
        /// the body is never rotated toward its own velocity: doing so would curve the movement into
        /// a spiral. Without an opponent the current facing is kept.
        /// </summary>
        private void UpdateFacing(float deltaSeconds)
        {
            if (!_faceNearestOpponent)
            {
                return;
            }

            if (_facingTarget == null || !_facingTarget.gameObject.activeInHierarchy)
            {
                _facingSearchTimer -= deltaSeconds;
                if (_facingSearchTimer > 0f)
                {
                    return;
                }

                _facingSearchTimer = _facingSearchInterval;
                _facingTarget = FindNearestOpponent();
            }

            if (_facingTarget == null)
            {
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
