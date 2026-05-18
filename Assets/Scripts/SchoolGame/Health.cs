using System;
using UnityEngine;

namespace SchoolGame
{
    // Composant de vie générique réutilisé par le joueur et les ennemis.
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action OnDamaged;
        public event Action OnDeath;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void SetMaxHealth(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill) Current = maxHealth;
            OnHealthChanged?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            OnHealthChanged?.Invoke(Current, maxHealth);
            OnDamaged?.Invoke();

            if (Current <= 0f)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            OnHealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
