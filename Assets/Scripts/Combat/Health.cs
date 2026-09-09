using System;
using UnityEngine;

namespace Arena.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private float _invulnerabilityDuration = 0.5f;

        private float _lastDamageTime;

        public int MaxHealth => _maxHealth;
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        public event Action<int> Damaged;
        public event Action Died;

        private void Awake()
        {
            Current = _maxHealth;
            _lastDamageTime = Time.time;
        }

        public void Configure(int maxHealth, float invulnerabilityDuration)
        {
            _maxHealth = maxHealth;
            _invulnerabilityDuration = invulnerabilityDuration;
            Current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (Time.time - _lastDamageTime < _invulnerabilityDuration)
            {
                return;
            }

            _lastDamageTime = Time.time;

            int applied = Mathf.Min(amount, Current);
            Current -= applied;
            Damaged?.Invoke(applied);

            if (Current <= 0)
            {
                Died?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            Current = Mathf.Min(_maxHealth, Current + amount);
        }
    }
}
