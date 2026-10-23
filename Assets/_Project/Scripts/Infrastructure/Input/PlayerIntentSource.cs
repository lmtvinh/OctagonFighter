using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OctagonFighter.Infrastructure.Input
{
    /// <summary>
    /// Reads local player input through the Unity Input System and produces fighter intents.
    /// </summary>
    public sealed class PlayerIntentSource : MonoBehaviour, IFighterIntentSource
    {
        private InputAction _moveAction;
        private InputAction _runAction;
        private InputAction _blockAction;
        private InputAction _dodgeAction;
        private bool _dodgeQueued;

        /// <inheritdoc />
        public bool TryGetIntent(TickId tick, out FighterIntent intent)
        {
            Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            MovementGait gait = _runAction != null && _runAction.IsPressed()
                ? MovementGait.Run
                : MovementGait.Walk;
            bool blockHeld = _blockAction != null && _blockAction.IsPressed();
            bool blockPressed = _blockAction != null && _blockAction.WasPressedThisFrame();
            bool dodgePressed = _dodgeQueued;
            _dodgeQueued = false;

            if (dodgePressed)
            {
                intent = new FighterIntent(
                    new Float2(move.x, move.y),
                    FighterCommand.Dodge,
                    true,
                    false,
                    gait,
                    tick);
                return true;
            }

            if (blockHeld)
            {
                intent = new FighterIntent(
                    new Float2(move.x, move.y),
                    FighterCommand.Block,
                    blockPressed,
                    true,
                    gait,
                    tick);
                return true;
            }

            intent = new FighterIntent(
                new Float2(move.x, move.y),
                FighterCommand.None,
                false,
                false,
                gait,
                tick);
            return true;
        }

        private void Awake()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _runAction = new InputAction("Run", InputActionType.Button);
            _runAction.AddBinding("<Keyboard>/leftCtrl");
            _runAction.AddBinding("<Gamepad>/leftTrigger");
            _runAction.AddBinding("<Gamepad>/leftStickPress");

            _blockAction = new InputAction("Block", InputActionType.Button);
            _blockAction.AddBinding("<Keyboard>/space");
            _blockAction.AddBinding("<Gamepad>/rightShoulder");
            _blockAction.AddBinding("<Mouse>/rightButton");

            _dodgeAction = new InputAction("Dodge", InputActionType.Button);
            _dodgeAction.AddBinding("<Keyboard>/leftShift");
            _dodgeAction.AddBinding("<Gamepad>/buttonEast");
            _dodgeAction.performed += OnDodgePerformed;
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _runAction?.Enable();
            _blockAction?.Enable();
            _dodgeAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _runAction?.Disable();
            _blockAction?.Disable();
            _dodgeAction?.Disable();
            _dodgeQueued = false;
        }

        private void OnDestroy()
        {
            if (_dodgeAction != null)
            {
                _dodgeAction.performed -= OnDodgePerformed;
                _dodgeAction.Dispose();
                _dodgeAction = null;
            }

            _moveAction?.Dispose();
            _moveAction = null;

            _runAction?.Dispose();
            _runAction = null;

            _blockAction?.Dispose();
            _blockAction = null;
        }

        private void OnDodgePerformed(InputAction.CallbackContext context)
        {
            _dodgeQueued = true;
        }
    }
}
