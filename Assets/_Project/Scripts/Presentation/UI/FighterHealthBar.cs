using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.UI;

namespace OctagonFighter.Presentation.UI
{
    /// <summary>
    /// Presents a single fighter's health as a filled bar and a numeric label.
    /// </summary>
    /// <remarks>
    /// The bar is a pure presentation adapter: it listens to <see cref="IHealthSource"/> change
    /// events raised by the fighter's health component and never polls the combat simulation.
    /// If the bar starts before the fighter has initialized, the fighter's <c>Spawned</c> event
    /// supplies the initial refresh.
    /// </remarks>
    public sealed class FighterHealthBar : MonoBehaviour
    {
        [SerializeField] private FighterController _fighter;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _trailFill;
        [SerializeField] private Text _label;
        [SerializeField] private string _title = "FIGHTER";

        /// <summary>
        /// Optional rich-text format for the value label using {0} = current, {1} = maximum. When
        /// set (the rebuilt combat HUD uses it), the title prefix is omitted and the label renders
        /// the styled value only; when empty, the legacy "TITLE  cur / max" line is written.
        /// </summary>
        [SerializeField] private string _valueFormat;
        [SerializeField, Min(0f)] private float _damageTrailDelay = 0.22f;
        [SerializeField, Min(0.01f)] private float _damageTrailSeconds = 0.65f;
        [SerializeField] private Color _healthyColor = new Color(0.34f, 0.79f, 0.45f, 1f);
        [SerializeField] private Color _defeatedColor = new Color(0.78f, 0.24f, 0.24f, 1f);

        private IHealthSource _health;
        private float _trailDelayRemaining;

        /// <summary>Gets the fighter whose health this bar presents.</summary>
        public FighterController Fighter => _fighter;

        /// <summary>Gets the image used as the bar fill.</summary>
        public Image Fill => _fill;

        /// <summary>Gets the label that reports the numeric health.</summary>
        public Text Label => _label;

        /// <summary>Gets the normalized fill currently displayed by the bar.</summary>
        public float NormalizedHealth => _fill != null ? _fill.fillAmount : 0f;

        /// <summary>
        /// Gets the normalized fill currently displayed by the delayed damage trail, or zero when no
        /// trail image is wired. The trail lags the health bar for a few tenths of a second after a
        /// hit so recent damage stays readable.
        /// </summary>
        public float NormalizedTrail => _trailFill != null ? _trailFill.fillAmount : 0f;

        private void OnEnable()
        {
            _trailDelayRemaining = 0f;
            if (_fighter == null)
            {
                return;
            }

            _fighter.Spawned += OnSpawned;
            _fighter.Defeated += OnDefeated;

            if (_fighter.IsInitialized)
            {
                Bind();
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (_fighter != null)
            {
                _fighter.Spawned -= OnSpawned;
                _fighter.Defeated -= OnDefeated;
            }

            Unbind();
        }

        private void OnSpawned(FighterSpawnedEvent spawned)
        {
            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (_health != null)
            {
                return;
            }

            _health = _fighter.Health;
            if (_health == null)
            {
                return;
            }

            _health.Changed += OnHealthChanged;
            _health.Depleted += OnDepleted;
        }

        private void Unbind()
        {
            if (_health == null)
            {
                return;
            }

            _health.Changed -= OnHealthChanged;
            _health.Depleted -= OnDepleted;
            _health = null;
        }

        private void OnDefeated(FighterDefeatedEvent defeated)
        {
            ApplyHealth(0f);
            ApplyColor(_defeatedColor);
        }

        private void OnHealthChanged(HealthChangedEvent changed)
        {
            ApplyHealth(changed.CurrentHealth);
        }

        private void OnDepleted(HealthDepletedEvent depleted)
        {
            ApplyHealth(0f);
            ApplyColor(_defeatedColor);
        }

        private void Refresh()
        {
            if (_health == null)
            {
                return;
            }

            ApplyHealth(_health.CurrentHealth);
            if (_trailFill != null && _fill != null)
            {
                _trailFill.fillAmount = _fill.fillAmount;
                _trailDelayRemaining = 0f;
            }

            ApplyColor(_health.IsAlive ? _healthyColor : _defeatedColor);
        }

        private void ApplyHealth(float currentHealth)
        {
            float maximum = _health != null ? _health.MaxHealth : 0f;
            float normalized = maximum > 0f ? Mathf.Clamp01(currentHealth / maximum) : 0f;

            if (_fill != null)
            {
                _fill.fillAmount = normalized;
            }

            ApplyTrail(normalized);

            if (_label != null)
            {
                int current = Mathf.CeilToInt(currentHealth);
                int maximumInt = Mathf.CeilToInt(_health != null ? _health.MaxHealth : 0f);
                _label.text = string.IsNullOrEmpty(_valueFormat)
                    ? $"{_title}  {current} / {maximumInt}"
                    : string.Format(_valueFormat, current, maximumInt);
            }
        }

        private void ApplyTrail(float normalized)
        {
            if (_trailFill == null)
            {
                return;
            }

            float currentTrail = _trailFill.fillAmount;
            if (normalized >= currentTrail)
            {
                // Healing, spawning, and resets snap the trail forward; damage always trails behind.
                _trailFill.fillAmount = normalized;
                _trailDelayRemaining = 0f;
                return;
            }

            // Damage keeps the old trail value and starts the hold-off timer; Update drains it.
            _trailDelayRemaining = Mathf.Max(_trailDelayRemaining, _damageTrailDelay);
        }

        private void Update()
        {
            if (_trailFill == null || _fill == null)
            {
                return;
            }

            float target = _fill.fillAmount;
            float currentTrail = _trailFill.fillAmount;
            if (Mathf.Approximately(currentTrail, target) || currentTrail < target)
            {
                return;
            }

            if (_trailDelayRemaining > 0f)
            {
                _trailDelayRemaining = Mathf.Max(0f, _trailDelayRemaining - Time.deltaTime);
                return;
            }

            _trailFill.fillAmount = Mathf.MoveTowards(
                currentTrail,
                target,
                Time.deltaTime / _damageTrailSeconds);
        }

        private void ApplyColor(Color color)
        {
            if (_fill != null)
            {
                _fill.color = color;
            }
        }
    }
}
