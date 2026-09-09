using System.Collections.Generic;
using Arena.Combat;
using Arena.Economy;
using Arena.Pooling;
using Arena.Waves;
using UnityEngine;

namespace Arena.Game
{
    public class ArenaGame : MonoBehaviour
    {
        private struct Archetype
        {
            public string Name;
            public int MaxHealth;
            public int Armor;
            public float Invulnerability;
            public Color Color;
        }

        [SerializeField] private float _spawnRadius = 12f;
        [SerializeField] private float _fireInterval = 0.35f;
        [SerializeField] private int _turretBaseDamage = 12;
        [SerializeField] private float _critChance = 0.25f;
        [SerializeField] private float _critMultiplier = 2f;
        [SerializeField] private float _projectileSpeed = 25f;
        [SerializeField] private float _projectileLifetime = 120f;
        [SerializeField] private float _despawnDistance = 26f;

        private static readonly Archetype[] Archetypes =
        {
            new Archetype { Name = "grunt", MaxHealth = 30, Armor = 0, Invulnerability = 0.4f, Color = new Color(0.85f, 0.30f, 0.30f) },
            new Archetype { Name = "grunt", MaxHealth = 30, Armor = 0, Invulnerability = 0.4f, Color = new Color(0.85f, 0.30f, 0.30f) },
            new Archetype { Name = "armored", MaxHealth = 6, Armor = 2000, Invulnerability = 0.4f, Color = new Color(0.45f, 0.55f, 0.85f) },
            new Archetype { Name = "breached", MaxHealth = 40, Armor = -50, Invulnerability = 0.4f, Color = new Color(0.90f, 0.65f, 0.25f) }
        };

        private WaveSpawner _spawner;
        private SimplePool<Projectile> _pool;
        private LootTable _lootTable;

        private Transform _turretHead;
        private Transform _muzzle;

        private readonly List<EnemyUnit> _units = new List<EnemyUnit>();
        private readonly List<Projectile> _activeProjectiles = new List<Projectile>();
        private readonly List<Projectile> _recycleBuffer = new List<Projectile>();
        private readonly List<string> _log = new List<string>();

        private float _fireTimer;
        private int _spawnIndex;
        private int _kills;
        private int _currentWave = -1;
        private string _status = "Запуск";
        private string _lastShot = "—";
        private Texture2D _bar;

        private void Start()
        {
            SetupCamera();
            BuildArena();

            _bar = Texture2D.whiteTexture;

            _lootTable = new LootTable(new[]
            {
                new LootEntry("nothing", 50),
                new LootEntry("scrap", 30),
                new LootEntry("ammo", 15),
                new LootEntry("rare_core", 5)
            });

            _pool = new SimplePool<Projectile>(CreateProjectile);

            _spawner = gameObject.AddComponent<WaveSpawner>();
            _spawner.EnemyFactory = CreateEnemy;
            _spawner.WaveStarted += OnWaveStarted;
            _spawner.WaveCompleted += OnWaveCompleted;
            _spawner.AllWavesCompleted += OnAllWavesCompleted;
            _spawner.StartRun();
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.transform.position = new Vector3(0f, 17f, -15f);
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
        }

        private void BuildArena()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(transform);
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(34f, 0.5f, 34f);
            Paint(ground, new Color(0.16f, 0.17f, 0.20f));

            GameObject baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseCyl.name = "TurretBase";
            baseCyl.transform.SetParent(transform);
            baseCyl.transform.position = new Vector3(0f, 0.3f, 0f);
            baseCyl.transform.localScale = new Vector3(1.6f, 0.3f, 1.6f);
            Paint(baseCyl, new Color(0.35f, 0.75f, 0.55f));

            GameObject head = new GameObject("TurretHead");
            head.transform.SetParent(transform);
            head.transform.position = new Vector3(0f, 0.8f, 0f);
            _turretHead = head.transform;

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            barrel.transform.SetParent(head.transform);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.9f);
            barrel.transform.localScale = new Vector3(0.35f, 0.35f, 1.8f);
            Paint(barrel, new Color(0.55f, 0.90f, 0.70f));
            Destroy(barrel.GetComponent<Collider>());

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(head.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 1.9f);
            _muzzle = muzzle.transform;
        }

        private static void Paint(GameObject go, Color color)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = color;
            }
        }

        private Health CreateEnemy()
        {
            Archetype type = Archetypes[_spawnIndex % Archetypes.Length];
            _spawnIndex++;

            float angle = _spawnIndex * 47f * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * _spawnRadius;
            position.y = 1f;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{type.Name}_{_spawnIndex}";
            go.transform.SetParent(transform);
            go.transform.position = position;

            Health health = go.AddComponent<Health>();
            health.Configure(type.MaxHealth, type.Invulnerability);

            EnemyUnit unit = go.AddComponent<EnemyUnit>();
            unit.Archetype = type.Name;
            unit.Armor = type.Armor;
            unit.Target = _turretHead;
            unit.Paint(type.Color);

            health.Died += () => OnEnemyDied(unit);

            _units.Add(unit);
            return health;
        }

        private Projectile CreateProjectile()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Paint(go, new Color(1f, 0.92f, 0.45f));

            SphereCollider collider = go.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            Projectile projectile = go.AddComponent<Projectile>();
            projectile.Expired += OnProjectileExpired;
            return projectile;
        }

        private void Update()
        {
            if (_spawner == null)
            {
                return;
            }

            _units.RemoveAll(u => u == null);

            AimAndFire();
            RecycleFarProjectiles();
        }

        private void AimAndFire()
        {
            EnemyUnit target = FindTarget();

            if (target != null)
            {
                Vector3 aim = target.transform.position - _turretHead.position;
                aim.y = 0f;
                if (aim.sqrMagnitude > 0.001f)
                {
                    _turretHead.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
                }
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer > 0f || target == null)
            {
                return;
            }

            _fireTimer = _fireInterval;

            bool isCrit = Random.value < _critChance;
            var request = new DamageRequest
            {
                BaseDamage = _turretBaseDamage,
                BonusPercent = 0f,
                Armor = target.Armor,
                IsCritical = isCrit,
                CriticalMultiplier = _critMultiplier
            };

            int damage = DamageCalculator.Calculate(request);
            _lastShot = $"{target.Archetype} (броня {target.Armor}){(isCrit ? ", крит" : "")} → урон {damage}";

            Projectile projectile = _pool.Get();
            projectile.transform.position = _muzzle.position;
            projectile.transform.rotation = _turretHead.rotation;
            projectile.Speed = _projectileSpeed;
            projectile.Lifetime = _projectileLifetime;
            projectile.Damage = damage;
            _activeProjectiles.Add(projectile);
        }

        private EnemyUnit FindTarget()
        {
            EnemyUnit best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _units.Count; i++)
            {
                EnemyUnit unit = _units[i];
                if (unit == null || unit.IsDying)
                {
                    continue;
                }

                float distance = (unit.transform.position - _turretHead.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = unit;
                }
            }

            return best;
        }

        private void RecycleFarProjectiles()
        {
            _recycleBuffer.Clear();

            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                Projectile projectile = _activeProjectiles[i];
                if (projectile == null || projectile.transform.position.sqrMagnitude > _despawnDistance * _despawnDistance)
                {
                    _recycleBuffer.Add(projectile);
                }
            }

            for (int i = 0; i < _recycleBuffer.Count; i++)
            {
                Release(_recycleBuffer[i]);
            }
        }

        private void OnProjectileExpired(Projectile projectile)
        {
            Release(projectile);
        }

        private void Release(Projectile projectile)
        {
            if (projectile == null)
            {
                _activeProjectiles.RemoveAll(p => p == null);
                return;
            }

            if (!_activeProjectiles.Remove(projectile))
            {
                return;
            }

            _pool.Release(projectile);
        }

        private void OnEnemyDied(EnemyUnit unit)
        {
            _kills++;

            int roll = Random.Range(0, _lootTable.TotalWeight);
            string loot = _lootTable.Pick(roll);

            AddLog($"убит {unit.Archetype}, roll={roll}, лут: {loot}");
            unit.BeginDeath();
        }

        private void OnWaveStarted(int wave)
        {
            _currentWave = wave;
            _status = $"идёт волна {wave}";
            AddLog($"волна {wave} началась");
        }

        private void OnWaveCompleted(int wave)
        {
            _status = $"волна {wave} зачищена";
            AddLog($"волна {wave} зачищена");
        }

        private void OnAllWavesCompleted()
        {
            _status = "все волны пройдены";
            AddLog("все волны пройдены");
        }

        private void AddLog(string line)
        {
            _log.Add(line);
            if (_log.Count > 7)
            {
                _log.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            if (_spawner == null || _pool == null)
            {
                return;
            }

            GUI.skin.label.fontSize = 14;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(12f, 12f, 380f, 168f), _bar);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(24f, 20f, 360f, 156f));
            GUILayout.Label($"Волна: {(_currentWave < 0 ? "—" : (_currentWave + 1).ToString())} из {_spawner.WaveCount}    Статус: {_status}");
            GUILayout.Label($"Живых на арене: {_spawner.AliveCount}    Убито всего: {_kills}");
            GUILayout.Label($"Врагов создано: {_spawnIndex}    Снарядов в пуле: {_pool.CreatedCount}");
            GUILayout.Label($"Последний выстрел: {_lastShot}");
            GUILayout.Space(4f);

            for (int i = 0; i < _log.Count; i++)
            {
                GUILayout.Label(_log[i]);
            }

            GUILayout.EndArea();

            DrawHealthBars();
        }

        private void DrawHealthBars()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            for (int i = 0; i < _units.Count; i++)
            {
                EnemyUnit unit = _units[i];
                if (unit == null || unit.IsDying)
                {
                    continue;
                }

                Health health = unit.GetComponent<Health>();
                if (health == null)
                {
                    continue;
                }

                Vector3 screen = cam.WorldToScreenPoint(unit.transform.position + Vector3.up * 1.4f);
                if (screen.z <= 0f)
                {
                    continue;
                }

                float x = screen.x - 26f;
                float y = Screen.height - screen.y;
                float ratio = health.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)health.Current / health.MaxHealth);

                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(x - 1f, y - 1f, 54f, 8f), _bar);
                GUI.color = new Color(0.35f, 0.85f, 0.45f);
                GUI.DrawTexture(new Rect(x, y, 52f * ratio, 6f), _bar);
                GUI.color = Color.white;

                GUI.Label(new Rect(x - 6f, y + 8f, 120f, 18f), $"{unit.Archetype} {health.Current}/{health.MaxHealth}");
            }
        }
    }
}
