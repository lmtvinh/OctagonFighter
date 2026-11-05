using System.Collections;
using System.Linq;
using NUnit.Framework;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Presentation.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies that the shipped combat HUD presents the guard resource: both bars start full, a
    /// blocked strike drops the defender's guard, a guard break empties the bar, and the post-break
    /// partial guard is shown when the stun ends. Layout assertions keep the new bars in the top HUD
    /// band on the correct side.
    /// </summary>
    public sealed class GuardHudTests : CombatSceneFixture
    {
        private const float CloseDistance = 0.76f;

        [UnityTest]
        public IEnumerator BothGuardBarsStartFullAndBlockedJabDropsTheDefenderBar()
        {
            FighterGuardBar playerBar = FindGuardBar(Attacker);
            FighterGuardBar opponentBar = FindGuardBar(Defender);

            Assert.AreEqual(1f, playerBar.NormalizedGuard, 0.001f, "The player guard bar must start full.");
            Assert.AreEqual(1f, opponentBar.NormalizedGuard, 0.001f, "The opponent guard bar must start full.");
            StringAssert.Contains("100", opponentBar.Label.text);

            PlaceFighters(CloseDistance);
            yield return Settle();
            yield return BlockDefender();

            BeginScenario("hud_guard_blocked_jab");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Assert.AreEqual(1, BlockedHits.Count, "The jab must be blocked.");
            Assert.AreEqual(88.125f, Defender.CurrentGuard, 0.01f);
            Assert.AreEqual(
                0.88125f,
                opponentBar.NormalizedGuard,
                0.001f,
                "The defender guard bar must show the 11.875 guard damage from the jab.");
            StringAssert.Contains("89", opponentBar.Label.text);
            Assert.AreEqual(1f, playerBar.NormalizedGuard, 0.001f, "The attacker guard bar must be untouched.");
        }

        [UnityTest]
        public IEnumerator GuardBreakEmptiesTheBarAndRecoveryRestoresThePartialGuard()
        {
            FighterGuardBar opponentBar = FindGuardBar(Defender);
            PlaceFighters(CloseDistance);
            yield return Settle();
            yield return BlockDefender();

            float healthBefore = Defender.Snapshot.CurrentHealth;
            Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                healthBefore,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;

            Assert.IsTrue(Defender.IsBroken);
            Assert.IsTrue(opponentBar.IsBroken);
            Assert.AreEqual(0f, opponentBar.NormalizedGuard, 0.001f, "A broken guard must show an empty bar.");
            StringAssert.Contains("0", opponentBar.Label.text);

            yield return WaitForDefenderState(FighterStateId.Blocking, 2.0f);

            Assert.IsFalse(Defender.IsBroken);
            Assert.AreEqual(0.35f, opponentBar.NormalizedGuard, 0.001f,
                "The bar must show the 35% post-break guard.");
            StringAssert.Contains("35", opponentBar.Label.text);
        }

        [UnityTest]
        public IEnumerator GuardBarsStayInTheTopHudBandOnTheirOwnSide()
        {
            Canvas canvas = GameObject.Find("CombatHud").GetComponent<Canvas>();
            Canvas.ForceUpdateCanvases();
            yield return null;

            FighterGuardBar playerBar = FindGuardBar(Attacker);
            FighterGuardBar opponentBar = FindGuardBar(Defender);

            AssertTopHud(playerBar.GetComponent<RectTransform>(), canvas, "player guard");
            AssertTopHud(opponentBar.GetComponent<RectTransform>(), canvas, "opponent guard");
            Assert.Greater(
                playerBar.GetComponent<RectTransform>().anchoredPosition.x + 1f,
                0f,
                "Player guard must sit on the left.");
            Assert.Less(
                opponentBar.GetComponent<RectTransform>().anchoredPosition.x,
                0f,
                "Opponent guard must sit on the right.");
        }

        private static FighterGuardBar FindGuardBar(Presentation.Fighter.FighterController fighter)
        {
            FighterGuardBar[] bars = Object.FindObjectsByType<FighterGuardBar>(FindObjectsInactive.Exclude);
            FighterGuardBar bar = bars.FirstOrDefault(candidate => candidate.Fighter == fighter);
            Assert.IsNotNull(bar, $"No guard bar is bound to {fighter.name}.");
            return bar;
        }

        private static void AssertTopHud(RectTransform rect, Canvas canvas, string label)
        {
            Assert.AreEqual(1f, rect.anchorMin.y, 0.001f, $"{label} must be top-anchored.");
            Assert.AreEqual(1f, rect.anchorMax.y, 0.001f, $"{label} must be top-anchored.");
            Assert.Less(rect.anchoredPosition.y, 0f, $"{label} must hang from the top edge.");
            Assert.Greater(rect.anchoredPosition.y, -230f, $"{label} must remain inside the top HUD band.");
        }

        private IEnumerator BlockDefender()
        {
            SetDefenderIntentSource(new HeldBlockIntentSource());
            yield return WaitForDefenderState(FighterStateId.Blocking);
            yield return WaitForDefenderAnimatorState("Blocking");
        }

        private sealed class HeldBlockIntentSource : IFighterIntentSource
        {
            public bool TryGetIntent(TickId tick, out FighterIntent intent)
            {
                intent = new FighterIntent(
                    Float2.Zero,
                    FighterCommand.Block,
                    true,
                    true,
                    MovementGait.Walk,
                    tick);
                return true;
            }
        }
    }
}
