using System.Collections.Generic;
using UnityEngine;

namespace SchoolGame
{
    // Place cet objet dans une pièce. Il fait apparaître des ennemis à partir
    // de simples prefabs de modèles (Synty, etc.) auxquels il ajoute l'IA au runtime.
    public class EnemySpawner : MonoBehaviour
    {
        public enum SpawnMode { OnStart, WhenPlayerNear }

        [Header("Modèles d'ennemis (prefabs Synty, etc.)")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();

        [Tooltip("Controller d'animation (auto-assigné par l'outil de setup)")]
        public RuntimeAnimatorController enemyAnimator;

        [Tooltip("Avatar Humanoid (requis pour les prefabs sans Animator, ex. Prototype)")]
        public Avatar enemyAvatar;

        [Header("Quand spawner ?")]
        public SpawnMode mode = SpawnMode.WhenPlayerNear;
        [Tooltip("Distance d'activation pour le mode WhenPlayerNear")]
        public float activationDistance = 12f;

        [Header("Vague")]
        public int totalToSpawn = 5;
        public int maxAlive = 3;
        public float spawnInterval = 2f;
        public float spawnRadius = 3.5f;

        [Header("Stats des ennemis")]
        public float enemyHealth = 40f;
        public float enemyMoveSpeed = 2.5f;
        public float enemyDamage = 8f;
        public float enemyAttackRange = 2f;
        public float enemyAttackInterval = 1.2f;
        public float enemyAggroRange = 16f;
        [Tooltip("Échelle du modèle (1 = normal, >1 pour un boss)")]
        public float enemyScale = 1f;
        [Tooltip("Affiche une barre de vie au-dessus de la tête (pour le boss)")]
        public bool showHealthBar = false;

        public int TotalToSpawn => totalToSpawn;

        private int spawnedCount;
        private int aliveCount;
        private bool activated;
        private float nextSpawnTime;

        private void Start()
        {
            GameManager.Instance?.RegisterSpawner(this);
            if (mode == SpawnMode.OnStart) activated = true;
        }

        private void Update()
        {
            if (!activated)
            {
                var t = GameManager.Instance?.PlayerTransform;
                if (t != null && Vector3.Distance(t.position, transform.position) <= activationDistance)
                    activated = true;
                else
                    return;
            }

            if (spawnedCount >= totalToSpawn) return;
            if (aliveCount >= maxAlive) return;
            if (Time.time < nextSpawnTime) return;

            nextSpawnTime = Time.time + spawnInterval;
            SpawnOne();
        }

        private void SpawnOne()
        {
            if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            if (prefab == null) return;

            Vector2 rnd = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + new Vector3(rnd.x, 0.2f, rnd.y);

            GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
            enemy.name = prefab.name + "_Enemy";
            if (!Mathf.Approximately(enemyScale, 1f))
                enemy.transform.localScale *= enemyScale;

            ConfigureEnemy(enemy);

            spawnedCount++;
            aliveCount++;
            GameManager.Instance?.NotifyEnemySpawned();
        }

        private void ConfigureEnemy(GameObject enemy)
        {
            // Animator : certains prefabs Synty l'ont déjà (Office), d'autres non
            // (Prototype). On l'ajoute au besoin et on assigne l'avatar Humanoid.
            var animator = enemy.GetComponentInChildren<Animator>();
            if (animator == null) animator = enemy.AddComponent<Animator>();
            if (enemyAvatar != null) animator.avatar = enemyAvatar;
            if (enemyAnimator != null) animator.runtimeAnimatorController = enemyAnimator;
            animator.applyRootMotion = false;

            // Capsule de collision englobant le modèle.
            var capsule = enemy.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            var rb = enemy.GetComponent<Rigidbody>();
            if (rb == null) rb = enemy.AddComponent<Rigidbody>();
            rb.mass = 60f;
            rb.useGravity = true;

            var health = enemy.GetComponent<Health>();
            if (health == null) health = enemy.AddComponent<Health>();
            health.SetMaxHealth(enemyHealth);
            health.OnDeath += () => aliveCount = Mathf.Max(0, aliveCount - 1);

            var ai = enemy.GetComponent<EnemyAI>();
            if (ai == null) ai = enemy.AddComponent<EnemyAI>();
            ai.moveSpeed = enemyMoveSpeed;
            ai.attackDamage = enemyDamage;
            ai.attackRange = enemyAttackRange;
            ai.attackInterval = enemyAttackInterval;
            ai.aggroRange = enemyAggroRange;

            if (showHealthBar && enemy.GetComponent<EnemyHealthBar>() == null)
                enemy.AddComponent<EnemyHealthBar>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            if (mode == SpawnMode.WhenPlayerNear)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, activationDistance);
            }
        }
    }
}
