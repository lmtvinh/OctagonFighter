using OctagonFighter.Domain.Combat;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.UI;

namespace OctagonFighter.Presentation.UI
{
    /// <summary>
    /// Presents HEAD/BODY/LEG hit feedback for one fighter, expiring after a configurable duration.
    /// </summary>
    /// <remarks>
    /// The feedback is purely presentational: it listens to <see cref="FighterController.HitFeedback"/>
    /// (raised exactly once per resolved strike) and shows the region with an optional blocked prefix.
    /// It never polls the simulation and never creates duplicate events -- one event per strike is
    /// enforced at the event source.
    /// </remarks>
    [RequireComponent(typeof(FighterController))]
    public sealed class HitRegionFeedback : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController _fighter;
        [SerializeField] private Text _label;

        [Header("Settings")]
        [Tooltip("Seconds the feedback remains visible before expiring.")]
        [SerializeField, Min(0f)] private float _expirySeconds = 1.5f;

        private float _expiryRemaining;
        private bool _active;

        /// <summary>
        /// Gets a value indicating whether feedback is currently being shown.
        /// </summary>
        public bool IsShowing => _active;

        private void Reset()
        {
            _fighter = _fighter ?? GetComponent<FighterController>();
            if (_label == null)
            {
                _label = GetComponentInChildren<Text>();
            }
        }

        private void OnValidate()
        {
            _fighter = _fighter ?? GetComponent<FighterController>();
            if (_label == null)
            {
                _label = GetComponentInChildren<Text>();
            }
        }

        private void OnEnable()
        {
            if (_fighter != null)
            {
                _fighter.HitFeedback += OnHitFeedback;
            }
        }

        private void OnDisable()
        {
            if (_fighter != null)
            {
                _fighter.HitFeedback -= OnHitFeedback;
            }
        }

        private void OnHitFeedback(HitFeedback feedback)
        {
            _label.text = feedback.Blocked
                ? $"BLOCKED {feedback.Region}"
                : feedback.Region.ToString().ToUpperInvariant();
            _expiryRemaining = _expirySeconds;
            if (!_active)
            {
                _active = true;
                if (_label != null)
                {
                    _label.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            _expiryRemaining -= Time.deltaTime;
            if (_expiryRemaining <= 0f && _label != null)
            {
                _active = false;
                _label.gameObject.SetActive(false);
            }
        }
    }
}
