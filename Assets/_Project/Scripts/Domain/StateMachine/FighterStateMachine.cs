using System;
using System.Collections.Generic;
using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Registration-based state machine for one fighter.
    /// </summary>
    public sealed class FighterStateMachine
    {
        private readonly Dictionary<FighterStateId, IFighterState> _states =
            new Dictionary<FighterStateId, IFighterState>();
        private IFighterState _currentState;
        private float _timeInState;

        /// <summary>
        /// Gets a value indicating whether the machine has been initialized.
        /// </summary>
        public bool IsInitialized => _currentState != null;

        /// <summary>
        /// Gets the current state identifier.
        /// </summary>
        public FighterStateId CurrentStateId =>
            _currentState != null ? _currentState.Id : FighterStateId.Spawning;

        /// <summary>
        /// Gets the time spent in the current state.
        /// </summary>
        public float TimeInState => _timeInState;

        /// <summary>
        /// Gets the movement authority granted by the current state.
        /// </summary>
        public MovementAuthority CurrentMovementAuthority =>
            _currentState != null ? _currentState.MovementAuthority : MovementAuthority.None;

        /// <summary>
        /// Gets the facing authority granted by the current state.
        /// </summary>
        public FacingAuthority CurrentFacingAuthority =>
            _currentState != null ? _currentState.FacingAuthority : FacingAuthority.Free;

        /// <summary>
        /// Registers a state with the machine.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the state identifier is undefined or already registered.</exception>
        public void Register(IFighterState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!Enum.IsDefined(typeof(FighterStateId), state.Id))
            {
                throw new ArgumentException("State identifier is not defined.", nameof(state));
            }

            if (_states.ContainsKey(state.Id))
            {
                throw new ArgumentException($"State '{state.Id}' is already registered.", nameof(state));
            }

            _states.Add(state.Id, state);
        }

        /// <summary>
        /// Initializes the machine in an initial state.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the machine is already initialized.</exception>
        /// <exception cref="ArgumentException">Thrown when the initial state is not registered.</exception>
        public void Initialize(
            FighterStateId initialState,
            in FighterStateContext context,
            in FighterIntent intent)
        {
            if (_currentState != null)
            {
                throw new InvalidOperationException("State machine is already initialized.");
            }

            if (!_states.TryGetValue(initialState, out IFighterState state))
            {
                throw new ArgumentException($"State '{initialState}' is not registered.", nameof(initialState));
            }

            _currentState = state;
            _timeInState = 0f;
            FighterIntent filteredIntent = intent.Filter(state.AcceptedIntents);
            state.Enter(context, filteredIntent);
        }

        /// <summary>
        /// Advances the current state and applies a requested transition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the machine is not initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="deltaSeconds"/> is invalid.</exception>
        public StateTransition? Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            EnsureInitialized();

            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");
            }

            FighterIntent filteredIntent = intent.Filter(_currentState.AcceptedIntents);
            _timeInState += deltaSeconds;

            StateTickResult result = _currentState.Tick(context, filteredIntent, deltaSeconds);
            if (!result.HasTransition || result.TargetState == _currentState.Id)
            {
                return null;
            }

            return TryTransition(result.TargetState, context, filteredIntent, out StateTransition transition)
                ? transition
                : (StateTransition?)null;
        }

        /// <summary>
        /// Attempts to transition to a target state using the current state's rules.
        /// </summary>
        public bool TryTransition(
            FighterStateId targetState,
            in FighterStateContext context,
            in FighterIntent intent)
        {
            return TryTransition(targetState, context, intent, out _);
        }

        /// <summary>
        /// Forces a transition regardless of the current state's transition rules.
        /// </summary>
        /// <remarks>
        /// Terminal states still cannot be exited. This is intended for authoritative interrupts
        /// such as a knockout or match-ending event.
        /// </remarks>
        public bool ForceTransition(
            FighterStateId targetState,
            in FighterStateContext context,
            in FighterIntent intent)
        {
            EnsureInitialized();

            if (_currentState.Id == FighterStateId.Defeated || targetState == _currentState.Id)
            {
                return false;
            }

            if (!_states.TryGetValue(targetState, out IFighterState target))
            {
                return false;
            }

            TransitionTo(target, context, intent, out _);
            return true;
        }

        private bool TryTransition(
            FighterStateId targetState,
            in FighterStateContext context,
            in FighterIntent intent,
            out StateTransition transition)
        {
            EnsureInitialized();
            transition = default;

            if (targetState == _currentState.Id ||
                _currentState.Id == FighterStateId.Defeated ||
                !_currentState.CanTransitionTo(targetState) ||
                !_states.TryGetValue(targetState, out IFighterState target))
            {
                return false;
            }

            TransitionTo(target, context, intent, out transition);
            return true;
        }

        private void TransitionTo(
            IFighterState target,
            in FighterStateContext context,
            in FighterIntent intent,
            out StateTransition transition)
        {
            FighterStateId previousState = _currentState.Id;
            _currentState.Exit(context);

            _currentState = target;
            _timeInState = 0f;

            FighterIntent filteredIntent = intent.Filter(target.AcceptedIntents);
            target.Enter(context, filteredIntent);

            transition = new StateTransition(previousState, target.Id, context.Clock.CurrentTick);
        }

        private void EnsureInitialized()
        {
            if (_currentState == null)
            {
                throw new InvalidOperationException("State machine is not initialized.");
            }
        }
    }
}
