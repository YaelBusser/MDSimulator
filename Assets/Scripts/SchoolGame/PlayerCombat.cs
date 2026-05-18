using UnityEngine;

namespace SchoolGame
{
    [System.Serializable]
    public class WeaponData
    {
        public string weaponName = "Poings";
        public float damage = 8f;
        public float range = 2.2f;
        public float radius = 1.2f;
        public float cooldown = 0.6f;
    }

    public class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Arme de départ")]
        public WeaponData currentWeapon = new WeaponData();

        [Tooltip("Bouton souris : 0 = clic gauche")]
        public int attackMouseButton = 0;

        public string CurrentWeaponName => currentWeapon != null ? currentWeapon.weaponName : "—";

        private float nextAttackTime;

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (Input.GetMouseButtonDown(attackMouseButton) && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + currentWeapon.cooldown;
                DoAttack();
            }
        }

        private void DoAttack()
        {
            Vector3 origin = transform.position + Vector3.up * 1f + transform.forward * (currentWeapon.range * 0.5f);

            Collider[] hits = Physics.OverlapSphere(origin, currentWeapon.radius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<EnemyAI>();
                if (enemy == null) continue;

                var h = enemy.GetComponent<Health>();
                if (h != null && !h.IsDead)
                    h.TakeDamage(currentWeapon.damage);
            }
        }

        public void EquipWeapon(WeaponData weapon)
        {
            if (weapon == null) return;
            currentWeapon = weapon;
            GameManager.Instance?.ShowMessage("Arme équipée : " + weapon.weaponName);
        }
    }
}
