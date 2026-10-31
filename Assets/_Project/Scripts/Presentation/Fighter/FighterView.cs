using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;

namespace OctagonFighter.Presentation.Fighter
{
    /// <summary>
    /// Presentation adapter that translates the motor's applied planar velocity into animator
    /// parameters and playback speed.
    /// </summary>
    public sealed class FighterView : MonoBehaviour
    {
        private const float MinimumSpeed = 0.0001f;

        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int MoveXParameter = Animator.StringToHash("MoveX");
        private static readonly int MoveYParameter = Animator.StringToHash("MoveY");
        private static readonly int IsBlockingParameter = Animator.StringToHash("IsBlocking");
        private static readonly int IsDefeatedParameter = Animator.StringToHash("IsDefeated");
        private static readonly int JabParameter = Animator.StringToHash("Jab");
        private static readonly int CrossParameter = Animator.StringToHash("Cross");
        private static readonly int LocomotionState = Animator.StringToHash("Locomotion");
        private static readonly int JabState = Animator.StringToHash("Jab");
        private static readonly int CrossState = Animator.StringToHash("Cross");
        private static readonly int HitReactionHeadState = Animator.StringToHash("HitReactionHead");
        private static readonly int HitReactionBodyState = Animator.StringToHash("HitReactionBody");

        private const float AttackFadeSeconds = 0.05f;
        private const float ReturnFadeSeconds = 0.1f;

        [SerializeField] private Animator _animator;
        [SerializeField, Min(0f)] private float _speedDamping = 0.1f;

        private MovementProfile _profile;
        private bool _hasProfile;
        private bool _hasSpeed;
        private bool _hasMoveX;
        private bool _hasMoveY;
        private bool _hasBlocking;
        private bool _hasDefeated;
        private bool _hasJab;
        private bool _hasCross;
        private bool _hasLocomotionState;
        private bool _hasJabState;
        private bool _hasCrossState;
        private bool _hasHitReactionHeadState;
        private bool _hasHitReactionBodyState;
        private int _lastState = -1;

        /// <summary>
        /// Resolves the animator reference, caches the supported parameters, and stores the
        /// movement profile used to normalize velocity into blend-tree coordinates.
        /// </summary>
        public void Initialize(MovementProfile profile)
        {
            _profile = profile;
            _hasProfile = profile.WalkSpeedMultiplier > 0f;

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            _hasLocomotionState = _animator.HasState(0, LocomotionState);
            _hasJabState = _animator.HasState(0, JabState);
            _hasCrossState = _animator.HasState(0, CrossState);
            _hasHitReactionHeadState = _animator.HasState(0, HitReactionHeadState);
            _hasHitReactionBodyState = _animator.HasState(0, HitReactionBodyState);

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == SpeedParameter)
                {
                    _hasSpeed = true;
                }
                else if (parameter.nameHash == MoveXParameter)
                {
                    _hasMoveX = true;
                }
                else if (parameter.nameHash == MoveYParameter)
                {
                    _hasMoveY = true;
                }
                else if (parameter.nameHash == IsBlockingParameter)
                {
                    _hasBlocking = true;
                }
                else if (parameter.nameHash == IsDefeatedParameter)
                {
                    _hasDefeated = true;
                }
                else if (parameter.nameHash == JabParameter)
                {
                    _hasJab = true;
                }
                else if (parameter.nameHash == CrossParameter)
                {
                    _hasCross = true;
                }
            }
        }

        /// <summary>
        /// Applies a simulation snapshot and the motor's applied planar velocity to the view.
        /// </summary>
        /// <param name="snapshot">Latest immutable fighter snapshot.</param>
        /// <param name="planarVelocity">World-space planar velocity actually applied by the motor.</param>
        /// <param name="deltaSeconds">Simulation delta used for parameter damping.</param>
        public void Apply(
            in FighterSnapshot snapshot,
            Vector3 planarVelocity,
            float deltaSeconds)
        {
            if (_animator == null)
            {
                return;
            }

            Vector2 move = Vector2.zero;
            float normalizedSpeed = 0f;
            float playback = 1f;

            if (_hasProfile)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(
                    new Vector3(planarVelocity.x, 0f, planarVelocity.z));
                var planar = new Vector2(localVelocity.x, localVelocity.z);
                float speed = planar.magnitude;

                if (speed > MinimumSpeed)
                {
                    Vector2 direction = planar / speed;

                    // The blend tree is driven by the walk reference speed for the current
                    // direction. Below that reference the blend magnitude falls toward idle;
                    // at and above it the direction stays at full weight while playback carries
                    // the extra speed.
                    float walkReferenceSpeed = _profile.GetPlanarSpeed(
                        direction.x,
                        direction.y,
                        _profile.WalkSpeedMultiplier);
                    float runReferenceSpeed = _profile.GetPlanarSpeed(
                        direction.x,
                        direction.y);

                    if (walkReferenceSpeed > MinimumSpeed)
                    {
                        float blendMagnitude = Mathf.Clamp01(speed / walkReferenceSpeed);
                        move = direction * blendMagnitude;
                        playback = Mathf.Clamp(
                            speed / walkReferenceSpeed,
                            1f,
                            1f / _profile.WalkSpeedMultiplier);
                    }

                    if (runReferenceSpeed > MinimumSpeed)
                    {
                        normalizedSpeed = Mathf.Clamp01(speed / runReferenceSpeed);
                    }
                }
            }

            // Attacks and other committed states must play their own clip at authored speed.
            // Locomotion playback is only meaningful while the blend tree is visible, including
            // the idle state's deceleration tail.
            _animator.speed = UsesLocomotionPlayback(snapshot.State) ? playback : 1f;

            if (_hasSpeed)
            {
                _animator.SetFloat(SpeedParameter, normalizedSpeed, _speedDamping, deltaSeconds);
            }

            if (_hasMoveX)
            {
                _animator.SetFloat(MoveXParameter, move.x, _speedDamping, deltaSeconds);
            }

            if (_hasMoveY)
            {
                _animator.SetFloat(MoveYParameter, move.y, _speedDamping, deltaSeconds);
            }

            if (_hasBlocking)
            {
                _animator.SetBool(IsBlockingParameter, snapshot.State == FighterStateId.Blocking);
            }

            if (_hasDefeated)
            {
                _animator.SetBool(IsDefeatedParameter, snapshot.State == FighterStateId.Defeated);
            }

            if (_lastState != (int)snapshot.State)
            {
                if (snapshot.State == FighterStateId.LightPunch)
                {
                    EnterAttackState(JabParameter, JabState, _hasJab, _hasJabState);
                }
                else if (snapshot.State == FighterStateId.HeavyPunch)
                {
                    EnterAttackState(CrossParameter, CrossState, _hasCross, _hasCrossState);
                }
                else if (IsCommittedVisualState((FighterStateId)_lastState) &&
                         snapshot.State != FighterStateId.Defeated &&
                         _hasLocomotionState)
                {
                    // The view is the only authority that returns a finished strike or reaction to
                    // locomotion. Attack and reaction states deliberately have no exit-time
                    // transition in the controller, so the return happens exactly on the tick the
                    // simulation state ends. Fixed-time keeps the fade at a constant wall duration
                    // instead of scaling it by the finished state's clip length.
                    _animator.CrossFadeInFixedTime(LocomotionState, ReturnFadeSeconds, 0);
                }

                _lastState = (int)snapshot.State;
            }
        }

        /// <summary>
        /// Presents the reaction for a confirmed hit.
        /// </summary>
        /// <remarks>
        /// Called from the runtime's <c>HitReactionStarted</c> event, so the clip starts on the same
        /// event that applied damage. The simulation state owns the reaction duration; the animator
        /// only presents it and is cross-faded back to locomotion when the state ends.
        /// </remarks>
        /// <param name="region">Body region that received the hit.</param>
        public void PlayHitReaction(HitRegion region)
        {
            if (_animator == null)
            {
                return;
            }

            bool hasState = region == HitRegion.Body
                ? _hasHitReactionBodyState
                : _hasHitReactionHeadState;

            if (!hasState)
            {
                return;
            }

            int stateHash = region == HitRegion.Body ? HitReactionBodyState : HitReactionHeadState;

            // Fixed-time fade: CrossFade's duration is normalized to the source state's length,
            // and the locomotion blend tree is tens of seconds long, so a normalized fade would
            // outlast the whole reaction and the state would never be reached.
            _animator.CrossFadeInFixedTime(stateHash, AttackFadeSeconds, 0);
        }

        /// <summary>
        /// Starts an attack state. The trigger is the authored entry; the cross fade is a fallback
        /// for controllers that have the state but not the parameter.
        /// </summary>
        private void EnterAttackState(
            int triggerParameter,
            int stateHash,
            bool hasTrigger,
            bool hasState)
        {
            if (hasTrigger)
            {
                _animator.SetTrigger(triggerParameter);
            }
            else if (hasState)
            {
                _animator.CrossFadeInFixedTime(stateHash, AttackFadeSeconds, 0);
            }
        }

        private static bool UsesLocomotionPlayback(FighterStateId state)
        {
            return state == FighterStateId.Idle ||
                   state == FighterStateId.Walk ||
                   state == FighterStateId.Run ||
                   state == FighterStateId.Blocking ||
                   state == FighterStateId.Dodging;
        }

        private static bool IsCommittedVisualState(FighterStateId state)
        {
            return state == FighterStateId.LightPunch ||
                   state == FighterStateId.HeavyPunch ||
                   state == FighterStateId.HitReaction;
        }
    }
}
