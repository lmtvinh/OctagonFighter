using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.UI;

namespace OctagonFighter.Presentation.UI
{
    /// <summary>
    /// Presents a single fighter's stamina as a filled bar and a numeric label.
    /// </summary>
    /// <remarks>
    /// The bar is a pure presentation adapter: it listens to <see cref="IStaminaSource"/> change
    /// events raised by the fighter's stamina component and never polls the combat simulation.
    /// If the bar starts before the fighter has initialized, the fighter's <c>Spawned</c> event
    /// supplies the initial refresh. The component deliberately mirrors <see cref="FighterHealthBar"/>
    /// so both resources share the same lifecycle and event-driven update model.
    /// </remarks>
    public sealed class FighterStaminaBar : MonoBehaviour
    {
        [SerializeField] private FighterController _fighter;
        [SerializeField] private Image _fill;
        [SerializeField] private Text _label;
        [SerializeField] private string _title = "STAMINA";

        /// <summary>
        /// Optional rich-text format for the value label using {0} = current, {1} = maximum. When
        /// set (the rebuilt combat HUD uses it), the title prefix is omitted and the label renders
        /// the styled value only; when empty, the legacy "TITLE  cur / max" line is written.
        /// </summary>
        [SerializeField] private string _valueFormat;
        [SerializeField] private Color _freshColor = new Color(0.28f, 0.72f, 0.95f, 1f);
        [SerializeField] private Color _tiredColor = new Color(0.95f, 0.72f, 0.23f, 1f);
        [SerializeField] private Color _gassedColor = new Color(0.86f, 0.30f, 0.24f, 1f);

        private IStaminaSource _stamina;

        /// <summary>Gets the fighter whose stamina this bar presents.</summary>
        public FighterController Fighter => _fighter;

        /// <summary>Gets the image used as the bar fill.</summary>
        public Image Fill => _fill;

        /// <summary>Gets the label that reports the numeric stamina.</summary>
        public Text Label => _label;

        /// <summary>Gets the normalized fill currently displayed by the bar.</summary>
        public float NormalizedStamina => _fill != null ? _fill.fillAmount : 0f;

        private void OnEnable()
        {
            if (_fighter == null)
            {
                return;
            }

            _fighter.Spawned += OnSpawned;
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
            if (_stamina != null)
            {
                return;
            }

            _stamina = _fighter.Stamina;
            if (_stamina == null)
            {
                return;
            }

            _stamina.Changed += OnStaminaChanged;
            _stamina.TierChanged += OnTierChanged;
            _stamina.Depleted += OnDepleted;
        }

        private void Unbind()
        {
            if (_stamina == null)
            {
                return;
            }

            _stamina.Changed -= OnStaminaChanged;
            _stamina.TierChanged -= OnTierChanged;
            _stamina.Depleted -= OnDepleted;
            _stamina = null;
        }

        private void OnStaminaChanged(StaminaChangedEvent changed)
        {
            ApplyStamina(changed.CurrentStamina, _stamina != null ? _stamina.Tier : StaminaTier.Fresh);
        }

        private void OnTierChanged(StaminaTierChangedEvent changed)
        {
            ApplyStamina(changed.CurrentStamina, changed.CurrentTier);
        }

        private void OnDepleted(StaminaDepletedEvent depleted)
        {
            ApplyStamina(0f, StaminaTier.Gassed);
        }

        private void Refresh()
        {
            if (_stamina == null)
            {
                return;
            }

            ApplyStamina(_stamina.CurrentStamina, _stamina.Tier);
        }

        private void ApplyStamina(float currentStamina, StaminaTier tier)
        {
            float maximum = _stamina != null ? _stamina.MaxStamina : 0f;
            float normalized = maximum > 0f ? Mathf.Clamp01(currentStamina / maximum) : 0f;

            if (_fill != null)
            {
                _fill.fillAmount = normalized;
            }

            if (_label != null)
            {
                int current = Mathf.CeilToInt(currentStamina);
                int maximumInt = Mathf.CeilToInt(_stamina != null ? _stamina.MaxStamina : 0f);
                _label.text = string.IsNullOrEmpty(_valueFormat)
                    ? $"{_title}  {current} / {maximumInt}"
                    : string.Format(_valueFormat, current, maximumInt);
            }

            ApplyColor(ColorFor(tier));
        }

        private Color ColorFor(StaminaTier tier)
        {
            switch (tier)
            {
                case StaminaTier.Gassed:
                    return _gassedColor;

                case StaminaTier.Tired:
                    return _tiredColor;

                default:
                    return _freshColor;
            }
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
