using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arena.Combat;
using Arena.Waves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena.Tests.PlayMode
{
    public class WaveSpawnerTests : PlayModeFixture
    {
        private sealed class RunTrace
        {
            public WaveSpawner Spawner;
            public readonly List<Health> Enemies = new List<Health>();
            public readonly List<int> Started = new List<int>();
            public readonly List<int> Completed = new List<int>();
            public readonly List<int> Sizes = new List<int>();
            public readonly List<float> SpawnTimes = new List<float>();
            public readonly List<float> StartTimes = new List<float>();
            public readonly List<float> CompleteTimes = new List<float>();
            public int AllCompleted;
            public bool RunningAtAllCompleted;
        }

        private RunTrace CreateRun(int waves = 3, int firstSize = 2, int increment = 1)
        {
            var trace = new RunTrace { Spawner = NewComponent<WaveSpawner>("Spawner under test") };
            WaveSpawner spawner = trace.Spawner;
            spawner.WaveCount = waves;
            spawner.BaseEnemiesPerWave = firstSize;
            spawner.EnemiesIncrementPerWave = increment;
            spawner.SpawnInterval = 0.05f;
            spawner.DelayBetweenWaves = 0.1f;
            spawner.WaveStarted += index =>
            {
                trace.Started.Add(index);
                trace.StartTimes.Add(Time.time);
                trace.Sizes.Add(0);
            };
            spawner.EnemyFactory = () =>
            {
                var enemy = NewComponent<Health>("Enemy " + trace.Enemies.Count);
                enemy.Configure(10, 0f); // Do not couple wave tests to creation-frame invulnerability.
                trace.Enemies.Add(enemy);
                trace.SpawnTimes.Add(Time.time);
                Assert.That(trace.Sizes, Is.Not.Empty, "WaveStarted must precede its factory calls");
                trace.Sizes[trace.Sizes.Count - 1]++;
                return enemy;
            };
            spawner.WaveCompleted += index =>
            {
                trace.Completed.Add(index);
                trace.CompleteTimes.Add(Time.time);
                Assert.That(trace.Enemies.All(enemy => enemy.IsDead), Is.True,
                    "WaveCompleted must not precede enemy deaths");
            };
            spawner.AllWavesCompleted += () =>
            {
                trace.AllCompleted++;
                trace.RunningAtAllCompleted = spawner.IsRunning;
            };
            return trace;
        }

        private static IEnumerator FinishRun(RunTrace trace)
        {
            trace.Spawner.StartRun();
            float deadline = Time.realtimeSinceStartup + 8f;
            while (trace.AllCompleted == 0)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Run did not finish");
                foreach (Health enemy in trace.Enemies)
                    if (!enemy.IsDead) enemy.TakeDamage(enemy.Current);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ControlRun_WaveSizes_AreTwoThreeFour()
        {
            RunTrace trace = CreateRun();
            yield return FinishRun(trace);
            Assert.That(trace.Sizes, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [UnityTest]
        public IEnumerator ControlRun_WaveStarted_EmitsIndicesZeroOneTwo()
        {
            RunTrace trace = CreateRun();
            yield return FinishRun(trace);
            Assert.That(trace.Started, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [UnityTest]
        public IEnumerator ControlRun_WaveCompleted_EmitsIndicesZeroOneTwo()
        {
            RunTrace trace = CreateRun();
            yield return FinishRun(trace);
            Assert.That(trace.Completed, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [UnityTest]
        public IEnumerator ControlRun_CreatesNineEnemies()
        {
            RunTrace trace = CreateRun();
            yield return FinishRun(trace);
            Assert.That(trace.Enemies.Count, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator ControlRun_AllCompletedOnce_AndIsRunningIsFalseAtNotification()
        {
            RunTrace trace = CreateRun();
            yield return FinishRun(trace);
            yield return WaitGameSeconds(0.25f);
            Assert.That(trace.AllCompleted, Is.EqualTo(1));
            Assert.That(trace.RunningAtAllCompleted, Is.False);
            Assert.That(trace.Spawner.IsRunning, Is.False);
            Assert.That(trace.Spawner.AliveCount, Is.Zero);

        }

        [UnityTest]
        public IEnumerator SingleWave_RunActuallySpawnsAndCompletesItsOnlyWave()
        {
            RunTrace trace = CreateRun(waves: 1, firstSize: 1, increment: 0);
            yield return FinishRun(trace);
            Assert.That(trace.Sizes, Is.EqualTo(new[] { 1 }));
            Assert.That(trace.Completed, Is.EqualTo(new[] { 0 }));
        }

        [UnityTest]
        public IEnumerator AliveCount_TracksSpawnAndDeaths_CompletionWaitsForLastEnemy()
        {
            RunTrace trace = CreateRun(waves: 2);
            trace.Spawner.StartRun();
            Assert.That(trace.Spawner.IsRunning, Is.True);
            Assert.That(trace.Spawner.AliveCount, Is.EqualTo(trace.Enemies.Count));
            yield return Until(() => trace.Enemies.Count == 2, "two enemies spawned");
            yield return WaitGameSeconds(0.1f);
            Assert.That(trace.Spawner.AliveCount, Is.EqualTo(2));
            Assert.That(trace.Completed, Is.Empty);
            trace.Enemies[0].TakeDamage(10);
            Assert.That(trace.Spawner.AliveCount, Is.EqualTo(1));
            yield return WaitGameSeconds(0.2f);
            Assert.That(trace.Completed, Is.Empty, "One living enemy still blocks completion");
            trace.Enemies[1].TakeDamage(10);
            Assert.That(trace.Spawner.AliveCount, Is.Zero);
            yield return Until(() => trace.Completed.Count > 0, "completion after final death");
            Assert.That(trace.Completed[0], Is.Zero);
        }

        [UnityTest]
        public IEnumerator SpawnInterval_SeparatesFactoryCallsWithinWave()
        {
            RunTrace trace = CreateRun(waves: 2, firstSize: 3, increment: 0);
            trace.Spawner.StartRun();
            yield return Until(() => trace.Enemies.Count == 3, "all three factory calls");
            for (int i = 1; i < 3; i++)
            {
                float interval = trace.SpawnTimes[i] - trace.SpawnTimes[i - 1];
                Assert.That(interval, Is.GreaterThanOrEqualTo(trace.Spawner.SpawnInterval - 0.001f));
                Assert.That(interval, Is.LessThan(trace.Spawner.SpawnInterval + 2f / 60f + 0.001f));
            }
        }

        [UnityTest]
        public IEnumerator NextWave_StartsOnlyAfterCompletionAndInterWaveDelay()
        {
            RunTrace trace = CreateRun(waves: 3, firstSize: 1, increment: 0);
            trace.Spawner.DelayBetweenWaves = 0.3f;
            yield return FinishRun(trace);
            Assert.That(trace.StartTimes.Count, Is.GreaterThanOrEqualTo(2));
            float delay = trace.StartTimes[1] - trace.CompleteTimes[0];
            Assert.That(delay, Is.GreaterThanOrEqualTo(0.3f - 0.001f));
            Assert.That(delay, Is.LessThan(0.3f + 2f / 60f + 0.001f));
        }
    }
}
