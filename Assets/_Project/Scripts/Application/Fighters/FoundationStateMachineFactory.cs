using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Domain.StateMachine.States;

namespace OctagonFighter.Application.Fighters
{
    /// <summary>
    /// Builds the foundation state machine with lifecycle, movement, and reaction states.
    /// </summary>
    public static class FoundationStateMachineFactory
    {
        /// <summary>
        /// Creates a state machine containing every foundation state exactly once.
        /// </summary>
        public static FighterStateMachine Create()
        {
            var stateMachine = new FighterStateMachine();
            stateMachine.Register(new SpawningState());
            stateMachine.Register(new IdleState());
            stateMachine.Register(new WalkState());
            stateMachine.Register(new RunState());
            stateMachine.Register(new BlockingState());
            stateMachine.Register(new DodgingState());
            stateMachine.Register(new LightPunchState());
            stateMachine.Register(new HeavyPunchState());
            stateMachine.Register(new CommittedActionState());
            stateMachine.Register(new HitReactionState());
            stateMachine.Register(new KnockedDownState());
            stateMachine.Register(new RecoveringState());
            stateMachine.Register(new DefeatedState());
            return stateMachine;
        }
    }
}
