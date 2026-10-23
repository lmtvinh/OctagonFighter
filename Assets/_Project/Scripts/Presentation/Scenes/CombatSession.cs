using OctagonFighter.Core.Time;
using OctagonFighter.Infrastructure.Time;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;

namespace OctagonFighter.Presentation.Scenes
{
    /// <summary>
    /// Composition root that drives every fighter in a combat scene from one clock.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CombatSession : MonoBehaviour
    {
        [SerializeField] private CombatTickDriver _tickDriver;
        [SerializeField] private FighterController[] _fighters;

        private void Start()
        {
            if (_tickDriver == null)
            {
                Debug.LogWarning(
                    "Combat session has no tick driver. Fighters will use their own fixed-tick loops.",
                    this);
                return;
            }

            if (_fighters != null)
            {
                foreach (FighterController fighter in _fighters)
                {
                    if (fighter == null)
                    {
                        continue;
                    }

                    fighter.SetAutoTick(false);
                    fighter.Initialize(_tickDriver.Clock);
                }
            }

            _tickDriver.Tick += OnTick;
        }

        private void OnDestroy()
        {
            if (_tickDriver != null)
            {
                _tickDriver.Tick -= OnTick;
            }
        }

        private void OnTick(TickId tick, float deltaSeconds)
        {
            if (_fighters == null)
            {
                return;
            }

            foreach (FighterController fighter in _fighters)
            {
                if (fighter != null && fighter.IsInitialized)
                {
                    fighter.Tick(deltaSeconds);
                }
            }
        }
    }
}
