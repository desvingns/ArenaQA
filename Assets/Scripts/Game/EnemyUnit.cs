using UnityEngine;

namespace Arena.Game
{
    public class EnemyUnit : MonoBehaviour
    {
        public string Archetype = "grunt";
        public int Armor;
        public float MoveSpeed = 1.6f;
        public float StopRadius = 2.6f;
        public Transform Target;

        private Renderer _renderer;
        private bool _dying;
        private float _deathTimer;

        public bool IsDying => _dying;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        public void Paint(Color color)
        {
            if (_renderer != null)
            {
                _renderer.material.color = color;
            }
        }

        public void BeginDeath()
        {
            if (_dying)
            {
                return;
            }

            _dying = true;
            _deathTimer = 0f;

            if (_renderer != null)
            {
                _renderer.material.color = new Color(0.25f, 0.25f, 0.28f);
            }
        }

        private void Update()
        {
            if (_dying)
            {
                _deathTimer += Time.deltaTime;
                float k = Mathf.Clamp01(1f - _deathTimer / 0.7f);
                transform.localScale = new Vector3(k, k, k);

                if (_deathTimer >= 0.7f)
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (Target == null)
            {
                return;
            }

            Vector3 toTarget = Target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude > StopRadius)
            {
                transform.position += toTarget.normalized * (MoveSpeed * Time.deltaTime);
            }
        }
    }
}
