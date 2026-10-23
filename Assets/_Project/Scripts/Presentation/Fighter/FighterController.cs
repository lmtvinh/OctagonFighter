using System;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;
using OctagonFighter.Data.Fighters;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Infrastructure.Health;
using OctagonFighter.Infrastructure.Movement;
using OctagonFighter.Infrastructure.Stamina;
using UnityEngine;

namespace OctagonFighter.Presentation.Fighter
{
    /// <summary>
    /// Unity composition root and orchestrator for exactly one fighter.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(StaminaComponent))]
    [RequireComponent(typeof(FighterMotor))]
    public sealed class FighterController : MonoBehaviour
    {
        private const int MaximumAutoTicksPerFrame = 8;

        [Header("Definition")]
        [SerializeField] private FighterDefinition _definition;

        [Header("Intent")]
        [SerializeField] private MonoBehaviour _intentSourceBehaviour;

        [Header("Presentation")]
        [SerializeField] private FighterMotor _motor;
        [SerializeField] private FighterView _view;

        [Header("Ticking")]
        [SerializeField, Min(1)] private int _ticksPerSecond = 60;
        [SerializeField] private bool _autoTick = true;

        private readonly CombatClock _ownedClock = new CombatClock();
        private HealthComponent _healthComponent;
        private StaminaComponent _staminaComponent;
        private IFighterIntentSource _intentSource;
        private ICombatClock _clock;
        private FighterRuntime _runtime;
        private bool _ownsClock = true;
        private bool _isInitialized;
        private double _accumulator;

        /// <summary>
        /// Gets the fighter identifier.
        /// </summary>
        public FighterId Id => _runtime != null ? _runtime.Data.Id : default;

        /// <summary>
        /// Gets the latest immutable fighter snapshot.
        /// </summary>
        public FighterSnapshot Snapshot => _runtime != null ? _runtime.Snapshot : default;

        /// <summary>
        /// Gets the damage receiver for this fighter.
        /// </summary>
        public IDamageable Damageable => _healthComponent;

        /// <summary>
        /// Gets the read-only health source for this fighter.
        /// </summary>
        public IHealthSource Health => _healthComponent;

        /// <summary>
        /// Gets the read-only stamina source for this fighter.
        /// </summary>
        public IStaminaSource Stamina => _staminaComponent;

        /// <summary>
        /// Gets a value indicating whether the fighter has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Occurs after a successful state transition.
        /// </summary>
        public event Action<FighterStateChangedEvent> StateChanged;

        /// <summary>
        /// Occurs once when the fighter enters the simulation.
        /// </summary>
        public event Action<FighterSpawnedEvent> Spawned;

        /// <summary>
        /// Occurs once when the fighter becomes defeated.
        /// </summary>
        public event Action<FighterDefeatedEvent> Defeated;

        /// <summary>
        /// Enables or disables the controller's own fixed-tick loop.
        /// </summary>
        /// <remarks>
        /// A shared combat driver disables auto ticking so every fighter advances on the
        /// same clock.
        /// </remarks>
        public void SetAutoTick(bool autoTick)
        {
            _autoTick = autoTick;
            _accumulator = 0d;
        }

        /// <summary>
        /// Initializes the fighter from the assigned definition using its own clock.
        /// </summary>
        public void Initialize()
        {
            InitializeInternal(null);
        }

        /// <summary>
        /// Initializes the fighter from the assigned definition using an external clock.
        /// </summary>
        public void Initialize(ICombatClock clock)
        {
            InitializeInternal(clock);
        }

        /// <summary>
        /// Initializes the fighter from explicit runtime data.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when a dependency is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the fighter is already initialized.</exception>
        public void Initialize(
            FighterData data,
            IFighterIntentSource intentSource,
            ICombatClock clock)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Fighter controller is already initialized.");
            }

            if (intentSource == null)
            {
                throw new ArgumentNullException(nameof(intentSource));
            }

            _clock = clock ?? _ownedClock;
            _ownsClock = clock == null;

            _intentSource = intentSource;
            _healthComponent.Initialize(data.Health);
            _staminaComponent.Initialize(data.Stamina);
            _motor.Initialize(data.Movement);
            _view?.Initialize();

            var stateMachine = FoundationStateMachineFactory.Create();
            _runtime = new FighterRuntime(
                data,
                _healthComponent.Model,
                _staminaComponent.Model,
                stateMachine,
                _intentSource,
                _clock);

            _runtime.StateChanged += OnRuntimeStateChanged;
            _runtime.Defeated += OnRuntimeDefeated;
            _isInitialized = true;

            Spawned?.Invoke(new FighterSpawnedEvent(data.Id, _clock.CurrentTick));
        }

        /// <summary>
        /// Advances the fighter by one fixed tick.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the fighter is not initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the delta time is invalid.</exception>
        public void Tick(float deltaSeconds)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Fighter controller is not initialized.");
            }

            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            _runtime.Tick(deltaSeconds);

            FighterIntent intent = _runtime.CurrentIntent;
            _motor.Tick(intent.Move, _runtime.CurrentMovementAuthority, deltaSeconds);
            _view?.Apply(_runtime.Snapshot, intent.Move, _runtime.CurrentMovementAuthority);
        }

        private void Awake()
        {
            if (!TryGetComponent(out _healthComponent))
            {
                throw new MissingComponentException(
                    $"{nameof(FighterController)} requires a {nameof(HealthComponent)} component.");
            }

            if (!TryGetComponent(out _staminaComponent))
            {
                throw new MissingComponentException(
                    $"{nameof(FighterController)} requires a {nameof(StaminaComponent)} component.");
            }

            if (_motor == null && !TryGetComponent(out _motor))
            {
                throw new MissingComponentException(
                    $"{nameof(FighterController)} requires a {nameof(FighterMotor)} component.");
            }

            _intentSource = _intentSourceBehaviour as IFighterIntentSource;
        }

        private void Start()
        {
            if (!_isInitialized && _definition != null)
            {
                Initialize();
            }
        }

        private void FixedUpdate()
        {
            if (!_autoTick || !_isInitialized || !_ownsClock)
            {
                return;
            }

            double step = 1d / _ticksPerSecond;
            _accumulator += Time.fixedDeltaTime;

            int ticksApplied = 0;
            while (_accumulator >= step && ticksApplied < MaximumAutoTicksPerFrame)
            {
                _accumulator -= step;
                ticksApplied++;
                _ownedClock.Advance();
                Tick((float)step);
            }
        }

        private void OnDestroy()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.StateChanged -= OnRuntimeStateChanged;
            _runtime.Defeated -= OnRuntimeDefeated;
            _runtime.Dispose();
            _runtime = null;
            _isInitialized = false;
        }

        private void InitializeInternal(ICombatClock clock)
        {
            if (_isInitialized)
            {
                return;
            }

            if (_definition == null)
            {
                throw new InvalidOperationException("Fighter definition has not been assigned.");
            }

            if (_intentSource == null)
            {
                throw new InvalidOperationException(
                    "An intent source implementing IFighterIntentSource must be assigned.");
            }

            FighterData data = _definition.CreateRuntimeData();
            Initialize(data, _intentSource, clock);
        }

        private void OnRuntimeStateChanged(FighterStateChangedEvent stateChangedEvent)
        {
            StateChanged?.Invoke(stateChangedEvent);
        }

        private void OnRuntimeDefeated(FighterDefeatedEvent defeatedEvent)
        {
            Defeated?.Invoke(defeatedEvent);
        }
    }
}
