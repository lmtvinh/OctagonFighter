using System;
using OctagonFighter.Core.Ids;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Combat
{
    /// <summary>
    /// Links a scene object hierarchy to a simulation fighter identifier.
    /// </summary>
    public sealed class FighterIdentity : MonoBehaviour
    {
        [SerializeField] private string _fighterId;

        /// <summary>
        /// Gets the fighter identifier, or an empty identifier before initialization.
        /// </summary>
        public FighterId Id { get; private set; }

        /// <summary>
        /// Gets a value indicating whether an identifier has been assigned.
        /// </summary>
        public bool IsInitialized => !Id.IsEmpty;

        /// <summary>
        /// Assigns the simulation identifier for this fighter.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the identifier is empty.</exception>
        public void Initialize(FighterId fighterId)
        {
            if (fighterId.IsEmpty)
            {
                throw new ArgumentException("Fighter identity requires a non-empty identifier.", nameof(fighterId));
            }

            Id = fighterId;
            _fighterId = fighterId.Value;
        }

        private void Awake()
        {
            if (!Id.IsEmpty || string.IsNullOrWhiteSpace(_fighterId))
            {
                return;
            }

            Id = new FighterId(_fighterId);
        }
    }
}
