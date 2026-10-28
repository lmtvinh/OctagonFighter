using System;
using OctagonFighter.Application.Combat;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Data.Fighters;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Infrastructure.Combat;
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
    [RequireComponent(typeof(FighterIdentity))]
    public sealed class FighterController : MonoBehaviour
    {
        private const int MaximumAutoTicksPerFrame = 8;

        [Header("Definition")]
        [SerializeField] private FighterDefinition _definition;

        [Header("Intent")]
        [SerializeField] private MonoBehaviour _intentSourceBehaviour;

        [Header("Combat")]
        [SerializeField] private FighterIdentity _identity;
        [SerializeField] private FighterStrikeRig _strikeRig;

        [Header("Presentation")]
        [SerializeField] private FighterMotor _motor;
        [SerializeField] private FighterView _view;

        private readonly HitRegistry _hitRegistry = new HitRegistry();
        private IFighterRegistry _fighterRegistry;
        private IHitDetector _hitDetector;
        private HitId _activeHitId;

        [Header("Ticking")]
        [SerializeField, Min(1)] private int _ticksPerSecond = 60;
        [SerializeField] private bool _autoTick = true;

        [Header("Debug")]
        [SerializeField] private bool _logResolvedHits = true;

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
            InitializeInternal(null, null, null, null);
        }

        /// <summary>
        /// Initializes the fighter from the assigned definition using an external clock.
        /// </summary>
        public void Initialize(ICombatClock clock)
        {
            InitializeInternal(clock, null, null, null);
        }

        /// <summary>
        /// Initializes the fighter from the assigned definition with shared combat services.
        /// </summary>
        public void Initialize(
            ICombatClock clock,
            IFighterRegistry fighterRegistry,
            IHitDetector hitDetector)
        {
            InitializeInternal(clock, fighterRegistry, hitDetector, null);
        }

        /// <summary>
        /// Initializes the fighter from the assigned definition with a unique runtime identity.
        /// </summary>
        public void Initialize(
            ICombatClock clock,
            IFighterRegistry fighterRegistry,
            IHitDetector hitDetector,
            FighterId runtimeId)
        {
            InitializeInternal(clock, fighterRegistry, hitDetector, runtimeId);
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
            Initialize(data, intentSource, clock, null, null);
        }

        /// <summary>
        /// Initializes the fighter from explicit runtime data with shared combat services.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when a dependency is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the fighter is already initialized.</exception>
        public void Initialize(
            FighterData data,
            IFighterIntentSource intentSource,
            ICombatClock clock,
            IFighterRegistry fighterRegistry,
            IHitDetector hitDetector)
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
            _fighterRegistry = fighterRegistry;
            _hitDetector = hitDetector;

            _intentSource = intentSource;
            _identity.Initialize(data.Id);
            _healthComponent.Initialize(data.Health);
            _staminaComponent.Initialize(data.Stamina);
            _motor.Initialize(data.Movement);
            _view?.Initialize(data.Movement);
            _strikeRig?.Initialize();

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
            _runtime.HitRequested += OnRuntimeHitRequested;
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
            bool allowFacing = _runtime.CurrentFacingAuthority == FacingAuthority.Free;
            _motor.Tick(
                intent.Move,
                intent.Gait,
                _runtime.CurrentMovementAuthority,
                allowFacing,
                deltaSeconds);
            _view?.Apply(_runtime.Snapshot, _motor.PlanarVelocity, deltaSeconds);
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

            if (_identity == null && !TryGetComponent(out _identity))
            {
                _identity = gameObject.AddComponent<FighterIdentity>();
            }

            if (_strikeRig == null && !TryGetComponent(out _strikeRig))
            {
                _strikeRig = gameObject.AddComponent<FighterStrikeRig>();
            }

            _strikeRig.Initialize();

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
            _runtime.HitRequested -= OnRuntimeHitRequested;
            _runtime.Dispose();
            _runtime = null;
            _hitRegistry.Clear();
            _isInitialized = false;
        }

        private void InitializeInternal(
            ICombatClock clock,
            IFighterRegistry fighterRegistry,
            IHitDetector hitDetector,
            FighterId? runtimeId)
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
            if (runtimeId.HasValue)
            {
                data = data.WithId(runtimeId.Value);
            }

            Initialize(data, _intentSource, clock, fighterRegistry, hitDetector);
        }

        private void OnRuntimeStateChanged(FighterStateChangedEvent stateChangedEvent)
        {
            // A strike owns its hit window for exactly as long as the attack state runs. Releasing
            // here keeps the registry bounded and lets the next strike claim its own window.
            if (IsAttackState(stateChangedEvent.PreviousState) &&
                !IsAttackState(stateChangedEvent.CurrentState))
            {
                if (!_activeHitId.IsEmpty)
                {
                    _hitRegistry.Release(_activeHitId);
                    _activeHitId = default;
                }
            }

            StateChanged?.Invoke(stateChangedEvent);
        }

        private void OnRuntimeDefeated(FighterDefeatedEvent defeatedEvent)
        {
            Defeated?.Invoke(defeatedEvent);
        }

        private void OnRuntimeHitRequested(AttackHitRequest request)
        {
            if (_hitDetector == null || _fighterRegistry == null || _strikeRig == null)
            {
                return;
            }

            // A strike can only damage one target, so once the window is claimed the remaining
            // contact ticks do not need a physics query at all.
            if (_hitRegistry.IsClaimed(request.HitId))
            {
                return;
            }

            if (request.HitId != _activeHitId)
            {
                // Anchor the sweep at the first tick of the contact window so it never spans the
                // wind-up from the guard position.
                _activeHitId = request.HitId;
                _strikeRig.BeginSweep(request.Attack.Hand);
            }

            if (!_strikeRig.TrySampleSweep(
                    request.Attack.Hand,
                    out Vector3 previousHand,
                    out Vector3 currentHand))
            {
                return;
            }

            var attackerRoot = ToFloat3(transform.position);
            if (!request.Attack.IsHandInReach(attackerRoot, ToFloat3(currentHand)))
            {
                return;
            }

            HitQuery query = HitQuery.FromHandSweep(
                request.HitId,
                request.AttackerId,
                ToFloat3(previousHand),
                ToFloat3(currentHand),
                ToFloat3(transform.forward),
                request.Attack.Radius,
                request.Tick);

            if (!_hitDetector.TryFindTarget(query, out HitCandidate candidate))
            {
                return;
            }

            // Resolve the damageable before claiming so an unresolvable target never burns the
            // strike's one claim.
            if (!_fighterRegistry.TryGetDamageable(candidate.TargetId, out IDamageable damageable))
            {
                return;
            }

            if (!_hitRegistry.TryClaim(request.HitId, candidate.TargetId))
            {
                return;
            }

            DamageResult result = damageable.ApplyDamage(new DamageEvent(
                request.AttackerId,
                candidate.TargetId,
                request.Attack.Damage,
                DamageType.Impact,
                request.Attack.Region,
                request.Tick));

            if (_logResolvedHits)
            {
                Debug.Log(
                    $"{request.AttackerId.Value} landed {request.Attack.DisplayName} on " +
                    $"{candidate.TargetId.Value} for {result.AppliedAmount:0.#} damage " +
                    $"({request.Attack.Region}).",
                    this);
            }
        }

        private static Float3 ToFloat3(Vector3 value)
        {
            return new Float3(value.x, value.y, value.z);
        }

        private static bool IsAttackState(FighterStateId state)
        {
            return state == FighterStateId.LightPunch ||
                   state == FighterStateId.HeavyPunch;
        }
    }
}
