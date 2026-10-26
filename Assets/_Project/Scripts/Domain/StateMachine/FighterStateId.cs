namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Identifies a state in the fighter state machine.
    /// </summary>
    public enum FighterStateId : byte
    {
        /// <summary>
        /// Initial spawn state.
        /// </summary>
        Spawning = 0,

        /// <summary>
        /// Standing idle.
        /// </summary>
        Idle = 1,

        /// <summary>
        /// Intent-driven walking movement.
        /// </summary>
        Walk = 2,

        /// <summary>
        /// Defensive blocking stance.
        /// </summary>
        Blocking = 3,

        /// <summary>
        /// Timed evasive action.
        /// </summary>
        Dodging = 4,

        /// <summary>
        /// Generic locked state reserved for future committed combat actions.
        /// </summary>
        CommittedAction = 5,

        /// <summary>
        /// Reaction to received damage.
        /// </summary>
        HitReaction = 6,

        /// <summary>
        /// Downed state after a knockdown.
        /// </summary>
        KnockedDown = 7,

        /// <summary>
        /// Get-up or recovery window.
        /// </summary>
        Recovering = 8,

        /// <summary>
        /// Terminal defeated state.
        /// </summary>
        Defeated = 9,

        /// <summary>
        /// Intent-driven running movement.
        /// </summary>
        Run = 10,

        /// <summary>
        /// Fast light strike (jab).
        /// </summary>
        LightPunch = 11,

        /// <summary>
        /// Slow heavy strike (cross).
        /// </summary>
        HeavyPunch = 12
    }
}
