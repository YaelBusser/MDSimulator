using UnityEngine;

namespace SchoolGame
{
    // IA d'ennemi légère, sans NavMesh : poursuite physique du joueur + attaque au corps à corps.
    // Pilote un Animator (animations Invector retargetées sur le rig Synty Humanoid).
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Déplacement")]
        public float moveSpeed = 2.5f;
        public float aggroRange = 16f;
        public float turnSpeed = 10f;

        [Header("Attaque")]
        public float attackRange = 2f;
        public float attackDamage = 8f;
        public float attackInterval = 1.4f;

        [Header("Réaction aux coups")]
        public float knockbackForce = 3.5f;
        public float staggerDuration = 0.18f;

        private Rigidbody body;
        private Health health;
        private Animator animator;
        private Transform target;
        private float nextAttackTime;
        private float staggerUntil;
        private float flashUntil;
        private bool dead;

        private Renderer[] renderers;
        private MaterialPropertyBlock mpb;
        private MaterialPropertyBlock emptyMpb;
        private bool flashing;
        private Vector3 baseScale;
        private float punchUntil;

        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashAttack = Animator.StringToHash("Attack");
        private static readonly int HashDie = Animator.StringToHash("Die");
        private static readonly int HashBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int HashColor = Shader.PropertyToID("_Color");
        private static readonly int HashEmission = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            health = GetComponent<Health>();
            health.OnDeath += HandleDeath;
            health.OnDamaged += HandleDamaged;

            animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;

            renderers = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            emptyMpb = new MaterialPropertyBlock();
            baseScale = transform.localScale;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
                health.OnDamaged -= HandleDamaged;
            }
        }

        private void Update()
        {
            bool shouldFlash = Time.time < flashUntil;
            if (shouldFlash != flashing)
            {
                flashing = shouldFlash;
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    if (shouldFlash)
                    {
                        // On couvre les différents shaders (URP Lit / Synty / Standard).
                        var white = new Color(3f, 3f, 3f, 1f);
                        mpb.SetColor(HashBaseColor, white);
                        mpb.SetColor(HashColor, white);
                        mpb.SetColor(HashEmission, new Color(2f, 2f, 2f, 1f));
                        r.SetPropertyBlock(mpb);
                    }
                    else r.SetPropertyBlock(emptyMpb);
                }
            }

            // À-coup d'échelle : feedback de coup visible quel que soit le shader.
            if (!dead)
            {
                float t = punchUntil - Time.time;
                if (t > 0f)
                {
                    float k = 1f + 0.15f * Mathf.Sin((1f - t / 0.12f) * Mathf.PI);
                    transform.localScale = baseScale * k;
                }
                else if (transform.localScale != baseScale)
                {
                    transform.localScale = baseScale;
                }
            }
        }

        private void FixedUpdate()
        {
            if (dead) return;

            if (target == null)
            {
                var gm = GameManager.Instance;
                if (gm != null) target = gm.PlayerTransform;
                if (target == null) return;
            }

            // Recul : on laisse la vélocité de knockback s'amortir.
            if (Time.time < staggerUntil)
            {
                Vector3 v = body.linearVelocity;
                body.linearVelocity = new Vector3(v.x * 0.9f, v.y, v.z * 0.9f);
                if (animator != null) animator.SetFloat(HashSpeed, 0f);
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            float planarSpeed = 0f;

            if (dist <= aggroRange)
            {
                Vector3 dir = toTarget.normalized;

                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(dir);
                    body.rotation = Quaternion.Slerp(body.rotation, look, turnSpeed * Time.fixedDeltaTime);
                }

                if (dist > attackRange)
                {
                    Vector3 step = dir * moveSpeed;
                    body.linearVelocity = new Vector3(step.x, body.linearVelocity.y, step.z);
                    planarSpeed = moveSpeed;
                }
                else
                {
                    body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
                    TryAttack();
                }
            }
            else
            {
                body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            }

            if (animator != null)
                animator.SetFloat(HashSpeed, planarSpeed);
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime) return;
            nextAttackTime = Time.time + attackInterval;

            if (animator != null) animator.SetTrigger(HashAttack);

            GameManager.Instance?.DamagePlayer(attackDamage);
        }

        private void HandleDamaged()
        {
            if (dead) return;

            flashUntil = Time.time + 0.12f;
            punchUntil = Time.time + 0.12f;
            staggerUntil = Time.time + staggerDuration;

            Vector3 src = target != null ? target.position : transform.position - transform.forward;
            Vector3 away = transform.position - src;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();

            body.linearVelocity = new Vector3(away.x * knockbackForce,
                body.linearVelocity.y, away.z * knockbackForce);
        }

        private void HandleDeath()
        {
            dead = true;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;

            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            if (animator != null) animator.SetTrigger(HashDie);

            GameManager.Instance?.NotifyEnemyKilled();
            Destroy(gameObject, 3f);
        }
    }
}
