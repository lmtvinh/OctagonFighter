using System.Collections.Generic;
using OctagonFighter.Application.Combat;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Health;
using OctagonFighter.Infrastructure.Combat;
using OctagonFighter.Infrastructure.Time;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;

namespace OctagonFighter.Presentation.Scenes
{
    /// <summary>
    /// Composition root that drives every fighter in a combat scene from one clock.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CombatSession : MonoBehaviour, IFighterRegistry
    {
        [SerializeField] private CombatTickDriver _tickDriver;
        [SerializeField] private FighterController[] _fighters;

        private readonly Dictionary<FighterId, IDamageable> _damageables =
            new Dictionary<FighterId, IDamageable>();
        private readonly SweptHitDetector _hitDetector = new SweptHitDetector();

        private void Start()
        {
            if (_tickDriver == null)
            {
                Debug.LogWarning(
                    "Combat session has no tick driver. Fighters will use their own fixed-tick loops.",
                    this);
                return;
            }

            if (_fighters == null)
            {
                return;
            }

            for (int index = 0; index < _fighters.Length; index++)
            {
                FighterController fighter = _fighters[index];
                if (fighter == null)
                {
                    continue;
                }

                FighterId runtimeId = new FighterId($"combatant_{index + 1}");
                fighter.SetAutoTick(false);
                fighter.Initialize(_tickDriver.Clock, this, _hitDetector, runtimeId);
                _damageables[runtimeId] = fighter.Damageable;
            }

            _tickDriver.Tick += OnTick;
        }

        /// <inheritdoc />
        public bool TryGetDamageable(FighterId fighterId, out IDamageable damageable)
        {
            if (!fighterId.IsEmpty && _damageables.TryGetValue(fighterId, out damageable))
            {
                return true;
            }

            damageable = null;
            return false;
        }

        private void OnDestroy()
        {
            if (_tickDriver != null)
            {
                _tickDriver.Tick -= OnTick;
            }

            _damageables.Clear();
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
