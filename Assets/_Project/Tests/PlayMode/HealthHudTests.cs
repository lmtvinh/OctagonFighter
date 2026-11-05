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
    /// Verifies that the shipped combat HUD reflects the real health and stamina sources: both
    /// fighters start full, a landed strike drops the defender's health, spending drops the
    /// attacker's stamina, a blocked strike chips health instead of leaving it untouched, and
    /// lethal damage empties the health bar. Layout assertions keep both HUDs on-screen at the top
    /// of the Game View.
    /// </summary>
    public sealed class HealthHudTests : CombatSceneFixture
    {
        private const float CloseDistance = 0.76f;

        [UnityTest]
        public IEnumerator BothBarsStartFullAndTrackLandedDamage()
        {
            FighterHealthBar playerBar = FindBar(Attacker);
            FighterHealthBar opponentBar = FindBar(Defender);

            Assert.AreEqual(1f, playerBar.NormalizedHealth, 0.001f, "The player bar must start full.");
            Assert.AreEqual(1f, opponentBar.NormalizedHealth, 0.001f, "The opponent bar must start full.");
            StringAssert.Contains("100", opponentBar.Label.text, "The opponent label must show maximum health.");

            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("hud_damage");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(1.0f);
            yield return WaitForIdle();
            EndScenario();

            Assert.AreEqual(1, DamageEvents.Count, "The jab must remove health exactly once.");
            Assert.AreEqual(92f, Defender.Snapshot.CurrentHealth, 0.01f);
            Assert.AreEqual(0.92f, opponentBar.NormalizedHealth, 0.001f, "The bar must drop to 92%.");
            StringAssert.Contains("92", opponentBar.Label.text);
            Assert.AreEqual(1f, playerBar.NormalizedHealth, 0.001f, "The player's bar must be untouched.");
        }

        // Scenario windows bracket every Sample call: the capture clock only advances between
        // BeginScenario and EndScenario, so a missing pair would spin forever.

        [UnityTest]
        public IEnumerator DamageTrailLagsBehindTheBarAndThenConverges()
        {
            FighterHealthBar opponentBar = FindHealthBar(Defender);
            PlaceFighters(CloseDistance);
            yield return Settle();

            BeginScenario("hud_damage_trail");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(0.45f);
            EndScenario();

            Assert.AreEqual(92f, Defender.Snapshot.CurrentHealth, 0.01f, "The jab must have landed by now.");
            Assert.AreEqual(0.92f, opponentBar.NormalizedHealth, 0.001f, "The bar snaps to the new value.");
            Assert.GreaterOrEqual(
                opponentBar.NormalizedTrail,
                0.97f,
                "The trail must hold the pre-damage value briefly.");

            yield return new WaitForSeconds(2.0f);

            Assert.AreEqual(
                opponentBar.NormalizedHealth,
                opponentBar.NormalizedTrail,
                0.01f,
                "The trail catches up to the bar once the delay elapses.");
        }

        [UnityTest]
        public IEnumerator BlockedStrikeChipsTheHealthBarInsteadOfLeavingItUntouched()
        {
            FighterHealthBar opponentBar = FindBar(Defender);
            PlaceFighters(CloseDistance);
            yield return Settle();
            yield return BlockDefender();
            float fillBefore = opponentBar.NormalizedHealth;
            float healthBefore = Defender.Snapshot.CurrentHealth;

            BeginScenario("hud_blocked");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Assert.AreEqual(1, BlockedHits.Count, "The jab must be reported as blocked.");
            Assert.AreEqual(healthBefore - 2f, Defender.Snapshot.CurrentHealth, 0.01f,
                "A blocked jab must chip 2 health.");
            Assert.AreEqual(0.98f, opponentBar.NormalizedHealth, 0.001f,
                "The bar must show the 2-point chip.");
            StringAssert.Contains("98", opponentBar.Label.text);
            StringAssert.DoesNotContain("92", opponentBar.Label.text);
        }

        [UnityTest]
        public IEnumerator LethalDamageEmptiesTheBar()
        {
            FighterHealthBar opponentBar = FindHealthBar(Defender);
            float healthBefore = Defender.Snapshot.CurrentHealth;

            Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                healthBefore,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;
            yield return null;

            Assert.AreEqual(FighterStateId.Defeated, Defender.Snapshot.State);
            Assert.AreEqual(0f, opponentBar.NormalizedHealth, 0.0001f, "A defeated fighter must show an empty bar.");
            StringAssert.Contains("0", opponentBar.Label.text);
        }

        [UnityTest]
        public IEnumerator BothFightersSpawnWithFullHud()
        {
            FighterHealthBar playerHealth = FindHealthBar(Attacker);
            FighterHealthBar opponentHealth = FindHealthBar(Defender);
            FighterStaminaBar playerStamina = FindStaminaBar(Attacker);
            FighterStaminaBar opponentStamina = FindStaminaBar(Defender);

            Assert.AreEqual(1f, playerHealth.NormalizedHealth, 0.001f, "The player health bar must start full at spawn.");
            Assert.AreEqual(1f, opponentHealth.NormalizedHealth, 0.001f, "The opponent health bar must start full at spawn.");
            Assert.AreEqual(1f, playerStamina.NormalizedStamina, 0.001f, "The player stamina bar must start full at spawn.");
            Assert.AreEqual(1f, opponentStamina.NormalizedStamina, 0.001f, "The opponent stamina bar must start full at spawn.");
            StringAssert.Contains("100", playerHealth.Label.text);
            StringAssert.Contains("100", opponentHealth.Label.text);
            StringAssert.Contains("100", playerStamina.Label.text);
            StringAssert.Contains("100", opponentStamina.Label.text);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerHealthBarUpdatesWhenPlayerIsDamaged()
        {
            FighterHealthBar playerBar = FindHealthBar(Attacker);
            FighterHealthBar opponentBar = FindHealthBar(Defender);

            Attacker.ApplyDamage(new DamageEvent(
                Defender.Id,
                Attacker.Id,
                8f,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;

            Assert.AreEqual(92f, Attacker.Snapshot.CurrentHealth, 0.01f);
            Assert.AreEqual(
                0.92f,
                playerBar.NormalizedHealth,
                0.001f,
                "The player health bar must track damage applied to the player.");
            StringAssert.Contains("92", playerBar.Label.text);
            Assert.AreEqual(1f, opponentBar.NormalizedHealth, 0.001f, "The opponent health bar must be untouched.");
        }

        [UnityTest]
        public IEnumerator DestroyedDefenderLeavesNoStaleBars()
        {
            FighterHealthBar healthBar = FindHealthBar(Defender);
            FighterStaminaBar staminaBar = FindStaminaBar(Defender);
            string labelBeforeDestroy = healthBar.Label.text;

            Object.Destroy(Defender.gameObject);
            yield return null;
            yield return null;

            healthBar.gameObject.SetActive(false);
            staminaBar.gameObject.SetActive(false);
            yield return null;
            healthBar.gameObject.SetActive(true);
            staminaBar.gameObject.SetActive(true);
            yield return null;

            Assert.IsNotNull(healthBar.Fill, "The health bar must survive the fighter's destruction.");
            Assert.IsNotNull(staminaBar.Fill, "The stamina bar must survive the fighter's destruction.");
            Assert.AreEqual(
                labelBeforeDestroy,
                healthBar.Label.text,
                "A destroyed fighter must not refresh the bar with stale data or throw.");
        }

        [UnityTest]
        public IEnumerator BothStaminaBarsStartFullAndTrackJabSpend()
        {
            FighterStaminaBar playerBar = FindStaminaBar(Attacker);
            FighterStaminaBar opponentBar = FindStaminaBar(Defender);

            Assert.AreEqual(1f, playerBar.NormalizedStamina, 0.001f, "The player stamina bar must start full.");
            Assert.AreEqual(1f, opponentBar.NormalizedStamina, 0.001f, "The opponent stamina bar must start full.");
            Assert.AreEqual(100f, Attacker.Stamina.CurrentStamina, 0.01f);
            Assert.AreEqual(100f, Defender.Stamina.CurrentStamina, 0.01f);

            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("hud_stamina_jab");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(1.0f);
            yield return WaitForIdle();
            EndScenario();

            Assert.AreEqual(94f, Attacker.Stamina.CurrentStamina, 0.01f, "A jab must spend 6 stamina.");
            Assert.AreEqual(0.94f, playerBar.NormalizedStamina, 0.001f, "The player stamina bar must drop to 94%.");
            StringAssert.Contains("94", playerBar.Label.text);
            Assert.AreEqual(1f, opponentBar.NormalizedStamina, 0.001f, "The opponent stamina bar must be untouched.");
        }

        [UnityTest]
        public IEnumerator CrossSpendsAttackerStaminaEvenWhenBlocked()
        {
            FighterStaminaBar playerBar = FindStaminaBar(Attacker);
            FighterHealthBar opponentHealth = FindHealthBar(Defender);
            PlaceFighters(CloseDistance);
            yield return Settle();
            yield return BlockDefender();
            float healthFillBefore = opponentHealth.NormalizedHealth;

            BeginScenario("hud_stamina_blocked_cross");
            yield return Press(UnityEngine.InputSystem.Key.K);
            yield return Sample(1.4f);
            EndScenario();

            Assert.AreEqual(1, BlockedHits.Count, "The cross must be reported as blocked.");
            Assert.AreEqual(healthFillBefore - 0.0375f, opponentHealth.NormalizedHealth, 0.001f,
                "A blocked cross must chip 3.75 health.");
            Assert.AreEqual(88f, Attacker.Stamina.CurrentStamina, 0.01f, "A blocked cross must still spend 12 stamina.");
            Assert.AreEqual(0.88f, playerBar.NormalizedStamina, 0.001f);
        }

        [UnityTest]
        public IEnumerator StaminaBarTracksRegenerationAfterTheDelay()
        {
            FighterStaminaBar playerBar = FindStaminaBar(Attacker);
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("hud_stamina_regen");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(2.6f);
            EndScenario();

            Assert.Greater(
                Attacker.Stamina.CurrentStamina,
                94f,
                "Stamina must start regenerating after the authored 2 second delay.");
            Assert.Greater(playerBar.NormalizedStamina, 0.94f);
            Assert.Less(playerBar.NormalizedStamina, 1f);
        }

        [UnityTest]
        public IEnumerator PlayerAndOpponentHudStayOnTheTopEdge()
        {
            Canvas canvas = GameObject.Find("CombatHud").GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Canvas.ForceUpdateCanvases();
            yield return null;

            FighterHealthBar playerHealth = FindHealthBar(Attacker);
            FighterHealthBar opponentHealth = FindHealthBar(Defender);
            FighterStaminaBar playerStamina = FindStaminaBar(Attacker);
            FighterStaminaBar opponentStamina = FindStaminaBar(Defender);

            AssertTopHud(playerHealth.GetComponent<RectTransform>(), canvas, "player health");
            AssertTopHud(opponentHealth.GetComponent<RectTransform>(), canvas, "opponent health");
            AssertTopHud(playerStamina.GetComponent<RectTransform>(), canvas, "player stamina");
            AssertTopHud(opponentStamina.GetComponent<RectTransform>(), canvas, "opponent stamina");
            Assert.Greater(
                playerHealth.GetComponent<RectTransform>().anchoredPosition.x + 1f,
                0f,
                "Player HUD must sit on the left.");
            Assert.Less(
                opponentHealth.GetComponent<RectTransform>().anchoredPosition.x,
                0f,
                "Opponent HUD must sit on the right.");
        }

        [UnityTest]
        public IEnumerator DisabledHealthBarDoesNotKeepAStaleSubscription()
        {
            FighterHealthBar opponentBar = FindHealthBar(Defender);
            opponentBar.gameObject.SetActive(false);
            yield return null;

            float healthBefore = Defender.Snapshot.CurrentHealth;
            Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                8f,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;

            opponentBar.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(
                healthBefore - 8f,
                Defender.Snapshot.CurrentHealth,
                0.01f,
                "Disabling the bar must not stop the health model.");
            Assert.AreEqual(
                (healthBefore - 8f) / Defender.Health.MaxHealth,
                opponentBar.NormalizedHealth,
                0.001f,
                "Re-enabling the bar must refresh from the live health source.");
        }

        private static FighterHealthBar FindBar(Presentation.Fighter.FighterController fighter)
        {
            return FindHealthBar(fighter);
        }

        private static FighterHealthBar FindHealthBar(Presentation.Fighter.FighterController fighter)
        {
            FighterHealthBar[] bars = Object.FindObjectsByType<FighterHealthBar>(FindObjectsInactive.Exclude);
            FighterHealthBar bar = bars.FirstOrDefault(candidate => candidate.Fighter == fighter);
            Assert.IsNotNull(bar, $"No health bar is bound to {fighter.name}.");
            return bar;
        }

        private static FighterStaminaBar FindStaminaBar(Presentation.Fighter.FighterController fighter)
        {
            FighterStaminaBar[] bars = Object.FindObjectsByType<FighterStaminaBar>(FindObjectsInactive.Exclude);
            FighterStaminaBar bar = bars.FirstOrDefault(candidate => candidate.Fighter == fighter);
            Assert.IsNotNull(bar, $"No stamina bar is bound to {fighter.name}.");
            return bar;
        }

        private static void AssertTopHud(RectTransform rect, Canvas canvas, string label)
        {
            Assert.AreEqual(1f, rect.anchorMin.y, 0.001f, $"{label} must be top-anchored.");
            Assert.AreEqual(1f, rect.anchorMax.y, 0.001f, $"{label} must be top-anchored.");
            Assert.Less(rect.anchoredPosition.y, 0f, $"{label} must hang from the top edge.");
            Assert.Greater(rect.anchoredPosition.y, -160f, $"{label} must remain inside the top HUD band.");
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
