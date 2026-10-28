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
        private InputAction _lightPunchAction;
        private InputAction _heavyPunchAction;
        private bool _dodgeQueued;
        private bool _lightPunchQueued;
        private bool _heavyPunchQueued;

        /// <inheritdoc />
        public bool TryGetIntent(TickId tick, out FighterIntent intent)
        {
            Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            MovementGait gait = _runAction != null && _runAction.IsPressed()
                ? MovementGait.Run
                : MovementGait.Walk;
            bool blockHeld = _blockAction != null && _blockAction.IsPressed();
            bool blockPressed = _blockAction != null && _blockAction.WasPressedThisFrame();
            bool lightPunchPressed = _lightPunchQueued;
            bool heavyPunchPressed = _heavyPunchQueued;
            bool dodgePressed = _dodgeQueued;
            _lightPunchQueued = false;
            _heavyPunchQueued = false;
            _dodgeQueued = false;

            FighterCommand command = FighterCommand.None;
            bool isCommandPressed = false;
            bool isCommandHeld = false;

            if (lightPunchPressed)
            {
                command = FighterCommand.LightPunch;
                isCommandPressed = true;
            }
            else if (heavyPunchPressed)
            {
                command = FighterCommand.HeavyPunch;
                isCommandPressed = true;
            }
            else if (dodgePressed)
            {
                command = FighterCommand.Dodge;
                isCommandPressed = true;
            }
            else if (blockHeld)
            {
                command = FighterCommand.Block;
                isCommandPressed = blockPressed;
                isCommandHeld = true;
            }

            intent = new FighterIntent(
                new Float2(move.x, move.y),
                command,
                isCommandPressed,
                isCommandHeld,
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

            _lightPunchAction = new InputAction("LightPunch", InputActionType.Button);
            _lightPunchAction.AddBinding("<Keyboard>/j");
            _lightPunchAction.AddBinding("<Gamepad>/buttonWest");
            _lightPunchAction.AddBinding("<Mouse>/leftButton");
            _lightPunchAction.performed += OnLightPunchPerformed;

            _heavyPunchAction = new InputAction("HeavyPunch", InputActionType.Button);
            _heavyPunchAction.AddBinding("<Keyboard>/k");
            _heavyPunchAction.AddBinding("<Gamepad>/buttonNorth");
            _heavyPunchAction.performed += OnHeavyPunchPerformed;
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _runAction?.Enable();
            _blockAction?.Enable();
            _dodgeAction?.Enable();
            _lightPunchAction?.Enable();
            _heavyPunchAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _runAction?.Disable();
            _blockAction?.Disable();
            _dodgeAction?.Disable();
            _lightPunchAction?.Disable();
            _heavyPunchAction?.Disable();
            _dodgeQueued = false;
            _lightPunchQueued = false;
            _heavyPunchQueued = false;
        }

        private void OnDestroy()
        {
            if (_dodgeAction != null)
            {
                _dodgeAction.performed -= OnDodgePerformed;
                _dodgeAction.Dispose();
                _dodgeAction = null;
            }

            if (_lightPunchAction != null)
            {
                _lightPunchAction.performed -= OnLightPunchPerformed;
                _lightPunchAction.Dispose();
                _lightPunchAction = null;
            }

            if (_heavyPunchAction != null)
            {
                _heavyPunchAction.performed -= OnHeavyPunchPerformed;
                _heavyPunchAction.Dispose();
                _heavyPunchAction = null;
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

        private void OnLightPunchPerformed(InputAction.CallbackContext context)
        {
            _lightPunchQueued = true;
        }

        private void OnHeavyPunchPerformed(InputAction.CallbackContext context)
        {
            _heavyPunchQueued = true;
        }
    }
}
