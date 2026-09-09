using System.Collections;
using System.Collections.Generic;
using Arena.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena.Tests.PlayMode
{
    public class HealthTests : PlayModeFixture
    {
        private Health Unit(int hp = 100, float invulnerability = 0f)
        {
            var unit = NewComponent<Health>("Health under test");
            unit.Configure(hp, invulnerability);
            return unit;
        }

        [Test]
        public void Configure_SetsMaxAndCurrentHealth_UnitIsAlive()
        {
            Health unit = Unit(37);
            Assert.That(unit.MaxHealth, Is.EqualTo(37));
            Assert.That(unit.Current, Is.EqualTo(37));
            Assert.That(unit.IsDead, Is.False);

        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void TakeDamage_NonPositive_DoesNotChangeHealthOrEmitEvents(int amount)
        {
            Health unit = Unit();
            int damaged = 0, died = 0;
            unit.Damaged += _ => damaged++;
            unit.Died += () => died++;
            unit.TakeDamage(amount);
            Assert.That(unit.Current, Is.EqualTo(100));
            Assert.That(damaged, Is.Zero);
            Assert.That(died, Is.Zero);

        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(100000f)]
        public void TakeDamage_NewUnitInCreationFrame_IsImmediatelyVulnerable(float duration)
        {
            Health unit = Unit(100, duration);
            var events = new List<int>();
            unit.Damaged += events.Add;
            unit.TakeDamage(10);
            Assert.That(unit.Current, Is.EqualTo(90));
            Assert.That(events, Is.EqualTo(new[] { 10 }));

        }

        [Test]
        public void TakeDamage_AlreadyDead_IsCompletelyIgnored()
        {
            Health unit = Unit(3);
            unit.TakeDamage(3);
            int damaged = 0, died = 0;
            unit.Damaged += _ => damaged++;
            unit.Died += () => died++;
            unit.TakeDamage(10);
            Assert.That(unit.Current, Is.Zero);
            Assert.That(damaged, Is.Zero);
            Assert.That(died, Is.Zero);

        }

        [TestCase(3)]
        [TestCase(10)]
        [TestCase(int.MaxValue)]
        public void TakeDamage_Lethal_ClampsToZeroAndReportsOnlyRemainingHp(int amount)
        {
            Health unit = Unit(3);
            var damageEvents = new List<int>();
            int deaths = 0;
            unit.Damaged += damageEvents.Add;
            unit.Died += () =>
            {
                deaths++;
                Assert.That(unit.Current, Is.Zero, "Died must observe the death state");
                Assert.That(unit.IsDead, Is.True);
            };
            unit.TakeDamage(amount);
            Assert.That(unit.Current, Is.Zero);
            Assert.That(unit.IsDead, Is.True);
            Assert.That(damageEvents, Is.EqualTo(new[] { 3 }));
            Assert.That(deaths, Is.EqualTo(1));

        }

        [Test]
        public void TakeDamage_MultipleHitsAfterDeath_EmitsDiedExactlyOnce()
        {
            Health unit = Unit(5);
            int deaths = 0;
            unit.Died += () => deaths++;
            unit.TakeDamage(2);
            Assert.That(deaths, Is.Zero);
            unit.TakeDamage(3);
            unit.TakeDamage(1);
            unit.TakeDamage(1);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TakeDamage_InsideWindow_IgnoresDamageAndEvents_ThenAcceptsAfterExpiry()
        {
            Health unit = Unit(100, 0.5f);
            // Separates §2.3 from the independently tested creation-frame requirement §2.4.
            yield return WaitGameSeconds(0.55f);
            var events = new List<int>();
            int deaths = 0;
            unit.Damaged += events.Add;
            unit.Died += () => deaths++;
            unit.TakeDamage(10);
            unit.TakeDamage(99);
            Assert.That(unit.Current, Is.EqualTo(90));
            Assert.That(events, Is.EqualTo(new[] { 10 }));
            Assert.That(deaths, Is.Zero);

            yield return WaitGameSeconds(0.55f);
            unit.TakeDamage(20);
            Assert.That(unit.Current, Is.EqualTo(70));
            Assert.That(events, Is.EqualTo(new[] { 10, 20 }));
        }

        [UnityTest]
        public IEnumerator TakeDamage_IgnoredHitDoesNotRestartWindow_AcceptedHitDoes()
        {
            Health unit = Unit(100, 0.5f);
            yield return WaitGameSeconds(0.55f);
            unit.TakeDamage(10);
            yield return WaitGameSeconds(0.30f);
            unit.TakeDamage(30); // Ignored; must not move the original deadline.
            yield return WaitGameSeconds(0.25f);
            unit.TakeDamage(10); // > 0.5 from accepted hit, < 0.5 from ignored hit.
            Assert.That(unit.Current, Is.EqualTo(80));
            yield return WaitGameSeconds(0.20f);
            unit.TakeDamage(10); // Must respect the new window from the second accepted hit.
            Assert.That(unit.Current, Is.EqualTo(80));
            yield return WaitGameSeconds(0.35f);
            unit.TakeDamage(10);
            Assert.That(unit.Current, Is.EqualTo(70));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void Heal_NonPositive_IsIgnored(int amount)
        {
            Health unit = Unit();
            unit.TakeDamage(20);
            unit.Heal(amount);
            Assert.That(unit.Current, Is.EqualTo(80));
        }

        [Test]
        public void Heal_DeadUnit_DoesNotResurrect()
        {
            Health unit = Unit(3);
            unit.TakeDamage(3);
            unit.Heal(100);
            Assert.That(unit.Current, Is.Zero);
            Assert.That(unit.IsDead, Is.True);
        }

        [TestCase(10, 90)]
        [TestCase(20, 100)]
        [TestCase(50, 100)]
        [TestCase(int.MaxValue, 100)]
        public void Heal_PositiveAmount_RestoresHpUpToMaximum(int amount, int expected)
        {
            Health unit = Unit();
            unit.TakeDamage(20);
            unit.Heal(amount);
            Assert.That(unit.Current, Is.EqualTo(expected));
            Assert.That(unit.IsDead, Is.False);
        }

        [UnityTest]
        public IEnumerator Heal_DuringInvulnerability_DoesNotShortenOrExtendWindow()
        {
            Health unit = Unit(100, 0.5f);
            yield return WaitGameSeconds(0.55f);
            unit.TakeDamage(20);
            yield return WaitGameSeconds(0.30f);
            unit.Heal(5);
            unit.TakeDamage(10);
            Assert.That(unit.Current, Is.EqualTo(85), "Healing must not cancel protection");
            yield return WaitGameSeconds(0.25f);
            unit.TakeDamage(10);
            Assert.That(unit.Current, Is.EqualTo(75), "Healing must not restart protection");
        }
    }
}
