using System;
using OctagonFighter.Core.Time;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Time
{
    /// <summary>
    /// Advances the shared combat clock on a fixed simulation rate.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class CombatTickDriver : MonoBehaviour
    {
        private const int MaximumTicksPerFrame = 8;

        [SerializeField, Min(1)] private int _ticksPerSecond = 60;

        private readonly CombatClock _clock = new CombatClock();
        private double _accumulator;

        /// <summary>
        /// Gets the shared combat clock.
        /// </summary>
        public ICombatClock Clock => _clock;

        /// <summary>
        /// Gets the configured simulation rate.
        /// </summary>
        public int TicksPerSecond => _ticksPerSecond;

        /// <summary>
        /// Occurs once per fixed simulation tick.
        /// </summary>
        public event Action<TickId, float> Tick;

        private void FixedUpdate()
        {
            double step = 1d / _ticksPerSecond;
            _accumulator += UnityEngine.Time.fixedDeltaTime;

            int ticksApplied = 0;
            while (_accumulator >= step && ticksApplied < MaximumTicksPerFrame)
            {
                _accumulator -= step;
                ticksApplied++;
                TickId tick = _clock.Advance();
                Tick?.Invoke(tick, (float)step);
            }
        }

        private void OnValidate()
        {
            if (_ticksPerSecond < 1)
            {
                _ticksPerSecond = 1;
            }
        }
    }
}
