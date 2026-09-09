using System;
using Arena.Pooling;
using UnityEngine;

namespace Arena.Combat
{
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private int _damage = 10;

        private float _elapsed;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public float Lifetime
        {
            get => _lifetime;
            set => _lifetime = value;
        }

        public int Damage
        {
            get => _damage;
            set => _damage = value;
        }

        public float Elapsed => _elapsed;

        public bool HasHit { get; private set; }

        public event Action<Projectile> Expired;

        public void OnSpawn()
        {
            HasHit = false;
        }

        public void OnDespawn()
        {
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);

            _elapsed += Time.deltaTime;

            if (_elapsed >= _lifetime)
            {
                Expired?.Invoke(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject);
        }

        public void HandleHit(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            damageable.TakeDamage(_damage);
            HasHit = true;
        }
    }
}
