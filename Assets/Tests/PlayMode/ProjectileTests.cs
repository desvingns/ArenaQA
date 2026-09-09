using System.Collections;
using Arena.Combat;
using Arena.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena.Tests.PlayMode
{
    public class ProjectileTests : PlayModeFixture
    {
        private Projectile Shot(float lifetime = 10f)
        {
            var shot = NewComponent<Projectile>("Projectile under test");
            shot.Speed = 0f;
            shot.Lifetime = lifetime;
            shot.Damage = 17;
            shot.OnSpawn();
            return shot;
        }

        [UnityTest]
        public IEnumerator Flight_RotatedProjectile_MovesAlongLocalZAtSpeedAndAccumulatesTime()
        {
            Projectile shot = Shot();
            shot.Speed = 6f;
            shot.transform.SetPositionAndRotation(new Vector3(2f, 3f, 4f), Quaternion.Euler(0f, 90f, 0f));
            yield return null; // Align snapshots after initialization; no private Update invocation.
            float startTime = Time.time;
            float startElapsed = shot.Elapsed;
            Vector3 startPosition = shot.transform.position;
            Vector3 direction = shot.transform.forward;
            for (int frame = 0; frame < 6; frame++)
            {
                yield return null;
                float duration = Time.time - startTime;
                Assert.That(shot.Elapsed - startElapsed, Is.EqualTo(duration).Within(0.001f));
                Vector3 expected = startPosition + direction * (6f * duration);
                Assert.That(Vector3.Distance(shot.transform.position, expected), Is.LessThan(0.01f));
            }
        }

        [UnityTest]
        public IEnumerator Lifetime_ExpiresAtFirstFrameReachingLimit_NotBefore()
        {
            Projectile shot = Shot(0.2f);
            int expirations = 0;
            float elapsedAtExpiry = -1f;
            shot.Expired += expired =>
            {
                Assert.That(expired, Is.SameAs(shot));
                expirations++;
                elapsedAtExpiry = expired.Elapsed;
                expired.gameObject.SetActive(false); // Owner consumes the first expiration.
            };
            yield return Until(() => expirations > 0, "projectile lifetime expiration");
            Assert.That(elapsedAtExpiry, Is.GreaterThanOrEqualTo(shot.Lifetime));
            Assert.That(elapsedAtExpiry, Is.LessThan(shot.Lifetime + 1f / 60f + 0.001f));
            // No assertion about repeated Expired on an active expired object: §5.2 leaves it unspecified.
        }

        [Test]
        public void HandleHit_Damageable_ReceivesConfiguredDamageAndSetsHasHit()
        {
            Projectile shot = Shot();
            var target = NewComponent<DamageableProbe>("Target");
            shot.HandleHit(target.gameObject);
            Assert.That(target.ReceivedDamage, Is.EqualTo(new[] { 17 }));
            Assert.That(shot.HasHit, Is.True);
        }

        [Test]
        public void HandleHit_FirstDamageable_EmitsExpiredSynchronously()
        {
            Projectile shot = Shot();
            var target = NewComponent<DamageableProbe>("Target");
            int expirations = 0;
            shot.Expired += expired =>
            {
                expirations++;
                Assert.That(expired, Is.SameAs(shot));
                Assert.That(expired.HasHit, Is.True);
                Assert.That(target.ReceivedDamage, Is.EqualTo(new[] { 17 }));
            };
            shot.HandleHit(target.gameObject);
            Assert.That(expirations, Is.EqualTo(1), "Expired must occur before HandleHit returns");
        }

        [Test]
        public void HandleHit_RepeatedSameTarget_AppliesDamageOnlyOnce()
        {
            Projectile shot = Shot();
            var target = NewComponent<DamageableProbe>("Target");
            shot.HandleHit(target.gameObject);
            shot.HandleHit(target.gameObject);
            Assert.That(target.ReceivedDamage, Is.EqualTo(new[] { 17 }));
        }

        [Test]
        public void HandleHit_SecondTargetInSameFlight_DoesNotDamageSecondTarget()
        {
            Projectile shot = Shot();
            var first = NewComponent<DamageableProbe>("First target");
            var second = NewComponent<DamageableProbe>("Second target");
            shot.HandleHit(first.gameObject);
            shot.HandleHit(second.gameObject);
            Assert.That(first.ReceivedDamage, Is.EqualTo(new[] { 17 }));
            Assert.That(second.ReceivedDamage, Is.Empty);
        }

        [Test]
        public void HandleHit_NonDamageable_IsIgnoredAndDoesNotConsumeFlight()
        {
            Projectile shot = Shot();
            int expirations = 0;
            shot.Expired += _ => expirations++;
            shot.HandleHit(NewObject("Decoration without IDamageable"));
            Assert.That(shot.HasHit, Is.False);
            Assert.That(expirations, Is.Zero);
            var target = NewComponent<DamageableProbe>("Valid target");
            shot.HandleHit(target.gameObject);
            Assert.That(target.ReceivedDamage, Is.EqualTo(new[] { 17 }));
        }

        [UnityTest]
        public IEnumerator Pool_ReusedProjectile_ResetsElapsedToZero()
        {
            var pool = new SimplePool<Projectile>(() => Shot());
            Projectile first = pool.Get();
            yield return Until(() => first.Elapsed >= 0.1f, "accumulating first-flight time");
            pool.Release(first);
            Projectile reused = pool.Get();
            Assert.That(reused, Is.SameAs(first), "Must verify reuse, not a new factory instance");
            Assert.That(pool.CreatedCount, Is.EqualTo(1));
            Assert.That(reused.Elapsed, Is.Zero);
        }

        [Test]
        public void Pool_ReusedProjectile_ResetsHasHitAndCanDamageAgain()
        {
            var pool = new SimplePool<Projectile>(() => Shot());
            Projectile first = pool.Get();
            var target = NewComponent<DamageableProbe>("Target");
            first.HandleHit(target.gameObject);
            Assert.That(first.HasHit, Is.True);
            pool.Release(first);
            Projectile reused = pool.Get();
            Assert.That(reused, Is.SameAs(first));
            Assert.That(reused.HasHit, Is.False);
            reused.HandleHit(target.gameObject);
            Assert.That(target.ReceivedDamage, Is.EqualTo(new[] { 17, 17 }));
        }

        [UnityTest]
        public IEnumerator Pool_ThreeFlights_EachReceivesFullLifetime()
        {
            var pool = new SimplePool<Projectile>(() => Shot(0.2f));
            Projectile shot = pool.Get();
            int expirations = 0;
            shot.Expired += item => { expirations++; pool.Release(item); };
            for (int flight = 0; flight < 3; flight++)
            {
                if (flight > 0) Assert.That(pool.Get(), Is.SameAs(shot));
                float started = Time.time;
                yield return Until(() => expirations == flight + 1, "expiration of flight " + flight);
                float lived = Time.time - started;
                Assert.That(lived, Is.GreaterThanOrEqualTo(shot.Lifetime - 0.001f),
                    "Flight " + flight + " must receive its own complete lifetime");
                Assert.That(lived, Is.LessThan(shot.Lifetime + 2f / 60f + 0.001f));
            }
            Assert.That(pool.CreatedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PhysicsTrigger_DamageableCollider_DelegatesHitToDamageable()
        {
            Projectile shot = Shot();
            shot.transform.position = new Vector3(1000f, 1000f, 1000f);
            shot.gameObject.AddComponent<SphereCollider>().isTrigger = true;
            var body = shot.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var target = NewComponent<DamageableProbe>("Physics target");
            target.transform.position = shot.transform.position;
            target.gameObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            yield return Until(() => target.ReceivedDamage.Count > 0, "OnTriggerEnter damage");
            Assert.That(target.ReceivedDamage, Is.EqualTo(new[] { 17 }));
            Assert.That(shot.HasHit, Is.True);
        }
    }
}
