using System;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Player or AI input for one simulation tick.
    /// </summary>
    public readonly struct FighterIntent
    {
        /// <summary>
        /// Gets an empty intent.
        /// </summary>
        public static FighterIntent None => new FighterIntent(Float2.Zero, FighterCommand.None, false, false, TickId.Zero);

        /// <summary>
        /// Gets the normalized movement direction.
        /// </summary>
        public Float2 Move { get; }

        /// <summary>
        /// Gets the requested command.
        /// </summary>
        public FighterCommand Command { get; }

        /// <summary>
        /// Gets a value indicating whether the command was pressed this tick.
        /// </summary>
        public bool IsCommandPressed { get; }

        /// <summary>
        /// Gets a value indicating whether the command is held.
        /// </summary>
        public bool IsCommandHeld { get; }

        /// <summary>
        /// Gets the requested movement gait.
        /// </summary>
        public MovementGait Gait { get; }

        /// <summary>
        /// Gets the tick this intent belongs to.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Gets a value indicating whether movement input is present.
        /// </summary>
        public bool IsMovementRequested => Move.SqrMagnitude > 0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterIntent"/> struct using a walking gait.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when movement is not finite.</exception>
        /// <exception cref="ArgumentException">Thrown when the command is undefined.</exception>
        public FighterIntent(
            Float2 move,
            FighterCommand command,
            bool isCommandPressed,
            bool isCommandHeld,
            TickId tick)
            : this(move, command, isCommandPressed, isCommandHeld, MovementGait.Walk, tick)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterIntent"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when movement is not finite.</exception>
        /// <exception cref="ArgumentException">Thrown when the command or gait is undefined.</exception>
        public FighterIntent(
            Float2 move,
            FighterCommand command,
            bool isCommandPressed,
            bool isCommandHeld,
            MovementGait gait,
            TickId tick)
        {
            if (!move.IsFinite)
            {
                throw new ArgumentOutOfRangeException(nameof(move), "Movement input must be finite.");
            }

            if (!Enum.IsDefined(typeof(FighterCommand), command))
            {
                throw new ArgumentException("Command is not defined.", nameof(command));
            }

            if (!Enum.IsDefined(typeof(MovementGait), gait))
            {
                throw new ArgumentException("Movement gait is not defined.", nameof(gait));
            }

            Move = move.ClampMagnitude(1f);
            Command = command;
            IsCommandPressed = isCommandPressed;
            IsCommandHeld = isCommandHeld;
            Gait = gait;
            Tick = tick;
        }

        /// <summary>
        /// Returns a copy of this intent with a different command and command flags.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the command is undefined.</exception>
        public FighterIntent WithCommand(
            FighterCommand command,
            bool isCommandPressed,
            bool isCommandHeld)
        {
            return new FighterIntent(Move, command, isCommandPressed, isCommandHeld, Gait, Tick);
        }

        /// <summary>
        /// Removes inputs that are not accepted by a state.
        /// </summary>
        public FighterIntent Filter(FighterIntentMask acceptedIntents)
        {
            bool acceptsMovement = (acceptedIntents & FighterIntentMask.Move) != 0;
            bool acceptsCommand = (acceptedIntents & GetCommandMask(Command)) != 0;

            return new FighterIntent(
                acceptsMovement ? Move : Float2.Zero,
                acceptsCommand ? Command : FighterCommand.None,
                acceptsCommand && IsCommandPressed,
                acceptsCommand && IsCommandHeld,
                Gait,
                Tick);
        }

        private static FighterIntentMask GetCommandMask(FighterCommand command)
        {
            switch (command)
            {
                case FighterCommand.Block:
                    return FighterIntentMask.Block;

                case FighterCommand.Dodge:
                    return FighterIntentMask.Dodge;

                case FighterCommand.Cancel:
                    return FighterIntentMask.Cancel;

                case FighterCommand.Interact:
                    return FighterIntentMask.Interact;

                case FighterCommand.StandUp:
                    return FighterIntentMask.StandUp;

                case FighterCommand.LightPunch:
                case FighterCommand.HeavyPunch:
                    return FighterIntentMask.Attack;

                default:
                    return FighterIntentMask.None;
            }
        }
    }
}
