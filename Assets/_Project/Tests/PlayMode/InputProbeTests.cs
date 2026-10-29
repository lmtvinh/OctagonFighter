using System.Collections;
using NUnit.Framework;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Infrastructure.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Diagnoses whether the scripted keyboard reaches the shipped intent source. This exists so a
    /// scenario failure can be attributed to the harness instead of to the combat code.
    /// </summary>
    public sealed class InputProbeTests : CombatSceneFixture
    {
        [UnityTest]
        public IEnumerator VirtualKeyboardReachesThePlayerIntentSource()
        {
            var intentSource = Attacker.GetComponent<PlayerIntentSource>();
            Debug.Log(
                $"[P41] probe keyboard={Keyboard.current?.name ?? "none"} " +
                $"added={ValidationKeyboard.added} enabled={ValidationKeyboard.enabled} " +
                $"keys={ValidationKeyboard.allKeys.Count} " +
                $"updateMode={InputSystem.settings.updateMode} " +
                $"intent={(intentSource != null ? intentSource.GetType().Name : "none")} " +
                $"intentEnabled={(intentSource != null && intentSource.isActiveAndEnabled)} " +
                $"tick={Attacker.Snapshot.Tick} state={Attacker.Snapshot.State} " +
                $"timeDelta={Time.deltaTime:0.0000} fixedDelta={Time.fixedDeltaTime:0.0000}");

            InputSystem.QueueStateEvent(ValidationKeyboard, new KeyboardState(Key.J));
            yield return null;
            Debug.Log(
                $"[P41] probe after J down: j={ValidationKeyboard[Key.J].isPressed} " +
                $"any={ValidationKeyboard.anyKey.isPressed}");

            yield return new WaitForSeconds(0.25f);
            Debug.Log(
                $"[P41] probe after J hold: state={Attacker.Snapshot.State} " +
                $"tick={Attacker.Snapshot.Tick}");

            InputSystem.QueueStateEvent(ValidationKeyboard, new KeyboardState());
            yield return null;
            yield return new WaitForSeconds(0.6f);
            Debug.Log(
                $"[P41] probe after J release: state={Attacker.Snapshot.State} " +
                $"tick={Attacker.Snapshot.Tick}");

            InputSystem.QueueStateEvent(ValidationKeyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.4f);
            Debug.Log(
                $"[P41] probe after D hold: state={Attacker.Snapshot.State} " +
                $"pos={Attacker.transform.position} d={ValidationKeyboard[Key.D].isPressed} " +
                $"tick={Attacker.Snapshot.Tick}");
            InputSystem.QueueStateEvent(ValidationKeyboard, new KeyboardState());
            yield return null;
            yield return new WaitForSeconds(0.25f);

            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The probe must leave the fighter in idle.");
        }
    }
}
