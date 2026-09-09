using System;
using System.Collections;
using System.Collections.Generic;
using Arena.Combat;
using UnityEngine;

namespace Arena.Waves
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private int _waveCount = 3;
        [SerializeField] private int _baseEnemiesPerWave = 2;
        [SerializeField] private int _enemiesIncrementPerWave = 1;
        [SerializeField] private float _spawnInterval = 0.1f;
        [SerializeField] private float _delayBetweenWaves = 0.2f;

        private readonly List<Health> _alive = new List<Health>();

        public Func<Health> EnemyFactory;

        public int WaveCount
        {
            get => _waveCount;
            set => _waveCount = value;
        }

        public int BaseEnemiesPerWave
        {
            get => _baseEnemiesPerWave;
            set => _baseEnemiesPerWave = value;
        }

        public int EnemiesIncrementPerWave
        {
            get => _enemiesIncrementPerWave;
            set => _enemiesIncrementPerWave = value;
        }

        public float SpawnInterval
        {
            get => _spawnInterval;
            set => _spawnInterval = value;
        }

        public float DelayBetweenWaves
        {
            get => _delayBetweenWaves;
            set => _delayBetweenWaves = value;
        }

        public int AliveCount => _alive.Count;
        public bool IsRunning { get; private set; }

        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action AllWavesCompleted;

        public void StartRun()
        {
            if (IsRunning)
            {
                return;
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            IsRunning = true;

            for (int wave = 0; wave < _waveCount - 1; wave++)
            {
                WaveStarted?.Invoke(wave);

                int count = _baseEnemiesPerWave + wave * _enemiesIncrementPerWave;

                for (int i = 0; i < count; i++)
                {
                    Health enemy = EnemyFactory();
                    _alive.Add(enemy);
                    enemy.Died += () => _alive.Remove(enemy);

                    yield return new WaitForSeconds(_spawnInterval);
                }

                while (_alive.Count > 0)
                {
                    yield return null;
                }

                WaveCompleted?.Invoke(wave);

                yield return new WaitForSeconds(_delayBetweenWaves);
            }

            IsRunning = false;
            AllWavesCompleted?.Invoke();
        }
    }
}
