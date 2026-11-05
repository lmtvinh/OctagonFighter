using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Guard;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.UI;

namespace OctagonFighter.Presentation.UI
{
    /// <summary>
    /// Presents a single fighter's guard as a filled bar and a numeric label.
    /// </summary>
    /// <remarks>
    /// The bar is a pure presentation adapter: it listens to the <see cref="IGuardSource"/> events
    /// forwarded by the fighter controller and never polls the simulation. It mirrors the health and
    /// stamina bars so all three resources share one lifecycle and event-driven update model. A
    /// broken guard empties the bar and uses the broken color until the post-stun partial guard is
    /// restored.
    /// </remarks>
    public sealed class FighterGuardBar : MonoBehaviour
    {
        [SerializeField] private FighterController _fighter;
        [SerializeField] private Image _fill;
        [SerializeField] private Text _label;
        [SerializeField] private string _title = "GUARD";

        /// <summary>
        /// Optional rich-text format for the value label using {0} = current, {1} = maximum. When
        /// set (the rebuilt combat HUD uses it), the title prefix is omitted and the label renders
        /// the styled value only; when empty, the legacy "TITLE  cur / max" line is written.
        /// </summary>
        [SerializeField] private string _valueFormat;
        [SerializeField] private Color _readyColor = new Color(0.95f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color _lowColor = new Color(0.95f, 0.56f, 0.22f, 1f);
        [SerializeField] private Color _brokenColor = new Color(0.86f, 0.30f, 0.24f, 1f);
        [SerializeField, Range(0f, 1f)] private float _lowThreshold = 0.25f;

        private IGuardSource _guard;
        private bool _isBroken;

        /// <summary>Gets the fighter whose guard this bar presents.</summary>
        public FighterController Fighter => _fighter;

        /// <summary>Gets the image used as the bar fill.</summary>
        public Image Fill => _fill;

        /// <summary>Gets the label that reports the numeric guard.</summary>
        public Text Label => _label;

        /// <summary>Gets the normalized fill currently displayed by the bar.</summary>
        public float NormalizedGuard => _fill != null ? _fill.fillAmount : 0f;

        /// <summary>Gets a value indicating whether the guard is currently broken.</summary>
        public bool IsBroken => _isBroken;

        private void OnEnable()
        {
            _isBroken = false;
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
            if (_guard != null)
            {
                return;
            }

            _guard = _fighter;
            if (_guard == null)
            {
                return;
            }

            _guard.Changed += OnGuardChanged;
            _guard.Broken += OnGuardBroken;
        }

        private void Unbind()
        {
            if (_guard == null)
            {
                return;
            }

            _guard.Changed -= OnGuardChanged;
            _guard.Broken -= OnGuardBroken;
            _guard = null;
        }

        private void OnGuardChanged(GuardChangedEvent changed)
        {
            ApplyGuard(
                changed.CurrentGuard,
                changed.MaxGuard,
                _guard != null && _guard.IsBroken);
        }

        private void OnGuardBroken(GuardBrokenEvent broken)
        {
            ApplyGuard(
                0f,
                _guard != null ? _guard.MaxGuard : 0f,
                isBroken: true);
        }

        private void Refresh()
        {
            if (_guard == null)
            {
                return;
            }

            ApplyGuard(_guard.CurrentGuard, _guard.MaxGuard, _guard.IsBroken);
        }

        private void ApplyGuard(float currentGuard, float maximum, bool isBroken)
        {
            float normalized = maximum > 0f ? Mathf.Clamp01(currentGuard / maximum) : 0f;

            if (_fill != null)
            {
                _fill.fillAmount = normalized;
            }

            if (_label != null)
            {
                int current = Mathf.CeilToInt(currentGuard);
                int maximumInt = Mathf.CeilToInt(maximum);
                _label.text = string.IsNullOrEmpty(_valueFormat)
                    ? $"{_title}  {current} / {maximumInt}"
                    : string.Format(_valueFormat, current, maximumInt);
            }

            _isBroken = isBroken;

            if (_fill == null)
            {
                return;
            }

            _fill.color = isBroken
                ? _brokenColor
                : normalized <= _lowThreshold ? _lowColor : _readyColor;
        }
    }
}
